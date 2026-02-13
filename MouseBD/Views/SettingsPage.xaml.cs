using MouseBD.Models;
using MouseBD.Services;
using MouseBD.Shared;

namespace MouseBD.Views;

public partial class SettingsPage : ContentPage
{
    private readonly TouchpadService _service;
    private          AppSettings     _settings;

    public SettingsPage(TouchpadService service)
    {
        InitializeComponent();
        _service  = service;
        _settings = AppSettings.Load();

        _service.OnConnectionChanged += OnConnectionChanged;
        _service.OnError             += OnServiceError;

        LoadUi();
    }

    private void LoadUi()
    {
        IpEntry.Text            = _settings.ServerIp;
        PortEntry.Text          = _settings.ServerPort.ToString();
        SensSlider.Value        = _settings.SensitivityX;
        ScrollSensSlider.Value  = _settings.ScrollSensitivity;
        VibrationSwitch.IsToggled = _settings.Vibration;
        SensLabel.Text          = _settings.SensitivityX.ToString("F1");
        ScrollSensLabel.Text    = _settings.ScrollSensitivity.ToString("F1");

        UpdateConnStatus(_service.IsConnected);
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        SaveSettings();

        if (_service.IsConnected)
        {
            _service.Disconnect();
            ConnectBtn.Text = "Połącz";
            return;
        }

        ConnectBtn.IsEnabled      = false;
        ConnectBtn.Text           = "Łączenie...";
        ConnStatusLabel.Text      = "";

        bool ok = await _service.ConnectAsync(_settings);

        ConnectBtn.IsEnabled = true;
        ConnectBtn.Text      = ok ? "Rozłącz" : "Połącz";
        UpdateConnStatus(ok);
    }

    private void OnConnectionChanged(bool connected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateConnStatus(connected);
            ConnectBtn.Text = connected ? "Rozłącz" : "Połącz";
        });
    }

    private void OnServiceError(string msg)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ConnStatusLabel.Text      = msg;
            ConnStatusLabel.TextColor = Color.FromArgb("#f85149");
        });
    }

    private void UpdateConnStatus(bool connected)
    {
        ConnStatusLabel.Text      = connected ? $"✓ Połączono z {_settings.ServerIp}" : "";
        ConnStatusLabel.TextColor = Color.FromArgb(connected ? "#3fb950" : "#f85149");
    }

    private void OnSensChanged(object? sender, ValueChangedEventArgs e)
    {
        SensLabel.Text = e.NewValue.ToString("F1");
    }

    private void OnScrollSensChanged(object? sender, ValueChangedEventArgs e)
    {
        ScrollSensLabel.Text = e.NewValue.ToString("F1");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        SaveSettings();
        await Shell.Current.GoToAsync("..");
    }

    private void SaveSettings()
    {
        _settings.ServerIp          = IpEntry.Text?.Trim() ?? string.Empty;
        _settings.SensitivityX      = (float)SensSlider.Value;
        _settings.SensitivityY      = (float)SensSlider.Value;
        _settings.ScrollSensitivity = (float)ScrollSensSlider.Value;
        _settings.Vibration         = VibrationSwitch.IsToggled;

        if (int.TryParse(PortEntry.Text, out int port) && port is >= 1 and <= 65535)
            _settings.ServerPort = port;
        else
            _settings.ServerPort = Protocol.DefaultPort;

        _settings.Save();
    }

    protected override bool OnBackButtonPressed()
    {
        SaveSettings();
        return base.OnBackButtonPressed();
    }
}
