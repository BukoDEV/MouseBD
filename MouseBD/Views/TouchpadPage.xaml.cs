using MouseBD.Models;
using MouseBD.Services;

namespace MouseBD.Views;

public partial class TouchpadPage : ContentPage
{
    private readonly TouchpadService _service;
    private          AppSettings     _settings;

    // --- Touch tracking ---
    private float _lastX, _lastY;
    private bool  _tracking;
    private int   _fingerCount;
    private long  _touchStartMs;

    // Two-finger scroll state
    private float _lastScrollX, _lastScrollY;
    private bool  _scrollMode;

    // Tap detection
    private const int TapMaxMs   = 200;
    private const float TapMaxMv = 15f; // max pixels moved to count as tap
    private float _startX, _startY;

    // Settings icon path
    private static readonly string SettingsIcon = "settings.png";

    public TouchpadPage(TouchpadService service)
    {
        InitializeComponent();
        _service  = service;
        _settings = AppSettings.Load();

        _service.OnConnectionChanged += OnConnectionChanged;
        _service.OnError             += OnServiceError;

        AttachGestures();
        AttachButtonGestures();

        // Auto-connect on startup if IP is saved
        if (!string.IsNullOrEmpty(_settings.ServerIp))
            _ = TryConnectAsync();
    }

    // =====================================================================
    // Gesture handling
    // =====================================================================

    private void AttachGestures()
    {
        var recognizer = new PointerGestureRecognizer();
        recognizer.PointerMoved   += OnPointerMoved;
        recognizer.PointerPressed  += OnPointerPressed;
        recognizer.PointerReleased += OnPointerReleased;

        // Multi-touch via PanGestureRecognizer won't give multi-finger,
        // so we use the Android-specific touch handler via custom handler
        // For cross-platform we use the built-in approach with PanGesture
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        TouchSurface.GestureRecognizers.Add(pan);

        // Also add touch-specific for Android
        TouchSurface.GestureRecognizers.Add(recognizer);
    }

    // PanGestureRecognizer is the standard MAUI way for tracking drags
    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!_service.IsConnected) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastX = (float)e.TotalX;
                _lastY = (float)e.TotalY;
                _startX = _lastX;
                _startY = _lastY;
                _touchStartMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _tracking = true;
                _scrollMode = false;
                ShowRipple(0, 0);
                break;

            case GestureStatus.Running:
                if (!_tracking) break;

                float currX = (float)e.TotalX;
                float currY = (float)e.TotalY;
                float dx    = currX - _lastX;
                float dy    = currY - _lastY;
                _lastX = currX;
                _lastY = currY;

                // Two-finger scroll detection heuristic: if pan is very vertical
                // and user started with two fingers (rough heuristic via speed)
                if (_scrollMode)
                {
                    float sdx = (currX - _lastScrollX);
                    float sdy = (currY - _lastScrollY);
                    _service.SendScroll(
                        sdx * _settings.ScrollSensitivity * -0.03f,
                        sdy * _settings.ScrollSensitivity * -0.03f);
                    _lastScrollX = currX;
                    _lastScrollY = currY;
                }
                else
                {
                    _service.SendMove(
                        dx * _settings.SensitivityX,
                        dy * _settings.SensitivityY);
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_tracking)
                {
                    long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _touchStartMs;
                    float moved  = MathF.Sqrt(
                        MathF.Pow((float)e.TotalX - _startX, 2) +
                        MathF.Pow((float)e.TotalY - _startY, 2));

                    // Tap detection
                    if (elapsed < TapMaxMs && moved < TapMaxMv && !_scrollMode)
                    {
                        _ = SimulateTapAsync();
                    }

                    _tracking   = false;
                    _scrollMode = false;
                    HideRipple();
                }
                break;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e) { }
    private void OnPointerPressed(object? sender, PointerEventArgs e) { }
    private void OnPointerReleased(object? sender, PointerEventArgs e) { }

    private void AttachButtonGestures()
    {
        // Left button
        var leftTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        leftTap.Tapped += (_, _) =>
        {
            Vibrate();
            _ = SimulateButtonAsync(_service.SendLeftDown, _service.SendLeftUp);
        };
        LeftBtn.GestureRecognizers.Add(leftTap);

        // Right button
        var rightTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        rightTap.Tapped += (_, _) =>
        {
            Vibrate();
            _ = SimulateButtonAsync(_service.SendRightDown, _service.SendRightUp);
        };
        RightBtn.GestureRecognizers.Add(rightTap);

        // Middle button
        var middleTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        middleTap.Tapped += (_, _) =>
        {
            Vibrate();
            _ = SimulateButtonAsync(_service.SendMiddleDown, _service.SendMiddleUp);
        };
        MiddleBtn.GestureRecognizers.Add(middleTap);
    }

    // Scroll mode toggled via two-finger long press gesture
    public void ActivateScrollMode()
    {
        _scrollMode  = true;
        _lastScrollX = _lastX;
        _lastScrollY = _lastY;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScrollIndicator.Text      = "↕ Tryb przewijania";
            ScrollIndicator.IsVisible = true;
        });
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
        {
            await DisplayAlert("Błąd", msg, "OK");
        });
    }

    private void UpdateStatus(string text, string colorHex)
    {
        StatusLabel.Text = text;
        StatusDot.Fill   = new SolidColorBrush(Color.FromArgb(colorHex));
    }

    // =====================================================================
    // Button / tap helpers
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

    // =====================================================================
    // Visual feedback
    // =====================================================================

    private void ShowRipple(double x, double y)
    {
        RippleCircle.IsVisible        = true;
        RippleCircle.TranslationX     = x - 30;
        RippleCircle.TranslationY     = y - 30;
    }

    private void HideRipple()
    {
        RippleCircle.IsVisible        = false;
        ScrollIndicator.IsVisible     = false;
        ScrollIndicator.Text          = string.Empty;
    }

    private void Vibrate()
    {
        if (!_settings.Vibration) return;
        try { HapticFeedback.Perform(HapticFeedbackType.Click); }
        catch { /* device may not support */ }
    }

    // =====================================================================
    // Navigation
    // =====================================================================

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reload settings when returning from settings page
        _settings = AppSettings.Load();

        if (!_service.IsConnected && !string.IsNullOrEmpty(_settings.ServerIp))
            _ = TryConnectAsync();
    }
}
