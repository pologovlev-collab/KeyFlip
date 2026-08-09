namespace KeyFlip;

internal static class FileNameConverter
{
    internal static string ConvertForRename(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var lastDot = fileName.LastIndexOf('.');
        if (lastDot == 0) return fileName;
        if (lastDot < 0) return ConvertBaseName(fileName);

        var baseName = fileName[..lastDot];
        return ConvertBaseName(baseName) + fileName[lastDot..];
    }

    private static string ConvertBaseName(string baseName) => string.Join(
        '.',
        baseName.Split('.').Select(static segment => LayoutConverter.ConvertWords(
            segment,
            forceSingleToken: false).OutputText));
}
