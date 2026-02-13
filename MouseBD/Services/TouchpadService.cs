using System.Net;
using System.Net.Sockets;
using MouseBD.Models;
using MouseBD.Shared;

namespace MouseBD.Services;

/// <summary>
/// Sends touchpad events to the PC server via UDP.
/// Uses a dedicated socket for minimum latency.
/// </summary>
public sealed class TouchpadService : IDisposable
{
    private UdpClient?  _udp;
    private IPEndPoint? _remote;
    private bool        _connected;

    public bool IsConnected => _connected;
    public event Action<bool>?   OnConnectionChanged;
    public event Action<string>? OnError;

    // --- Connection management ---

    public async Task<bool> ConnectAsync(AppSettings settings)
    {
        Disconnect();

        if (string.IsNullOrWhiteSpace(settings.ServerIp))
        {
            OnError?.Invoke("Podaj adres IP serwera.");
            return false;
        }

        try
        {
            _remote = new IPEndPoint(IPAddress.Parse(settings.ServerIp), settings.ServerPort);
            _udp    = new UdpClient();

            // Low-latency options
            _udp.Client.SendBufferSize    = 512;
            _udp.Client.ReceiveBufferSize = 512;
            _udp.Client.Blocking          = false;

            // Send ping to verify server is reachable
            var pingData = Protocol.BuildPingPacket();
            await _udp.SendAsync(pingData, pingData.Length, _remote);

            // Wait for pong with 2s timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                var result = await _udp.ReceiveAsync(cts.Token);
                if (result.Buffer.Length >= 1 && result.Buffer[0] == (byte)PacketType.Pong)
                {
                    _connected = true;
                    OnConnectionChanged?.Invoke(true);
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                OnError?.Invoke("Serwer nie odpowiada. Sprawdź IP i czy serwer jest uruchomiony.");
                Disconnect();
                return false;
            }

            OnError?.Invoke("Nieprawidłowa odpowiedź serwera.");
            Disconnect();
            return false;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Błąd połączenia: {ex.Message}");
            Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        _connected = false;
        _udp?.Dispose();
        _udp    = null;
        _remote = null;
        OnConnectionChanged?.Invoke(false);
    }

    // --- Sending events (fire-and-forget, no await for minimum latency) ---

    public void SendMove(float dx, float dy)
        => Send(Protocol.BuildMovePacket(dx, dy));

    public void SendScroll(float dx, float dy)
        => Send(Protocol.BuildScrollPacket(dx, dy));

    public void SendLeftDown()   => Send(Protocol.BuildButtonPacket(PacketType.LeftDown));
    public void SendLeftUp()     => Send(Protocol.BuildButtonPacket(PacketType.LeftUp));
    public void SendRightDown()  => Send(Protocol.BuildButtonPacket(PacketType.RightDown));
    public void SendRightUp()    => Send(Protocol.BuildButtonPacket(PacketType.RightUp));
    public void SendMiddleDown() => Send(Protocol.BuildButtonPacket(PacketType.MiddleDown));
    public void SendMiddleUp()   => Send(Protocol.BuildButtonPacket(PacketType.MiddleUp));

    // --- Internals ---

    private void Send(byte[] data)
    {
        if (!_connected || _udp == null || _remote == null) return;
        try
        {
            // Fire and forget - UDP doesn't need await for low latency
            _udp.SendAsync(data, data.Length, _remote);
        }
        catch
        {
            // Ignore send errors for performance; connection check handles this
        }
    }

    public void Dispose()
    {
        Disconnect();
        _udp?.Dispose();
    }
}
