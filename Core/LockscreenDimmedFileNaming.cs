namespace LockscreenGif.Core;

/// <summary>
/// Pure logic for working out which "dimmed" lockscreen cache file names need to be
/// overwritten with the user's GIF. Kept free of file-system/registry access so it can
/// be unit tested; callers supply the display resolutions and any already-existing
/// dimmed file names they discovered on disk.
/// </summary>
public static class LockscreenDimmedFileNaming
{
    public const string DimmedSuffix = "_notdimmed.jpg";
    public const string DimmedBaseName = "LockScreen.jpg";

    public static IReadOnlySet<string> BuildDimmedFileNames(
        IEnumerable<string> displayResolutions,
        IEnumerable<string> existingDimmedFileNames)
    {
        var dests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resolution in displayResolutions)
        {
            dests.Add($"LockScreen___{resolution}{DimmedSuffix}");
        }

        foreach (var existing in existingDimmedFileNames)
        {
            if (!string.IsNullOrWhiteSpace(existing))
            {
                dests.Add(existing);
            }
        }

        return dests;
    }
}
