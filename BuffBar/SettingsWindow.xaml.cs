using System.Globalization;
using System.Windows;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar;

/// <summary>
/// Fenêtre Paramètres : édite settings.json (thème, hauteur, ville, mode jeu,
/// widgets activables, OBS) et applique les changements en direct.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Load(ConfigService.Current);
    }

    private void Load(Config c)
    {
        ThemeBuff.IsChecked = c.Theme == "buff";
        ThemeWindows.IsChecked = c.Theme == "windows";
        ThemeCyber.IsChecked = c.Theme == "cyber";
        if (c.Theme is not ("buff" or "windows" or "cyber"))
            ThemeBuff.IsChecked = true;

        HeightBox.Text = c.Height.ToString(CultureInfo.InvariantCulture);
        CityBox.Text = c.WeatherCity;

        GamingBox.IsChecked = c.GamingMode;
        ExtAccentBox.IsChecked = c.ExternalAccent;
        AcrylicBox.IsChecked = c.Acrylic;

        WWeather.IsChecked = c.Widgets.Weather;
        WUptime.IsChecked = c.Widgets.Uptime;
        WNetwork.IsChecked = c.Widgets.Network;
        WMedia.IsChecked = c.Widgets.Media;
        WObs.IsChecked = c.Widgets.Obs;
        WVisualizer.IsChecked = c.Widgets.Visualizer;
        WVolume.IsChecked = c.Widgets.Volume;
        WBluetooth.IsChecked = c.Widgets.Bluetooth;
        WBattery.IsChecked = c.Widgets.Battery;

        ObsHostBox.Text = c.Obs.Host;
        ObsPortBox.Text = c.Obs.Port.ToString(CultureInfo.InvariantCulture);
        ObsPassBox.Text = c.Obs.Password;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var c = new Config
        {
            Theme = ThemeCyber.IsChecked == true ? "cyber"
                  : ThemeWindows.IsChecked == true ? "windows"
                  : "buff",
            Height = ParseDouble(HeightBox.Text, 36, 24, 96),
            WeatherCity = string.IsNullOrWhiteSpace(CityBox.Text) ? "Terrebonne" : CityBox.Text.Trim(),
            GamingMode = GamingBox.IsChecked == true,
            ExternalAccent = ExtAccentBox.IsChecked == true,
            Acrylic = AcrylicBox.IsChecked == true,
            Widgets = new WidgetToggles
            {
                Weather = WWeather.IsChecked == true,
                Uptime = WUptime.IsChecked == true,
                Network = WNetwork.IsChecked == true,
                Media = WMedia.IsChecked == true,
                Obs = WObs.IsChecked == true,
                Visualizer = WVisualizer.IsChecked == true,
                Volume = WVolume.IsChecked == true,
                Bluetooth = WBluetooth.IsChecked == true,
                Battery = WBattery.IsChecked == true
            },
            Obs = new ObsConfig
            {
                Host = string.IsNullOrWhiteSpace(ObsHostBox.Text) ? "127.0.0.1" : ObsHostBox.Text.Trim(),
                Port = (int)ParseDouble(ObsPortBox.Text, 4455, 1, 65535),
                Password = ObsPassBox.Text
            }
        };

        ConfigService.Save(c);
        (Application.Current as App)?.ApplyConfigAndRestart();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private static double ParseDouble(string s, double fallback, double min, double max)
    {
        if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
            && !double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return fallback;
        return v < min ? min : v > max ? max : v;
    }
}
