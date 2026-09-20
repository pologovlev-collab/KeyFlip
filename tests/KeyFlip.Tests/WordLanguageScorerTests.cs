using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class WordLanguageScorerTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        SingleLetterWithValidTargetIsAmbiguous();
        DeterministicConversionOutranksValidityInBothLanguages();
        PhysicalClusterCanOverrideValidSourceWord();
        PhysicalClusterEvidenceOutranksSpellValidity();
        WeakSpellValidityDefersToClauseContext();
        StrongDeterministicEnglishEvidenceStaysKeptWithoutDictionary();
        ClearlyValidSourceWordIsConfidentKeep();
    }

    private void SingleLetterWithValidTargetIsAmbiguous()
    {
        var decider = new MixedWordDecider(new FixedScorer(originalValid: true, convertedValid: true));

        Equal(
            WordConversionDecision.Ambiguous,
            decider.Decide("B", WordLanguage.English, "И", WordLanguage.Russian));
    }

    private void DeterministicConversionOutranksValidityInBothLanguages()
    {
        var decider = new MixedWordDecider(new FixedScorer(originalValid: true, convertedValid: true));

        Equal(
            WordConversionDecision.ConfidentConvert,
            decider.Decide("kb", WordLanguage.English, "ли", WordLanguage.Russian));
    }

    private void PhysicalClusterCanOverrideValidSourceWord()
    {
        var decider = new MixedWordDecider(new FixedScorer(originalValid: true, convertedValid: true));

        Equal(
            WordConversionDecision.ConfidentConvert,
            decider.Decide(
                "dc",
                WordLanguage.English,
                "всё",
                WordLanguage.Russian,
                conversionEvidenceBonus: 4,
                isPhysicalCluster: true));
    }

    private void PhysicalClusterEvidenceOutranksSpellValidity()
    {
        var decider = new MixedWordDecider(new FixedScorer(originalValid: true, convertedValid: false));

        Equal(
            WordConversionDecision.ConfidentConvert,
            decider.Decide(
                "bpm",
                WordLanguage.English,
                "жизь",
                WordLanguage.Russian,
                conversionEvidenceBonus: 4,
                isPhysicalCluster: true));
    }

    private void WeakSpellValidityDefersToClauseContext()
    {
        var decider = new MixedWordDecider(new FixedScorer(originalValid: true, convertedValid: false));

        Equal(
            WordConversionDecision.LexicalKeep,
            decider.Decide("vjtve", WordLanguage.English, "моему", WordLanguage.Russian));
    }

    private void StrongDeterministicEnglishEvidenceStaysKeptWithoutDictionary()
    {
        var decider = new MixedWordDecider(null);

        Equal(
            WordConversionDecision.ConfidentKeep,
            decider.Decide("source", WordLanguage.English, "ыщгксу", WordLanguage.Russian));
    }

    private void ClearlyValidSourceWordIsConfidentKeep()
    {
        var decider = new MixedWordDecider(new FixedScorer(originalValid: true, convertedValid: false));

        Equal(
            WordConversionDecision.ConfidentKeep,
            decider.Decide("codwars", WordLanguage.English, "сщвцфкы", WordLanguage.Russian));
    }

    private void Equal(WordConversionDecision expected, WordConversionDecision actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private sealed class FixedScorer(bool originalValid, bool convertedValid) : IWordLanguageScorer
    {
        public bool? IsValid(string word, WordLanguage language) =>
            language == WordLanguage.English ? originalValid : convertedValid;
    }
}
