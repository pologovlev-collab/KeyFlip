using System.Text;

namespace KeyFlip;

public static class LayoutConverter
{
    private enum ConversionDirection
    {
        None,
        EnglishToRussian,
        RussianToEnglish
    }

    private enum TokenResolution
    {
        ConfidentConvert,
        ContextConvert,
        ConfidentKeep,
        HardKeep,
        Ambiguous
    }

    private readonly record struct WordToken(
        int Start,
        int End,
        ConversionDirection CandidateDirection,
        TokenResolution Resolution,
        bool PreserveAdjacentSeparator)
    {
        internal ConversionDirection Direction => Resolution is TokenResolution.ConfidentConvert or TokenResolution.ContextConvert
            ? CandidateDirection
            : ConversionDirection.None;

        internal int Length => End - Start;
    }

    private const string StrongEnglishSymbols = "@#$^&";
    private const string StrongRussianSymbols = "№";
    private const string EnglishWordClusterSymbols = "`[];',.";
    private static readonly Lazy<MixedWordDecider> MixedDecider = new(static () => new MixedWordDecider());
    private static readonly IReadOnlyDictionary<char, char> EnglishToRussian = CreateMap(
        ("`~", "ёЁ"),
        ("1!", "1!"), ("2@", "2\""), ("3#", "3№"), ("4$", "4;"), ("5%", "5%"),
        ("6^", "6:"), ("7&", "7?"), ("8*", "8*"), ("9(", "9("), ("0)", "0)"),
        ("-_", "-_"), ("=+", "=+"),
        ("qQ", "йЙ"), ("wW", "цЦ"), ("eE", "уУ"), ("rR", "кК"), ("tT", "еЕ"),
        ("yY", "нН"), ("uU", "гГ"), ("iI", "шШ"), ("oO", "щЩ"), ("pP", "зЗ"),
        ("[{", "хХ"), ("]}", "ъЪ"), ("\\|", "\\/"),
        ("aA", "фФ"), ("sS", "ыЫ"), ("dD", "вВ"), ("fF", "аА"), ("gG", "пП"),
        ("hH", "рР"), ("jJ", "оО"), ("kK", "лЛ"), ("lL", "дД"), (";:", "жЖ"),
        ("'\"", "эЭ"),
        ("zZ", "яЯ"), ("xX", "чЧ"), ("cC", "сС"), ("vV", "мМ"), ("bB", "иИ"),
        ("nN", "тТ"), ("mM", "ьЬ"), (",<", "бБ"), (".>", "юЮ"), ("/?", ".,"));

    private static readonly IReadOnlyDictionary<char, char> RussianToEnglish = CreateReverseMap(
        ("`~", "ёЁ"),
        ("1!", "1!"), ("2@", "2\""), ("3#", "3№"), ("4$", "4;"), ("5%", "5%"),
        ("6^", "6:"), ("7&", "7?"), ("8*", "8*"), ("9(", "9("), ("0)", "0)"),
        ("-_", "-_"), ("=+", "=+"),
        ("qQ", "йЙ"), ("wW", "цЦ"), ("eE", "уУ"), ("rR", "кК"), ("tT", "еЕ"),
        ("yY", "нН"), ("uU", "гГ"), ("iI", "шШ"), ("oO", "щЩ"), ("pP", "зЗ"),
        ("[{", "хХ"), ("]}", "ъЪ"), ("\\|", "\\/"),
        ("aA", "фФ"), ("sS", "ыЫ"), ("dD", "вВ"), ("fF", "аА"), ("gG", "пП"),
        ("hH", "рР"), ("jJ", "оО"), ("kK", "лЛ"), ("lL", "дД"), (";:", "жЖ"),
        ("'\"", "эЭ"),
        ("zZ", "яЯ"), ("xX", "чЧ"), ("cC", "сС"), ("vV", "мМ"), ("bB", "иИ"),
        ("nN", "тТ"), ("mM", "ьЬ"), (",<", "бБ"), (".>", "юЮ"), ("/?", ".,"));

    public static string Convert(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokenCount = CountAlphabeticTokens(text);
        if (tokenCount == 0) return ConvertSymbolOnly(text);
        if (tokenCount == 1) return ConvertSingleToken(text);
        return ConvertSmart(text);
    }

    internal static ConversionResult ConvertCodeSafe(string text) =>
        ConvertWords(text, forceSingleToken: false, protectIdentifierFragments: false);

