using MouseBD.Models;
using MouseBD.Services;

namespace MouseBD.Views;

public partial class TouchpadPage : ContentPage
{
    private readonly TouchpadService _service;
    private          AppSettings     _settings;

    private bool _scrollMode;

    public TouchpadPage(TouchpadService service)
    {
        InitializeComponent();
        _service  = service;
        _settings = AppSettings.Load();

        _service.OnConnectionChanged += OnConnectionChanged;
        _service.OnError             += OnServiceError;

        WireTouchSurface();
        AttachButtonGestures();

        if (!string.IsNullOrEmpty(_settings.ServerIp))
            _ = TryConnectAsync();
    }

    // =====================================================================
    // MultiTouchSurface events
    // =====================================================================

    private void WireTouchSurface()
    {
        TouchSurface.TouchStarted      += OnTouchStarted;
        TouchSurface.SingleTouchMoved  += OnSingleTouchMoved;
        TouchSurface.MultiTouchScrolled += OnMultiTouchScrolled;
        TouchSurface.SecondFingerDown  += OnSecondFingerDown;
        TouchSurface.TouchEnded        += OnTouchEnded;
    }

    private void OnTouchStarted()
    {
        _scrollMode = false;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RippleCircle.IsVisible    = true;
            ScrollIndicator.IsVisible = false;
        });
    }

    private void OnSingleTouchMoved(float dx, float dy)
    {
        if (!_service.IsConnected || _scrollMode) return;
        _service.SendMove(dx * _settings.SensitivityX, dy * _settings.SensitivityY);
    }

    private void OnSecondFingerDown()
    {
        _scrollMode = true;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScrollIndicator.Text      = "↕ Przewijanie";
            ScrollIndicator.IsVisible = true;
        });
    }

    private void OnMultiTouchScrolled(float dx, float dy)
    {
        if (!_service.IsConnected) return;
        // Invert Y for natural scrolling, scale down
        _service.SendScroll(
            dx * _settings.ScrollSensitivity * -0.04f,
            dy * _settings.ScrollSensitivity * -0.04f);
    }

    private void OnTouchEnded(bool isTap)
    {
        _scrollMode = false;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RippleCircle.IsVisible    = false;
            ScrollIndicator.IsVisible = false;
            ScrollIndicator.Text      = string.Empty;
        });

        if (isTap && _service.IsConnected)
            _ = SimulateTapAsync();
    }

    // =====================================================================
    // Button gestures
    // =====================================================================

    private void AttachButtonGestures()
    {
        var leftTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        leftTap.Tapped += (_, _) =>
        {
            Vibrate();
            _ = SimulateButtonAsync(_service.SendLeftDown, _service.SendLeftUp);
        };
        LeftBtn.GestureRecognizers.Add(leftTap);

        var rightTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        rightTap.Tapped += (_, _) =>
        {
            Vibrate();
            _ = SimulateButtonAsync(_service.SendRightDown, _service.SendRightUp);
        };
        RightBtn.GestureRecognizers.Add(rightTap);

        var middleTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        middleTap.Tapped += (_, _) =>
        {
            Vibrate();
            _ = SimulateButtonAsync(_service.SendMiddleDown, _service.SendMiddleUp);
        };
        MiddleBtn.GestureRecognizers.Add(middleTap);
    }

    // =====================================================================
    // Connection
    // =====================================================================

    private async Task TryConnectAsync()
    {
        UpdateStatus("Łączenie...", "#f0a500");
        bool ok = await _service.ConnectAsync(_settings);
        if (!ok)
            UpdateStatus("Rozłączono", "#f85149");
    }

    private void OnConnectionChanged(bool connected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (connected)
            {
                UpdateStatus($"Połączono: {_settings.ServerIp}", "#3fb950");
                HintPanel.IsVisible = false;
            }
            else
            {
                UpdateStatus("Rozłączono", "#f85149");
                HintPanel.IsVisible = true;
            }
        });
    }

    private void OnServiceError(string msg)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
            await DisplayAlert("Błąd", msg, "OK"));
    }

    private void UpdateStatus(string text, string colorHex)
    {
        StatusLabel.Text = text;
        StatusDot.Fill   = new SolidColorBrush(Color.FromArgb(colorHex));
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private async Task SimulateTapAsync()
    {
        Vibrate();
        _service.SendLeftDown();
        await Task.Delay(30);
        _service.SendLeftUp();
    }

    private static async Task SimulateButtonAsync(Action down, Action up)
    {
        down();
        await Task.Delay(50);
        up();
    }

    private void Vibrate()
    {
        if (!_settings.Vibration) return;
        try { HapticFeedback.Perform(HapticFeedbackType.Click); }
        catch { }
    }

    // =====================================================================
    // Navigation
    // =====================================================================

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SettingsPage));

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _settings = AppSettings.Load();
        if (!_service.IsConnected && !string.IsNullOrEmpty(_settings.ServerIp))
            _ = TryConnectAsync();
    }
}
