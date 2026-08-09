namespace KeyFlip;

internal static class FileNameConverter
{
    internal static string ConvertForRename(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var lastDot = fileName.LastIndexOf('.');
        if (lastDot == 0) return fileName;
        if (lastDot < 0) return LayoutConverter.Convert(fileName);
        return LayoutConverter.Convert(fileName[..lastDot]) + fileName[lastDot..];
    }
}
