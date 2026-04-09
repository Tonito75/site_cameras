using System.Globalization;
using System.Text.RegularExpressions;

namespace PortalCameras.Helpers;

public static class FavouriteHelper
{
    private static readonly Regex DatePattern = new(@"_(\d{14})\.\w+$", RegexOptions.Compiled);

    public static DateTime ParsePhotoDate(string relativePath, DateTime fallback)
    {
        var match = DatePattern.Match(relativePath);
        if (!match.Success) return fallback;

        return DateTime.TryParseExact(
            match.Groups[1].Value,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date) ? date : fallback;
    }
}
