using System.Globalization;

namespace LinqToBlueSky.Tests.Common;

public class TestCulture
{
    public static void SetCulture()
    {
        string culture = string.Empty;
        CultureInfo cultureInfo = new(culture);
        CultureInfo.CurrentCulture = cultureInfo;
    }
}
