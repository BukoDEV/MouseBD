using System.Runtime.InteropServices;

namespace MouseBD.Server;

/// <summary>
/// Controls the Windows mouse cursor using SendInput (lowest latency Win32 API).
/// </summary>
public static class MouseController
{
    // ---- Win32 Interop ----

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const int INPUT_MOUSE    = 0;
    private const uint MOUSEEVENTF_MOVE       = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN   = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP     = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN  = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP    = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP   = 0x0040;
    private const uint MOUSEEVENTF_WHEEL      = 0x0800;
    private const uint MOUSEEVENTF_HWHEEL     = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int    dx;
        public int    dy;
        public uint   mouseData;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }

    // ---- Public API ----

    /// <summary>Moves the cursor by a relative delta (pixels).</summary>
    public static void MoveDelta(float dx, float dy)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dx      = (int)dx,
                dy      = (int)dy,
                dwFlags = MOUSEEVENTF_MOVE,
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    /// <summary>Scrolls the mouse wheel vertically.</summary>
    public static void ScrollVertical(float delta)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                mouseData = (uint)(int)(delta * 120),
                dwFlags   = MOUSEEVENTF_WHEEL,
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    /// <summary>Scrolls the mouse wheel horizontally.</summary>
    public static void ScrollHorizontal(float delta)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                mouseData = (uint)(int)(delta * 120),
                dwFlags   = MOUSEEVENTF_HWHEEL,
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    public static void LeftDown()   => SendButton(MOUSEEVENTF_LEFTDOWN);
    public static void LeftUp()     => SendButton(MOUSEEVENTF_LEFTUP);
    public static void RightDown()  => SendButton(MOUSEEVENTF_RIGHTDOWN);
    public static void RightUp()    => SendButton(MOUSEEVENTF_RIGHTUP);
    public static void MiddleDown() => SendButton(MOUSEEVENTF_MIDDLEDOWN);
    public static void MiddleUp()   => SendButton(MOUSEEVENTF_MIDDLEUP);

    private static void SendButton(uint flags)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi   = new MOUSEINPUT { dwFlags = flags }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }
}
