using MouseBD.Shared;

namespace MouseBD.Models;

public class AppSettings
{
    private const string KeyIp         = "server_ip";
    private const string KeyPort       = "server_port";
    private const string KeySensX      = "sensitivity_x";
    private const string KeySensY      = "sensitivity_y";
    private const string KeyScrollSens = "scroll_sensitivity";
    private const string KeyVibration  = "vibration";

    public string ServerIp   { get; set; } = string.Empty;
    public int    ServerPort { get; set; } = Protocol.DefaultPort;

    /// <summary>Mouse movement sensitivity multiplier.</summary>
    public float SensitivityX { get; set; } = 1.8f;
    public float SensitivityY { get; set; } = 1.8f;

    /// <summary>Scroll sensitivity multiplier.</summary>
    public float ScrollSensitivity { get; set; } = 1.0f;

    /// <summary>Whether to vibrate on button press.</summary>
    public bool Vibration { get; set; } = true;

    public static AppSettings Load()
    {
        return new AppSettings
        {
            ServerIp          = Preferences.Get(KeyIp, string.Empty),
            ServerPort        = Preferences.Get(KeyPort, Protocol.DefaultPort),
            SensitivityX      = Preferences.Get(KeySensX, 1.8f),
            SensitivityY      = Preferences.Get(KeySensY, 1.8f),
            ScrollSensitivity = Preferences.Get(KeyScrollSens, 1.0f),
            Vibration         = Preferences.Get(KeyVibration, true),
        };
    }

    public void Save()
    {
        Preferences.Set(KeyIp,         ServerIp);
        Preferences.Set(KeyPort,       ServerPort);
        Preferences.Set(KeySensX,      SensitivityX);
        Preferences.Set(KeySensY,      SensitivityY);
        Preferences.Set(KeyScrollSens, ScrollSensitivity);
        Preferences.Set(KeyVibration,  Vibration);
    }
}
