using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;

namespace SonarFlyout;

static class Strings
{
    static readonly ResourceLoader s_loader = new();

    public static string Get(string key) => s_loader.GetString(key);

    public static string Format(string key, object arg) => string.Format(CultureInfo.CurrentCulture, Get(key), arg);

    public static bool IsRightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    public static FlowDirection FlowDirection => IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
