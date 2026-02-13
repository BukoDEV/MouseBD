namespace MouseBD.Controls;

/// <summary>
/// Cross-platform touchpad surface view.
/// Platform-specific handlers wire up native touch events to these callbacks.
/// </summary>
public class MultiTouchSurface : View
{
    /// <summary>Fired when a single finger moves. Args: (dx, dy) in pixels.</summary>
    public event Action<float, float>? SingleTouchMoved;

    /// <summary>Fired when two fingers move (scroll gesture). Args: (dx, dy) in pixels.</summary>
    public event Action<float, float>? MultiTouchScrolled;

    /// <summary>Fired on finger down (first touch).</summary>
    public event Action? TouchStarted;

    /// <summary>Fired on finger up. isTap=true when gesture was a short tap in place.</summary>
    public event Action<bool>? TouchEnded;

    /// <summary>Fired when a second finger lands (entering scroll mode).</summary>
    public event Action? SecondFingerDown;

    // Called by platform handlers
    internal void RaiseSingleTouchMoved(float dx, float dy)  => SingleTouchMoved?.Invoke(dx, dy);
    internal void RaiseMultiTouchScrolled(float dx, float dy) => MultiTouchScrolled?.Invoke(dx, dy);
    internal void RaiseTouchStarted()                         => TouchStarted?.Invoke();
    internal void RaiseTouchEnded(bool isTap)                 => TouchEnded?.Invoke(isTap);
    internal void RaiseSecondFingerDown()                     => SecondFingerDown?.Invoke();
}
