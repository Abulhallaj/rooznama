using Windows.UI.ViewManagement;

namespace Rooznama.Services;

public static class ThemeHelper
{
    public static bool IsDark() => App.Settings.Theme switch
    {
        "dark" => true,
        "light" => false,
        _ => new UISettings().GetColorValue(UIColorType.Background).R < 128
    };
}
