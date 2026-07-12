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
        HeightBox.Text = c.Height.ToString(CultureInfo.InvariantCulture);
        CityBox.Text = c.WeatherCity;

        GamingBox.IsChecked = c.GamingMode;
        AcrylicBox.IsChecked = c.Acrylic;
        IncludeScreenshotsBox.IsChecked = c.IncludeInScreenshots;

        SysPrimary.IsChecked = c.SystemIndicatorsScope == "primary";
        SysAll.IsChecked = c.SystemIndicatorsScope == "all";
        // « Externe » par défaut, y compris pour une valeur inconnue.
        SysExternal.IsChecked = SysPrimary.IsChecked != true && SysAll.IsChecked != true;

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

        GoogleEnabled.IsChecked = c.GoogleCalendar.Enabled;
        GoogleClientId.Text = c.GoogleCalendar.ClientId;
        GoogleClientSecret.Text = c.GoogleCalendar.ClientSecret;
        UpdateGoogleStatus();
    }

    private void UpdateGoogleStatus()
    {
        bool connected = GoogleCalendarService.IsConnected;
        GoogleStatus.Text = connected ? "✔ Connecté" : "Non connecté";
        GoogleStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            connected ? System.Windows.Media.Color.FromRgb(0xDD, 0xFF, 0x24)
                      : System.Windows.Media.Color.FromRgb(0x90, 0x90, 0x90));
    }

    private async void OnGoogleConnect(object sender, RoutedEventArgs e)
    {
        // Persiste d'abord les identifiants (pour que le rafraîchissement du jeton fonctionne).
        Config c = ConfigService.Current;
        c.GoogleCalendar.Enabled = GoogleEnabled.IsChecked == true;
        c.GoogleCalendar.ClientId = GoogleClientId.Text.Trim();
        c.GoogleCalendar.ClientSecret = GoogleClientSecret.Text.Trim();
        ConfigService.Save(c);

        if (string.IsNullOrWhiteSpace(c.GoogleCalendar.ClientId))
        {
            GoogleStatus.Text = "Client ID requis";
            return;
        }

        GoogleConnectButton.IsEnabled = false;
        GoogleStatus.Text = "Connexion… (voir le navigateur)";
        bool ok = await GoogleCalendarService.ConnectAsync(
            c.GoogleCalendar.ClientId, c.GoogleCalendar.ClientSecret);
        GoogleConnectButton.IsEnabled = true;

        if (!ok) GoogleStatus.Text = "Échec de la connexion";
        else UpdateGoogleStatus();
    }

    private void OnGoogleDisconnect(object sender, RoutedEventArgs e)
    {
        GoogleCalendarService.Disconnect();
        UpdateGoogleStatus();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var c = new Config
        {
            Height = ParseDouble(HeightBox.Text, 36, 24, 96),
            WeatherCity = string.IsNullOrWhiteSpace(CityBox.Text) ? "Terrebonne" : CityBox.Text.Trim(),
            GamingMode = GamingBox.IsChecked == true,
            Acrylic = AcrylicBox.IsChecked == true,
            IncludeInScreenshots = IncludeScreenshotsBox.IsChecked == true,
            SystemIndicatorsScope = SelectedSysScope(),
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
            },
            GoogleCalendar = new GoogleCalendarConfig
            {
                Enabled = GoogleEnabled.IsChecked == true,
                ClientId = GoogleClientId.Text.Trim(),
                ClientSecret = GoogleClientSecret.Text.Trim(),
                MaxEvents = ConfigService.Current.GoogleCalendar.MaxEvents
            }
        };

        ConfigService.Save(c);
        (Application.Current as App)?.ApplyConfigAndRestart();
        Close();
    }

    private string SelectedSysScope()
    {
        if (SysPrimary.IsChecked == true) return "primary";
        if (SysAll.IsChecked == true) return "all";
        return "external";
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
