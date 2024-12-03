using System.Globalization;

namespace LinqToBlueSky;

public class Culture
{
    static CultureInfo? usCulture;

    public static CultureInfo US
    {
        get
        {
            if (usCulture == null)
                usCulture = new CultureInfo("en-US");

            return usCulture;
        }
    }
}
