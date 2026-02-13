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
        _menu.Items.Add("Pokaz IP...",  null, (_, _) => ShowStatus());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Zamknij",      null, (_, _) => ExitThread());

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
            "Polaczenie WiFi:\n" +
            "  Podaj adres IP w aplikacji lub uzyj przycisku 'Wykryj'.\n" +
            "  Jesli jest kilka adresow, wybierz ten z WiFi (np. 192.168.x.x).\n\n" +
            "Polaczenie USB (bez ADB):\n" +
            "  1. Podlacz telefon kablem USB do komputera.\n" +
            "  2. Wlacz 'Udostepnianie USB' (tethering) w ustawieniach Androida.\n" +
            "  3. W aplikacji nacisnij 'Wykryj' – serwer zostanie znaleziony automatycznie.",
            "MouseBD Server – Informacje o polaczeniu",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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
