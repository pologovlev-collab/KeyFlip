using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class SmartContextRegressionTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        ConvertsFullTesterCorpus();
        ConvertsRc1OutputCorpus();

        ConvertsTesterFailure("ntgthm e vtyz", "теперь у меня");
        ConvertsTesterFailure("rjulf z 'njn", "когда я этот");
        ConvertsTesterFailure("vjue kb z d nfrb[", "могу ли я в таких");
        ConvertsTesterFailure("d ltym bkb cgfnm d yb[", "в день или спать в них");
        ConvertsTesterFailure("gj'njve gecnm hf,jnftn", "поэтому пусть работает");
        ConvertsTesterFailure("ghj,ktve c jnj,hf;tybtv", "проблему с отображением");
        ConvertsTesterFailure("f yt 4", "а не 4");
        ConvertsTesterFailure(
            "ns pfxtv nj e,hfk ldjqye. fhbavtnbre bp ehjrf",
            "ты зачем то убрал двойную арифметику из урока");
        Converts("ldjbxye.", "двоичную");
        ConvertsTesterFailure("lj,fdm t` j,hfnyj b dc` jr", "добавь её обратно и всё ок");
        ConvertsTesterFailure("rfr vyt e;t yfljtkj", "как мне уже надоело");
        ConvertsTesterFailure("dctulf dc` yjhvfkmyj yt hf,jnftn", "всегда всё нормально не работает");
        ConvertsTesterFailure("z yt gjybvf.", "я не понимаю");
        ConvertsTesterFailure("rfr ;t vyt", "как же мне");

        ConvertsLongContextRegressions();
        ConvertsPhysicalClustersAtomically();
        PreservesTrueContextBoundaries();

        Converts("dc`", "всё");
        Converts("e;t", "уже");
        Converts(";t", "же");
        Converts("t`", "её");
        Converts("могу kb я в таких", "могу ли я в таких");
        Converts("ns зачем то убрал", "ты зачем то убрал");
        Converts("арифметику bp урока", "арифметику из урока");
        Converts("жизни в dep ,дома", "жизни в вуз ,дома");
        Converts("gj 2r ifujd", "по 2к шагов");

        Unchanged("как дела");
        Unchanged("hello world");
        Unchanged("SQL HTTP API");
        Converts("SQL и ghbdtn", "SQL и привет");
        Unchanged("я use API");
        Unchanged("он исправил проблему с отображением уровня и названия задач с codwars");
        Converts("ghbdtn green banana rfr ltkf", "привет green banana как дела");
        Converts("source. ghbdtn rfr ltkf", "source. привет как дела");
        Unchanged("API работает");
        Unchanged("Use SQL and HTTP in this project");
        Unchanged("test@example.com");
        Unchanged("https://example.com");
        Converts("Моя почта test@example.com и ghbdtn", "Моя почта test@example.com и привет");
        Converts("ghbdtn,test@example.com", "привет,test@example.com");
        Converts("ghbdtn,https://example.com", "привет,https://example.com");

        Unchanged("Plan B works");
        Unchanged("Vitamin D is important");
        Unchanged("Press C to continue");
        Unchanged("R value");
        Unchanged("Z score");
        Unchanged("E coli");
        Unchanged("J curve");
        Unchanged("Section A");
        Unchanged("Option B");
        Unchanged("Drive C");
        Unchanged("Grade D");
        Converts("ntgthm vtyz! B", "теперь меня! B");

        Unchanged("Windows x64 build");
        Unchanged("version v2 is stable");
        Unchanged("B2 level");
        Unchanged("H2O molecule");
        Unchanged("Win32 API");
    }

    private void ConvertsLongContextRegressions()
    {
        Converts(
            "z yt gjybvf. gjxtve d hjccbb nfr ckj;yj gjkexbnm yjhvfkmye. hf,jne? gj rfrjq ghbxbyt vyt yt [jnzn lfdfnm yjhvfkmyjt j,jpjdfybt b gjxtve jyb gsnf.ncz cltkfnm vj. ;bpm [e;t bp lyz d ltym c rf;lsv lytv dc` [e;t b [e;t ?",
            "я не понимаю почему в россии так сложно получить нормальную работу, по какой причине мне не хотят давать нормальное обозование и почему они пытаются сделать мою жизь хуже из дня в день с каждым днем всё хуже и хуже ,");
        Converts(
            "z ,s [jntk gjghj,jdfnm gj;bnm d lheujq cnhfyt d yflt;lt yf kextt yflt.cm r vjtve dsgecre bp depf dc` d vbht cnfytn cbkmyj ghjot b dbpe gjkexbnm nj;t eltn cbkmyj ghjot ?nen ukfdyjt dthbnm d ecg[ b yfltznmcz yf kexitt/",
            "я бы хотел попробовать пожить в другой стране в надежде на лучее надеюсь к моему выпуску из вуза всё в мире станет сильно проще и визу получить тоже удет сильно проще ,тут главное верить в успх и надеяться на лучшее.");
        Converts("yf kextt yflt.cm", "на лучее надеюсь");
        Converts("d yflt;lt yf kextt yflt.cm r vjtve", "в надежде на лучее надеюсь к моему");
    }

    private void ConvertsPhysicalClustersAtomically()
    {
        Converts("cltkfnm vj. ;bpm [e;t bp lyz", "сделать мою жизь хуже из дня");
        Converts("vj. ;bpm [e;t", "мою жизь хуже");
        Converts("hf,jne? gj rfrjq ghbxbyt", "работу, по какой причине");
    }

    private void PreservesTrueContextBoundaries()
    {
        Unchanged("hello world. B");
        Unchanged("как дела. SQL");
        Converts("ntgthm vtyz! B", "теперь меня! B");
        Converts("ghbdtn\nhello", "привет\nhello");
    }

    private void ConvertsFullTesterCorpus()
    {
        Converts(
            "ntgthm e vtyz yfrjytwnj dc` hf,jnftn yjhvfkmyj?rjulf z 'njn nbycb crfxfk",
            "теперь у меня наконецто всё работает нормально,когда я этот тинси скачал");
        Converts(
            "vjue kb z d nfrb[ cgjhnbdys[ nhecf[ [jlbnm d j,sxyjq ;bpyb d dep ?ljvf [jlbnm gj 2r ifujd d ltym bkb cgfnm d yb[",
            "могу ли я в таких спортивных трусах ходить в обычной жизни в вуз ,дома ходить по 2к шагов в день или спать в них");
        Converts("gj'njve gecnm hf,jnftn", "поэтому пусть работает");
        Converts(
            "jy bcghfdbk ghj,ktve c jnj,hf;tybtv ehjdyz b yfpdfybz pflfx c codwars",
            "он исправил проблему с отображением уровня и названия задач с codwars");
        Converts(
            "gj bnjue e vtyz ,skj ghjqltyj 2 ntcnf f yt 4?nfr rfr dctuj dblbvj njkmrj 2 ntcnf/ lfdfq lfkmit",
            "по итогу у меня было пройдено 2 теста а не 4,так как всего видимо только 2 теста. давай дальше");
        Converts(
            "ns pfxtv nj e,hfk ldjbxye. fhbavtnbre bp ehjrf? lj,fdm t` j,hfnyj b dc` jr vj;yj ,eltn hf,jnfnm /",
            "ты зачем то убрал двоичную арифметику из урока, добавь её обратно и всё ок можно будет работать .");
        Converts(
            "rfr vyt e;t yfljtkj xnj gjcnjzyyj yjhvfkmyj yt hf,jnftn dc` 'nj ?dtlm dc` dctulf hf,jnftn yt nfr b 'njn afrn vtyz jxtym cbkmyj gtxfkbn",
            "как мне уже надоело что постоянно нормально не работает всё это ,ведь всё всегда работает не так и этот факт меня очень сильно печалит");
        Converts(
            "gjxtve e vtyz dctulf dc` yjhvfkmyj yt hf,jnftn/ gjxtve dctulf yflj ltkfnm dczrbq [kfv b yt gjkexfnm [jhjibq htpekmnfn? rfr nfr?",
            "почему у меня всегда всё нормально не работает. почему всегда надо делать всякий хлам и не получать хороший результат, как так,");
        Converts(
            "z yt gjybvf. gjxtve vjz ghjuhfvvf ctqxfc yt hf,jnftn b 'nj jxtym uhecnyj",
            "я не понимаю почему моя программа сейчас не работает и это очень грустно");
        Converts(
            "rfr ;t vyt yfljtkj xnj dc` gjcnjzyyj yt hf,jnftn yjhvfkmyj",
            "как же мне надоело что всё постоянно не работает нормально");
    }

    private void ConvertsRc1OutputCorpus()
    {
        Converts(
            "теперь у меня наконецто dc` работает нормально,когда я этот тинси скачал",
            "теперь у меня наконецто всё работает нормально,когда я этот тинси скачал");
        Converts(
            "могу kb я в таких спортивных трусах ходить в обычной жизни в dep ,дома ходить по 2r шагов в день или спать в них",
            "могу ли я в таких спортивных трусах ходить в обычной жизни в вуз ,дома ходить по 2к шагов в день или спать в них");
        Converts(
            "ns зачем то убрал двоичную арифметику bp урока, добавь t` обратно и dc` ок можно будет работать /",
            "ты зачем то убрал двоичную арифметику из урока, добавь её обратно и всё ок можно будет работать .");
        Converts(
            "как мне e;t надоело что постоянно нормально не работает dc` это ,ведь dc` всегда работает не так и этот факт меня очень сильно печалит",
            "как мне уже надоело что постоянно нормально не работает всё это ,ведь всё всегда работает не так и этот факт меня очень сильно печалит");
        Converts(
            "почему у меня всегда dc` нормально не работает. почему всегда надо делать всякий хлам и не получать хороший результат, как так,",
            "почему у меня всегда всё нормально не работает. почему всегда надо делать всякий хлам и не получать хороший результат, как так,");
        Converts(
            "как ;t мне надоело что dc` постоянно не работает нормально",
            "как же мне надоело что всё постоянно не работает нормально");
    }

    private void ConvertsTesterFailure(string input, string expected) => Converts(input, expected);

    private void Converts(string input, string expected) => Equal(expected, LayoutConverter.Convert(input));

    private void Unchanged(string input) => Equal(input, LayoutConverter.Convert(input));

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }
}