    internal static ConversionResult ConvertCodeAware(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var safeCandidate = ConvertCodeSafe(text);
        var syntaxCandidate = ApplyConfirmedWrongLayoutSyntax(text, safeCandidate.OutputText);
        return ConversionResult.FromCharacterDifferences(text, syntaxCandidate);
    }

    internal static ConversionResult ConvertTargeted(string text, bool forceSingleToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        var outputText = CountAlphabeticTokens(text) == 0
            ? ConvertSymbolOnly(text)
            : ConvertWithTokens(text, CreateWordTokens(
                text,
                preserveTechnicalTokens: true,
                forceSingleToken,
                conservative: true));
        return ConversionResult.FromCharacterDifferences(text, outputText);
    }

    internal static ConversionResult ConvertWords(
        string text,
        bool forceSingleToken,
        bool protectIdentifierFragments = true)
    {
        ArgumentNullException.ThrowIfNull(text);
        var words = CreateWordTokens(
            text,
            preserveTechnicalTokens: true,
            forceSingleToken,
            conservative: true,
            protectIdentifierFragments);
        if (words.Count == 0) return ConversionResult.Unchanged(text);

        var edits = words
            .Where(static word => word.Direction != ConversionDirection.None)
            .Select(word => new ConversionEdit(
                word.Start,
                word.End - word.Start,
                ConvertWithMap(text[word.Start..word.End], GetMap(word.Direction))))
            .ToArray();
        return ConversionResult.FromEdits(text, edits);
    }

    private static string ConvertSingleToken(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var language = GetLanguage(text[index]);
            if (language is null) continue;

            var start = index;
            while (index < text.Length && GetLanguage(text[index]) == language) index++;
            if (TechnicalTokenDetector.IsKnownProtectedWord(text[start..index])) return text;
            return ConvertWithMap(text, language == WordLanguage.English ? EnglishToRussian : RussianToEnglish);
        }

