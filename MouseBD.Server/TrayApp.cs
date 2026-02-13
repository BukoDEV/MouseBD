using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MouseBD.Server;

/// <summary>
/// System tray application - sits in the notification area and shows status.
/// </summary>
public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon       _tray;
    private readonly UdpServer        _server;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _ipItem;

    public TrayApp()
    {
        _server = new UdpServer();
        _server.OnLog             += msg => _statusItem.Text = msg;
        _server.OnClientConnected += ip  => UpdateStatus($"Połączono: {ip}");

        _statusItem = new ToolStripMenuItem("Oczekiwanie...") { Enabled = false };
        _ipItem     = new ToolStripMenuItem(GetPrimaryIpDisplay()) { Enabled = false };

        _menu = new ContextMenuStrip();
        _menu.Items.Add(new ToolStripMenuItem("MouseBD Server") { Enabled = false, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_ipItem);
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Pokaż IP...",       null, (_, _) => ShowStatus());
        _menu.Items.Add("Włącz tryb USB",    null, (_, _) => EnableUsbMode());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Zamknij",           null, (_, _) => ExitThread());

        _tray = new NotifyIcon
        {
            Text             = "MouseBD Server",
            Icon             = SystemIcons.Application,
            Visible          = true,
            ContextMenuStrip = _menu,
        };
        _tray.DoubleClick += (_, _) => ShowStatus();

        _server.Start();
        UpdateStatus("Serwer uruchomiony");
        _tray.ShowBalloonTip(3000, "MouseBD", $"Nasłuchuję na:\n{GetAllIpsFormatted()}", ToolTipIcon.Info);
    }

    private void UpdateStatus(string msg)
    {
        _statusItem.Text = msg;
        _tray.Text = $"MouseBD - {msg}"[..Math.Min(63, $"MouseBD - {msg}".Length)];
    }

    private void ShowStatus()
    {
        var ips = GetAllLocalIps();
        var ipLines = ips.Count > 0
            ? string.Join("\n", ips.Select(x => $"  {x.Name}: {x.Ip}"))
            : "  (nie znaleziono)";

        MessageBox.Show(
            $"Adresy IP:\n{ipLines}\n\n" +
            $"Port: {MouseBD.Shared.Protocol.DefaultPort}\n\n" +
            "Podaj odpowiedni adres IP w aplikacji na telefonie.\n" +
            "Jeśli jest kilka adresów, wybierz ten z sieci WiFi (np. 192.168.x.x).\n\n" +
            "Polaczenie USB: kliknij 'Wlącz tryb USB' w menu,\n" +
            "a w aplikacji wpisz 127.0.0.1.",
            "MouseBD Server – Informacje o połączeniu",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ── USB / ADB ──────────────────────────────────────────────────────────

    private void EnableUsbMode()
    {
        var adb = FindAdb();
        if (adb is null)
        {
            MessageBox.Show(
                "Nie znaleziono adb.exe.\n\n" +
                "Zainstaluj Android SDK Platform Tools i upewnij się,\n" +
                "że adb.exe jest dostępne w zmiennej PATH.\n\n" +
                "Możesz też pobrać samo narzędzie z:\n" +
                "https://developer.android.com/studio/releases/platform-tools",
                "Tryb USB – brak ADB",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        int port = MouseBD.Shared.Protocol.DefaultPort;
        try
        {
            var psi = new ProcessStartInfo(adb, $"reverse tcp:{port} tcp:{port}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            using var proc = Process.Start(psi)!;
            proc.WaitForExit(5000);
            string output = proc.StandardOutput.ReadToEnd().Trim();
            string error  = proc.StandardError.ReadToEnd().Trim();

            if (proc.ExitCode == 0)
            {
                _tray.ShowBalloonTip(4000, "MouseBD – Tryb USB aktywny",
                    $"Połączenie USB gotowe.\nW aplikacji wpisz: 127.0.0.1", ToolTipIcon.Info);
                UpdateStatus("Tryb USB aktywny");
            }
            else
            {
                string detail = string.IsNullOrEmpty(error) ? output : error;
                MessageBox.Show(
                    $"Błąd ADB ({proc.ExitCode}):\n{detail}\n\n" +
                    "Upewnij się, że:\n" +
                    "• Telefon jest podłączony kablem USB\n" +
                    "• Debugowanie USB jest włączone w opcjach deweloperskich\n" +
                    "• Zaakceptowałeś monit o debugowanie USB na telefonie",
                    "Tryb USB – błąd ADB",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie można uruchomić adb:\n{ex.Message}",
                "Tryb USB – błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? FindAdb()
    {
        // 1. Check PATH
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir, "adb.exe");
            if (File.Exists(candidate)) return candidate;
        }

        // 2. Common Android SDK locations
        var sdkRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"C:\",
        };

        var sdkPaths = new[]
        {
            @"Android\sdk\platform-tools\adb.exe",
            @"Android\Sdk\platform-tools\adb.exe",
            @"Android\platform-tools\adb.exe",
        };

        foreach (var root in sdkRoots)
        foreach (var rel  in sdkPaths)
        {
            var full = Path.Combine(root, rel);
            if (File.Exists(full)) return full;
        }

        return null;
    }

    // ── IP Helpers ─────────────────────────────────────────────────────────

    private static List<(string Name, string Ip)> GetAllLocalIps()
    {
        var result = new List<(string, string)>();
        try
        {
            // Preferred types first: WiFi, Ethernet; skip virtual/tunnel adapters
            var ifaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType is not NetworkInterfaceType.Loopback
                                                   and not NetworkInterfaceType.Tunnel)
                .Where(n => !IsVirtualAdapter(n))
                .OrderBy(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1);

            foreach (var iface in ifaces)
            {
                foreach (var addr in iface.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        result.Add((iface.Name, addr.Address.ToString()));
                }
            }
        }
        catch { }
        return result;
    }

    private static bool IsVirtualAdapter(NetworkInterface n)
    {
        var name = n.Name + " " + n.Description;
        foreach (var kw in new[] { "Virtual", "VMware", "VirtualBox", "Hyper-V", "vEthernet",
                                   "TAP-", "Loopback", "Pseudo", "Teredo", "Bluetooth" })
            if (name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string GetPrimaryIpDisplay()
    {
        var ips = GetAllLocalIps();
        return ips.Count > 0 ? $"IP: {ips[0].Ip}" : "IP: (nieznane)";
    }

    private static string GetAllIpsFormatted()
    {
        var ips = GetAllLocalIps();
        return ips.Count > 0
            ? string.Join("\n", ips.Select(x => $"{x.Name}: {x.Ip}"))
            : "Nie znaleziono adresu IP";
    }

    // ──────────────────────────────────────────────────────────────────────

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _server.Dispose();
        base.ExitThreadCore();
    }
}
