using KeyFlip;

namespace KeyFlip.Tests;

internal static class ConversionDebugTool
{
    private const string Test11 = "z yt gjybvf. gjxtve d hjccbb nfr ckj;yj gjkexbnm yjhvfkmye. hf,jne? gj rfrjq ghbxbyt vyt yt [jnzn lfdfnm yjhvfkmyjt j,jpjdfybt b gjxtve jyb gsnf.ncz cltkfnm vj. ;bpm [e;t bp lyz d ltym c rf;lsv lytv dc` [e;t b [e;t ?";
    private const string Test12 = "z ,s [jntk gjghj,jdfnm gj;bnm d lheujq cnhfyt d yflt;lt yf kextt yflt.cm r vjtve dsgecre bp depf dc` d vbht cnfytn cbkmyj ghjot b dbpe gjkexbnm nj;t eltn cbkmyj ghjot ?nen ukfdyjt dthbnm d ecg[ b yfltznmcz yf kexitt/";

    internal static int Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: --convert-debug <none|english|russian|both|windows> <TEST11|TEST12|exact text>");
            return 2;
        }

        var inputArgument = string.Join(' ', args.Skip(2));
        var input = inputArgument.ToUpperInvariant() switch
        {
            "TEST11" => Test11,
            "TEST12" => Test12,
            _ => inputArgument
        };
        if (args[1].Equals("windows", StringComparison.OrdinalIgnoreCase))
        {
            return RunWindowsOnSta(input);
        }

        return RunCore(args[1], input);
    }

    private static int RunWindowsOnSta(string input)
    {
        var exitCode = 1;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                exitCode = RunCore("windows", input);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw failure;
        return exitCode;
    }

    private static int RunCore(string mode, string input)
    {
        var scorer = CreateScorer(mode, out var scorerAvailable);
        var result = ConversionDebugAnalyzer.Analyze(input, scorer);

        Console.WriteLine($"MODE\t{mode}\tSCORER_AVAILABLE\t{scorerAvailable}");
        Console.WriteLine($"INPUT\t{input}");
        Console.WriteLine($"OUTPUT\t{result.OutputText}");
        Console.WriteLine("ORIGINAL\tMAPPED\tINITIAL\tSPELL_ORIGINAL\tSPELL_MAPPED\tSCORE_ORIGINAL\tSCORE_MAPPED\tCLUSTER\tLOCAL_SUPPORT\tLOCAL_KEEP\tCLAUSE_SUPPORT\tCLAUSE_KEEP\tFINAL");
        foreach (var token in result.Tokens)
        {
            var cluster = string.IsNullOrEmpty(token.ClusterOriginal)
                ? "-"
                : $"{token.ClusterOriginal}->{token.ClusterMapped}";
            Console.WriteLine(string.Join('\t',
                token.Original,
                token.Mapped,
                token.InitialDecision,
                Format(token.OriginalSpellResult),
                Format(token.MappedSpellResult),
                token.OriginalScore,
                token.MappedScore,
                cluster,
                token.LocalSupportingAnchors,
                token.LocalOpposingAnchors,
                token.ClauseConvertSupport,
                token.ClauseKeepSupport,
                token.FinalDecision));
        }

        return 0;
    }

    private static IWordLanguageScorer? CreateScorer(string mode, out bool available)
    {
        IWordLanguageScorer? scorer = mode.ToLowerInvariant() switch
        {
            "none" => null,
            "english" => FakeLanguageScorer.EnglishOnly(),
            "russian" => FakeLanguageScorer.RussianOnly(),
            "both" => FakeLanguageScorer.Both(),
            "windows" => WindowsSpellChecker.TryCreate(),
            _ => throw new ArgumentException($"Unknown scorer mode '{mode}'.")
        };
        available = scorer is not null;
        return scorer;
    }

    private static string Format(bool? value) => value switch
    {
        true => "valid",
        false => "invalid",
        null => "unavailable"
    };
}
