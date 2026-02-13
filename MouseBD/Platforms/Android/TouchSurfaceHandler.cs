using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using MouseBD.Controls;
using AView = Android.Views.View;

namespace MouseBD.Platforms.Android;

/// <summary>
/// Android handler for MultiTouchSurface.
/// Creates a native View that handles raw multi-touch MotionEvents directly,
/// bypassing MAUI gesture recognizers for accurate 2-finger scroll detection.
/// </summary>
public class MultiTouchSurfaceHandler
    : ViewHandler<MultiTouchSurface, AView>
{
    public static IPropertyMapper<MultiTouchSurface, MultiTouchSurfaceHandler> Mapper =
        new PropertyMapper<MultiTouchSurface, MultiTouchSurfaceHandler>(ViewMapper);

    public MultiTouchSurfaceHandler() : base(Mapper) { }

    protected override AView CreatePlatformView()
        => new TouchNativeView(Context!, VirtualView);

    // ---------------------------------------------------------------

    private sealed class TouchNativeView : AView
    {
        private readonly MultiTouchSurface _surface;

        private float _lastX, _lastY;
        private float _startX, _startY;
        private long  _touchDownMs;

        private float _lastMidX, _lastMidY;
        private bool  _scrollMode;

        private const int   TapMaxMs = 200;
        private const float TapMaxPx = 40f;

        public TouchNativeView(Context context, MultiTouchSurface surface)
            : base(context)
        {
            _surface = surface;
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null) return false;

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    _lastX       = e.GetX(0);
                    _lastY       = e.GetY(0);
                    _startX      = _lastX;
                    _startY      = _lastY;
                    _touchDownMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _scrollMode  = false;
                    _surface.RaiseTouchStarted();
                    break;

                case MotionEventActions.PointerDown when e.PointerCount == 2:
                    _lastMidX   = (e.GetX(0) + e.GetX(1)) / 2f;
                    _lastMidY   = (e.GetY(0) + e.GetY(1)) / 2f;
                    _scrollMode = true;
                    _surface.RaiseSecondFingerDown();
                    break;

                case MotionEventActions.Move:
                    if (_scrollMode && e.PointerCount >= 2)
                    {
                        float midX = (e.GetX(0) + e.GetX(1)) / 2f;
                        float midY = (e.GetY(0) + e.GetY(1)) / 2f;
                        _surface.RaiseMultiTouchScrolled(midX - _lastMidX, midY - _lastMidY);
                        _lastMidX = midX;
                        _lastMidY = midY;
                    }
                    else if (!_scrollMode && e.PointerCount == 1)
                    {
                        float x = e.GetX(0);
                        float y = e.GetY(0);
                        _surface.RaiseSingleTouchMoved(x - _lastX, y - _lastY);
                        _lastX = x;
                        _lastY = y;
                    }
                    break;

                case MotionEventActions.PointerUp when e.PointerCount == 2:
                    _scrollMode = false;
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    long  elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _touchDownMs;
                    float dx      = e.GetX(0) - _startX;
                    float dy      = e.GetY(0) - _startY;
                    float dist    = MathF.Sqrt(dx * dx + dy * dy);
                    bool  isTap   = elapsed < TapMaxMs && dist < TapMaxPx && !_scrollMode;
                    _scrollMode   = false;
                    _surface.RaiseTouchEnded(isTap);
                    break;
            }

            return true;
        }
    }
}
