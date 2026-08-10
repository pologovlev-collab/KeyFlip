namespace KeyFlip;

internal static class WordEditPlanner
{
    internal static ConversionResult Create(string text) =>
        LayoutConverter.ConvertTargeted(text, forceSingleToken: true);
}
