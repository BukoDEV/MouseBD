using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MouseBD.Server;

/// <summary>
/// System tray application - sits in the notification area and shows status.
/// </summary>
public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon  _tray;
    private readonly UdpServer   _server;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _ipItem;

    public TrayApp()
    {
        _server = new UdpServer();
        _server.OnLog             += msg => _statusItem.Text = msg;
        _server.OnClientConnected += ip  => UpdateStatus($"Połączono: {ip}");

        _statusItem = new ToolStripMenuItem("Oczekiwanie...") { Enabled = false };
        _ipItem     = new ToolStripMenuItem(GetLocalIp())     { Enabled = false };

        _menu = new ContextMenuStrip();
        _menu.Items.Add(new ToolStripMenuItem("MouseBD Server") { Enabled = false, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_ipItem);
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Zamknij", null, (_, _) => ExitThread());

        _tray = new NotifyIcon
        {
            Text            = "MouseBD Server",
            Icon            = SystemIcons.Application,
            Visible         = true,
            ContextMenuStrip = _menu,
        };
        _tray.DoubleClick += (_, _) => ShowStatus();

        _server.Start();
        UpdateStatus("Serwer uruchomiony");
        _tray.ShowBalloonTip(3000, "MouseBD", $"Serwer nasłuchuje na {GetLocalIp()}", ToolTipIcon.Info);
    }

    private void UpdateStatus(string msg)
    {
        _statusItem.Text = msg;
        _tray.Text = $"MouseBD - {msg}";
    }

    private void ShowStatus()
    {
        MessageBox.Show(
            $"Adres IP: {GetLocalIp()}\nPort: {MouseBD.Shared.Protocol.DefaultPort}\n\n" +
            "Podaj ten adres IP w aplikacji na telefonie.",
            "MouseBD Server",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static string GetLocalIp()
    {
        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up) continue;
                if (iface.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;

                foreach (var addr in iface.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        return addr.Address.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _server.Dispose();
        base.ExitThreadCore();
    }
}
