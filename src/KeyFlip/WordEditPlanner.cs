namespace KeyFlip;

internal static class WordEditPlanner
{
    internal static ConversionResult Create(string text) =>
        LayoutConverter.ConvertWords(text, forceSingleToken: true);
}