        return text;
    }

    private static string ConvertSmart(string text)
    {
        var words = CreateWordTokens(text, preserveTechnicalTokens: true, forceSingleToken: false, conservative: false);
        ResolveAmbiguousTokens(text, words);
        return ConvertWithTokens(text, words);
    }

    private static string ConvertWithTokens(string text, IReadOnlyList<WordToken> words)
    {
        var result = new StringBuilder(text.Length);

        AppendLeadingSeparator(result, text[..words[0].Start], words[0].Direction);
        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];
            AppendWithDirection(result, text[word.Start..word.End], word.Direction);

            if (index + 1 < words.Count)
            {
                var next = words[index + 1];
                AppendBetweenWords(result, text[word.End..next.Start], word, next);
            }
        }

        var trailingSeparator = text[words[^1].End..];
        var trailingDirection = words[^1].Direction;
        if (trailingDirection == ConversionDirection.None && IsWrongLayoutTrailingPunctuation(trailingSeparator))
        {
            trailingDirection = InferTrailingDirection(words);
        }

        AppendTrailingSeparator(result, trailingSeparator, trailingDirection);
        return result.ToString();
    }

    private static List<WordToken> CreateWordTokens(
        string text,
        bool preserveTechnicalTokens,
        bool forceSingleToken,
        bool conservative,
        bool protectIdentifierFragments = true)
    {
        var words = new List<WordToken>();
        var evidenceDecisions = new Dictionary<(string Original, string Converted, WordLanguage Language, int Bonus), TokenResolution>();
        for (var index = 0; index < text.Length;)
        {
            var language = GetLanguage(text[index]);
            if (language is null)
            {
                index++;
                continue;
            }

            var start = index;
            while (index < text.Length && GetLanguage(text[index]) == language) index++;

            var original = text[start..index];
            var convertedLanguage = language == WordLanguage.English ? WordLanguage.Russian : WordLanguage.English;
            var candidateDirection = language == WordLanguage.English
                ? ConversionDirection.EnglishToRussian
                : ConversionDirection.RussianToEnglish;
            var converted = ConvertWithMap(original, GetMap(candidateDirection));
            var protectCurrentIdentifierFragment = protectIdentifierFragments || language == WordLanguage.English;
            var belongsToEmailOrUrl = preserveTechnicalTokens &&
                TechnicalTokenDetector.BelongsToEmailOrUrl(text, start, index);
            var allowNumericSuffix = !conservative && IsContextualNumericSuffix(text, start, index, language.Value);
            var isProtected = preserveTechnicalTokens && !allowNumericSuffix && TechnicalTokenDetector.ShouldKeep(
                text,
                start,
                index,
                protectCurrentIdentifierFragment,
                protectSingleCharacter: conservative);
            var clusterOriginal = string.Empty;
            var clusterConverted = string.Empty;
            var hasClusterEvidence = !conservative && TryGetClusterEvidence(
                    text,
                    start,
                    index,
                    language.Value,
                    convertedLanguage,
                    candidateDirection,
                    out clusterOriginal,
                    out clusterConverted);
            TokenResolution resolution;
            if (isProtected)
            {
                resolution = TokenResolution.HardKeep;
            }
            else
            {
                var evidenceKey = (original, converted, language.Value, Bonus: 0);
                if (!evidenceDecisions.TryGetValue(evidenceKey, out resolution))
                {
                    resolution = GetInitialResolution(
                        original,
                        language.Value,
                        converted,
                        convertedLanguage,
                        conservative,
                        conversionEvidenceBonus: 0,
                        isPhysicalCluster: false);
                    evidenceDecisions[evidenceKey] = resolution;
                }

                if (hasClusterEvidence)
                {
                    var clusterKey = (clusterOriginal, clusterConverted, language.Value, Bonus: 4);
                    if (!evidenceDecisions.TryGetValue(clusterKey, out var clusterResolution))
                    {
                        clusterResolution = GetInitialResolution(
                            clusterOriginal,
                            language.Value,
                            clusterConverted,
                            convertedLanguage,
                            conservative,
                            conversionEvidenceBonus: 4,
                            isPhysicalCluster: true);
                        evidenceDecisions[clusterKey] = clusterResolution;
                    }

                    if (clusterResolution == TokenResolution.ConfidentConvert)
                    {
                        resolution = clusterResolution;
                    }
                }
            }

            words.Add(new WordToken(start, index, candidateDirection, resolution, belongsToEmailOrUrl));
        }

        if (forceSingleToken && words.Count == 1)
        {
            var word = words[0];
            if (!preserveTechnicalTokens ||
                !TechnicalTokenDetector.IsKnownProtectedWord(text[word.Start..word.End]))
            {
                words[0] = word with { Resolution = TokenResolution.ConfidentConvert };
            }
        }

        return words;
    }

    private static bool TryGetClusterEvidence(
        string text,
        int wordStart,
        int wordEnd,
        WordLanguage originalLanguage,
        WordLanguage convertedLanguage,
        ConversionDirection direction,
        out string original,
        out string converted)
    {
        var start = wordStart;
        while (start > 0 && IsSameWordClusterCharacter(text[start - 1], originalLanguage)) start--;
        var end = wordEnd;
        while (end < text.Length && IsSameWordClusterCharacter(text[end], originalLanguage)) end++;

        original = text[start..end];
        if (start == wordStart && end == wordEnd)
        {
            converted = string.Empty;
            return false;
        }

        var originalLetters = new string(original.Where(character => GetLanguage(character) == originalLanguage).ToArray());
        var convertedCluster = ConvertWithMap(original, GetMap(direction));
        if (originalLetters.Length == 0 || convertedCluster.Any(character => GetLanguage(character) != convertedLanguage))
        {
            converted = string.Empty;
            return false;
        }

        original = originalLetters;
        converted = convertedCluster;
        return true;
    }

    private static bool IsSameWordClusterCharacter(char character, WordLanguage language) =>
        GetLanguage(character) == language ||
        language == WordLanguage.English && EnglishWordClusterSymbols.Contains(character, StringComparison.Ordinal);

    private static TokenResolution GetInitialResolution(
        string original,
        WordLanguage originalLanguage,
        string converted,
        WordLanguage convertedLanguage,
        bool conservative,
        int conversionEvidenceBonus,
        bool isPhysicalCluster)
    {
        if (conservative)
        {
            return MixedDecider.Value.ShouldUseConvertedConservatively(
                original,
                originalLanguage,
                converted,
                convertedLanguage)
                ? TokenResolution.ConfidentConvert
                : TokenResolution.HardKeep;
        }

        return MixedDecider.Value.Decide(
            original,
            originalLanguage,
            converted,
            convertedLanguage,
            conversionEvidenceBonus,
            isPhysicalCluster) switch
        {
            WordConversionDecision.ConfidentConvert => TokenResolution.ConfidentConvert,
            WordConversionDecision.ConfidentKeep => TokenResolution.ConfidentKeep,
            WordConversionDecision.HardKeep => TokenResolution.HardKeep,
            _ => TokenResolution.Ambiguous
        };
    }

    private static void ResolveAmbiguousTokens(string text, List<WordToken> words)
    {
        ResolveSandwichedSpans(text, words);
        ResolvePhraseEdges(text, words);
    }

    private static void ResolveSandwichedSpans(string text, List<WordToken> words)
    {
        for (var start = 1; start + 1 < words.Count; start++)
        {
            if (words[start].Resolution != TokenResolution.Ambiguous) continue;

            var end = start;
            while (end + 1 < words.Count && words[end + 1].Resolution == TokenResolution.Ambiguous) end++;
            if (end - start + 1 > 3 || end + 1 >= words.Count)
            {
                start = end;
                continue;
            }

            var left = words[start - 1];
            var right = words[end + 1];
            var direction = left.CandidateDirection;
            var hasStrongSandwich = IsSupportingAnchor(left, direction) &&
                IsSupportingAnchor(right, direction) &&
                !HasContextBoundary(text, left.End, right.Start);
            var lengthsAreSafe = true;
            for (var index = start; index <= end; index++)
            {
                var length = words[index].Length;
                if (words[index].CandidateDirection != direction || length > 4 ||
                    (length == 4 && start != end))
                {
                    lengthsAreSafe = false;
                    break;
                }
            }

            if (hasStrongSandwich && lengthsAreSafe)
            {
                for (var index = start; index <= end; index++)
                {
                    words[index] = words[index] with { Resolution = TokenResolution.ContextConvert };
                }
            }

            start = end;
        }
    }

    private static void ResolvePhraseEdges(string text, List<WordToken> words)
    {
        const int LocalWindow = 4;
        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];
            if (word.Resolution != TokenResolution.Ambiguous || word.Length > 3) continue;

            var anchorCount = 0;
            var oppositeAnchorCount = 0;
            var leftBlocked = false;
            var rightBlocked = false;
            for (var offset = 1; offset <= LocalWindow; offset++)
            {
                var left = index - offset;
                if (!leftBlocked && left >= 0)
                {
                    if (words[left].Resolution == TokenResolution.HardKeep ||
                        HasContextBoundary(text, words[left].End, words[left + 1].Start))
                    {
                        leftBlocked = true;
                    }
                    else if (IsLexicalAnchor(words[left]))
                    {
                        if (IsSupportingAnchor(words[left], word.CandidateDirection)) anchorCount++;
                        else oppositeAnchorCount++;
                    }
                }

                var right = index + offset;
                if (!rightBlocked && right < words.Count)
                {
                    if (words[right].Resolution == TokenResolution.HardKeep ||
                        HasContextBoundary(text, words[right - 1].End, words[right].Start))
                    {
                        rightBlocked = true;
                    }
                    else if (IsLexicalAnchor(words[right]))
                    {
                        if (IsSupportingAnchor(words[right], word.CandidateDirection)) anchorCount++;
                        else oppositeAnchorCount++;
                    }
                }
            }

            var hasEnoughEvidence = anchorCount >= 2 && anchorCount > oppositeAnchorCount ||
                word.Length == 1 && anchorCount >= 1 && oppositeAnchorCount == 0;
            if (hasEnoughEvidence)
            {
                words[index] = word with { Resolution = TokenResolution.ContextConvert };
            }
        }
    }

    private static bool HasContextBoundary(string text, int start, int end)
    {
        for (var index = start; index < end; index++)
        {
            if (text[index] is '\r' or '\n') return true;
            if (text[index] is '.' or '!' or '?' or '/' &&
                (index + 1 >= end || char.IsWhiteSpace(text[index + 1])))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLexicalAnchor(WordToken word) =>
        word.Resolution is TokenResolution.ConfidentConvert or TokenResolution.ConfidentKeep;

    private static bool IsSupportingAnchor(WordToken anchor, ConversionDirection candidateDirection) =>
        anchor.Resolution switch
        {
            TokenResolution.ConfidentConvert => anchor.CandidateDirection == candidateDirection,
            TokenResolution.ConfidentKeep => anchor.CandidateDirection != candidateDirection,
            _ => false
        };

    private static bool IsContextualNumericSuffix(
        string text,
        int start,
        int end,
        WordLanguage language)
    {
        if (language != WordLanguage.English || end - start != 1 || !char.IsLower(text[start]) ||
            start == 0 || !char.IsDigit(text[start - 1]) ||
            end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
        {
            return false;
        }

        var numberStart = start - 1;
        while (numberStart > 0 && char.IsDigit(text[numberStart - 1])) numberStart--;
        return numberStart == 0 || !char.IsLetterOrDigit(text[numberStart - 1]) && text[numberStart - 1] != '_';
    }

    private static int CountAlphabeticTokens(string text)
    {
        var count = 0;
        for (var index = 0; index < text.Length;)
        {
            var language = GetLanguage(text[index]);
            if (language is null)
            {
                index++;
                continue;
            }

            count++;
            while (index < text.Length && GetLanguage(text[index]) == language) index++;
        }

        return count;
    }

    private static void AppendLeadingSeparator(StringBuilder result, string separator, ConversionDirection nextDirection)
    {
        var lastWhitespace = separator.FindLastIndex(char.IsWhiteSpace);
        result.Append(separator.AsSpan(0, lastWhitespace + 1));
        AppendWithDirection(result, separator[(lastWhitespace + 1)..], nextDirection);
    }

    private static void AppendBetweenWords(
        StringBuilder result,
        string separator,
        WordToken previous,
        WordToken next)
    {
        var firstWhitespace = separator.FindIndex(char.IsWhiteSpace);
        if (firstWhitespace < 0)
        {
            if (previous.PreserveAdjacentSeparator || next.PreserveAdjacentSeparator)
            {
                result.Append(separator);
            }
            else
            {
                AppendWithDirection(result, separator, previous.Direction);
            }
            return;
        }

        var lastWhitespace = separator.FindLastIndex(char.IsWhiteSpace);
        AppendWithDirection(result, separator[..firstWhitespace], previous.Direction);
        result.Append(separator.AsSpan(firstWhitespace, lastWhitespace - firstWhitespace + 1));
        AppendWithDirection(result, separator[(lastWhitespace + 1)..], next.Direction);
    }

    private static void AppendTrailingSeparator(StringBuilder result, string separator, ConversionDirection previousDirection)
    {
        var firstWhitespace = separator.FindIndex(char.IsWhiteSpace);
        if (firstWhitespace < 0)
        {
            AppendWithDirection(result, separator, previousDirection);
            return;
        }

        var lastWhitespace = separator.FindLastIndex(char.IsWhiteSpace);
        AppendWithDirection(result, separator[..firstWhitespace], previousDirection);
        result.Append(separator.AsSpan(firstWhitespace, lastWhitespace - firstWhitespace + 1));
        AppendWithDirection(result, separator[(lastWhitespace + 1)..], previousDirection);
    }

    private static bool IsWrongLayoutTrailingPunctuation(string separator) =>
        separator.Any(character => character is '/' or '?') &&
        separator.All(character => char.IsWhiteSpace(character) || character is '/' or '?');

    private static ConversionDirection InferTrailingDirection(IReadOnlyList<WordToken> words)
    {
        const int LocalWindow = 4;
        var englishToRussian = 0;
        var russianToEnglish = 0;
        for (var index = words.Count - 1; index >= 0 && words.Count - index <= LocalWindow; index--)
        {
            var word = words[index];
            if (word.Resolution == TokenResolution.HardKeep) break;
            if (!IsLexicalAnchor(word)) continue;

            var outputDirection = word.Resolution == TokenResolution.ConfidentConvert
                ? word.CandidateDirection
                : Opposite(word.CandidateDirection);
            if (outputDirection == ConversionDirection.EnglishToRussian) englishToRussian++;
            else if (outputDirection == ConversionDirection.RussianToEnglish) russianToEnglish++;
        }

        if (englishToRussian >= 2 && englishToRussian > russianToEnglish)
        {
            return ConversionDirection.EnglishToRussian;
        }

        return ConversionDirection.None;
    }

    private static ConversionDirection Opposite(ConversionDirection direction) => direction switch
    {
        ConversionDirection.EnglishToRussian => ConversionDirection.RussianToEnglish,
        ConversionDirection.RussianToEnglish => ConversionDirection.EnglishToRussian,
        _ => ConversionDirection.None
    };

    private static void AppendWithDirection(StringBuilder result, string text, ConversionDirection direction)
    {
        result.Append(direction == ConversionDirection.None ? text : ConvertWithMap(text, GetMap(direction)));
    }

    private static IReadOnlyDictionary<char, char> GetMap(ConversionDirection direction) => direction switch
    {
        ConversionDirection.EnglishToRussian => EnglishToRussian,
        ConversionDirection.RussianToEnglish => RussianToEnglish,
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static int FindIndex(this string text, Func<char, bool> predicate)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (predicate(text[index])) return index;
        }

        return -1;
    }

    private static int FindLastIndex(this string text, Func<char, bool> predicate)
    {
        for (var index = text.Length - 1; index >= 0; index--)
        {
            if (predicate(text[index])) return index;
        }

        return -1;
    }

    private static string ConvertSymbolOnly(string text)
    {
        var englishEvidence = text.Count(character => StrongEnglishSymbols.Contains(character, StringComparison.Ordinal));
        var russianEvidence = text.Count(character => StrongRussianSymbols.Contains(character, StringComparison.Ordinal));
        if (englishEvidence == russianEvidence) return text;
        return ConvertWithMap(text, englishEvidence > russianEvidence ? EnglishToRussian : RussianToEnglish);
    }

    private static string ConvertWithMap(string text, IReadOnlyDictionary<char, char> map)
    {
        return string.Create(text.Length, (text, map), static (destination, state) =>
        {
            for (var index = 0; index < state.text.Length; index++)
            {
                destination[index] = state.map.TryGetValue(state.text[index], out var converted)
                    ? converted
                    : state.text[index];
            }
        });
    }

    private static string ApplyConfirmedWrongLayoutSyntax(string source, string safeCandidate)
    {
        var result = safeCandidate.ToCharArray();
        for (var opening = source.IndexOf('Э'); opening >= 0; opening = source.IndexOf('Э', opening + 1))
        {
            var closing = source.IndexOf('Э', opening + 1);
            if (closing < 0 || !IsStringLiteralPosition(source, opening, closing)) continue;

            var content = source[(opening + 1)..closing];
            var convertedContent = ConvertWords(
                content,
                forceSingleToken: false,
                protectIdentifierFragments: false).OutputText;
            result[opening] = '"';
            convertedContent.CopyTo(0, result, opening + 1, convertedContent.Length);
            result[closing] = '"';
            if (closing + 1 < source.Length && source[closing + 1] == 'ж')
            {
                result[closing + 1] = ';';
            }

            opening = closing;
        }

        for (var index = 1; index < source.Length; index++)
        {
            if (source[index] == 'ж' && ")]}".Contains(source[index - 1], StringComparison.Ordinal) &&
                (index + 1 == source.Length || char.IsWhiteSpace(source[index + 1])))
            {
                result[index] = ';';
            }
        }

        return new string(result);
    }

    private static bool IsStringLiteralPosition(string text, int opening, int closing)
    {
        if (closing == opening + 1) return false;
        var before = FindNearestNonWhitespace(text, opening - 1, step: -1);
        var after = FindNearestNonWhitespace(text, closing + 1, step: 1);
        return (before == '\0' || "([{=,:".Contains(before, StringComparison.Ordinal)) &&
            (after == '\0' || ")]}.,;ж".Contains(after, StringComparison.Ordinal));
    }

    private static char FindNearestNonWhitespace(string text, int index, int step)
    {
        while (index >= 0 && index < text.Length)
        {
            if (!char.IsWhiteSpace(text[index])) return text[index];
            index += step;
        }

        return '\0';
    }

    private static WordLanguage? GetLanguage(char character)
    {
        if (IsLatinLetter(character)) return WordLanguage.English;
        if (IsRussianLetter(character)) return WordLanguage.Russian;
        return null;
    }

    private static bool IsLatinLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsRussianLetter(char character) =>
        character is >= 'А' and <= 'Я' or >= 'а' and <= 'я' or 'Ё' or 'ё';

    private static IReadOnlyDictionary<char, char> CreateMap(params (string Source, string Target)[] keys) =>
        keys.SelectMany(pair => pair.Source.Zip(pair.Target))
            .ToDictionary(pair => pair.First, pair => pair.Second);

    private static IReadOnlyDictionary<char, char> CreateReverseMap(params (string Source, string Target)[] keys) =>
        keys.SelectMany(pair => pair.Source.Zip(pair.Target))
            .ToDictionary(pair => pair.Second, pair => pair.First);
}
