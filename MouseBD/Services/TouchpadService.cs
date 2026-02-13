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

    // Pre-allocated send buffers — avoids new byte[9] on every move/scroll event
    private readonly byte[] _moveBuf   = new byte[9];
    private readonly byte[] _scrollBuf = new byte[9];

    public bool IsConnected => _connected;
    public event Action<bool>?   OnConnectionChanged;
    public event Action<string>? OnError;

    // --- Discovery ---

    /// <summary>
    /// Broadcasts a Discover packet and waits for the first server that responds.
    /// Works on WiFi and USB tethering (no ADB required).
    /// Returns the server's IP on success, null on timeout.
    /// </summary>
    public async Task<string?> DiscoverAsync(int timeoutMs = 2000)
    {
        try
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            var discover = Protocol.BuildDiscoverPacket();
            await udp.SendAsync(discover, new IPEndPoint(IPAddress.Broadcast, Protocol.DefaultPort));

            using var cts = new CancellationTokenSource(timeoutMs);
            while (true)
            {
                var result = await udp.ReceiveAsync(cts.Token);
                if (result.Buffer.Length >= 3 &&
                    result.Buffer[0] == (byte)PacketType.DiscoverAck)
                {
                    return result.RemoteEndPoint.Address.ToString();
                }
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

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

            // Low-latency socket options
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
                OnError?.Invoke("Serwer nie odpowiada. Sprawdz IP i czy serwer jest uruchomiony.");
                Disconnect();
                return false;
            }

            OnError?.Invoke("Nieprawidlowa odpowiedz serwera.");
            Disconnect();
            return false;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Blad polaczenia: {ex.Message}");
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

    // --- Sending events ---

    public void SendMove(float dx, float dy)
    {
        if (!_connected || _udp == null || _remote == null) return;
        Protocol.WriteMovePacket(_moveBuf, dx, dy);
        SendRaw(_moveBuf);
    }

    public void SendScroll(float dx, float dy)
    {
        if (!_connected || _udp == null || _remote == null) return;
        Protocol.WriteScrollPacket(_scrollBuf, dx, dy);
        SendRaw(_scrollBuf);
    }

    public void SendLeftDown()   => Send(Protocol.BuildButtonPacket(PacketType.LeftDown));
    public void SendLeftUp()     => Send(Protocol.BuildButtonPacket(PacketType.LeftUp));
    public void SendRightDown()  => Send(Protocol.BuildButtonPacket(PacketType.RightDown));
    public void SendRightUp()    => Send(Protocol.BuildButtonPacket(PacketType.RightUp));
    public void SendMiddleDown() => Send(Protocol.BuildButtonPacket(PacketType.MiddleDown));
    public void SendMiddleUp()   => Send(Protocol.BuildButtonPacket(PacketType.MiddleUp));

    // --- Internals ---

    /// <summary>Zero-allocation send via raw Socket.SendTo with a Span.</summary>
    private void SendRaw(ReadOnlySpan<byte> data)
    {
        try
        {
            _udp!.Client.SendTo(data, SocketFlags.None, _remote!);
        }
        catch { }
    }

    /// <summary>Send a small heap-allocated button packet (infrequent, no perf concern).</summary>
    private void Send(byte[] data)
    {
        if (!_connected || _udp == null || _remote == null) return;
        SendRaw(data);
    }

    public void Dispose()
    {
        Disconnect();
        _udp?.Dispose();
    }
}
