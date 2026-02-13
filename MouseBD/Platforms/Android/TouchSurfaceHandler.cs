using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using MouseBD.Services;
using MouseBD.Views;
using AView = Android.Views.View;

namespace MouseBD.Platforms.Android;

/// <summary>
/// Custom handler for the touchpad surface that gives us raw multi-touch events,
/// bypassing MAUI gesture recognizers for 2-finger scroll detection.
/// </summary>
public class TouchSurfaceHandler : BoxViewHandler
{
    private TouchpadPage? _touchpadPage;

    protected override void ConnectHandler(AppCompatBoxView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Touch += OnTouch;
    }

    protected override void DisconnectHandler(AppCompatBoxView platformView)
    {
        platformView.Touch -= OnTouch;
        base.DisconnectHandler(platformView);
    }

    private void OnTouch(object? sender, AView.TouchEventArgs e)
    {
        var ev = e.Event;
        if (ev == null) return;

        e.Handled = true;

        // Route multi-touch to the page
        if (_touchpadPage != null)
            HandleMultiTouch(ev);
    }

    private float _prevMidX, _prevMidY;
    private bool  _wasMultiTouch;

    private void HandleMultiTouch(MotionEvent ev)
    {
        int pointerCount = ev.PointerCount;

        switch (ev.ActionMasked)
        {
            case MotionEventActions.PointerDown when pointerCount == 2:
                // Second finger landed - switch to scroll mode
                _prevMidX = (ev.GetX(0) + ev.GetX(1)) / 2f;
                _prevMidY = (ev.GetY(0) + ev.GetY(1)) / 2f;
                _wasMultiTouch = true;
                _touchpadPage?.ActivateScrollMode();
                break;

            case MotionEventActions.Move when pointerCount >= 2 && _wasMultiTouch:
                float midX = (ev.GetX(0) + ev.GetX(1)) / 2f;
                float midY = (ev.GetY(0) + ev.GetY(1)) / 2f;

                // Get service from page (accessed via DI root)
                // The scroll is handled in TouchpadPage.ActivateScrollMode path
                _prevMidX = midX;
                _prevMidY = midY;
                break;

            case MotionEventActions.PointerUp:
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                _wasMultiTouch = false;
                break;
        }
    }

    public void SetTouchpadPage(TouchpadPage page) => _touchpadPage = page;
}
