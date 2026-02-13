using System.Net;
using System.Net.Sockets;
using MouseBD.Shared;

namespace MouseBD.Server;

/// <summary>
/// Listens for UDP packets from the MAUI touchpad app and dispatches
/// mouse actions using MouseController.
/// </summary>
public sealed class UdpServer : IDisposable
{
    private readonly UdpClient  _udp;
    private readonly int        _port;
    private          CancellationTokenSource? _cts;
    private          Task?      _listenTask;

    public event Action<string>? OnLog;
    public event Action<string>? OnClientConnected;

    // Accumulate sub-pixel movement for smoother tracking
    private float _accX;
    private float _accY;

    public UdpServer(int port = Protocol.DefaultPort)
    {
        _port = port;
        _udp  = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        // Low-latency socket options
        _udp.Client.ReceiveBufferSize = 1024;
        _udp.Client.SendBufferSize    = 1024;
    }

    public void Start()
    {
        _cts        = new CancellationTokenSource();
        _listenTask = ListenAsync(_cts.Token);
        Log($"[Server] Listening on UDP :{_port}");
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listenTask?.Wait(2000);
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        string? lastClient = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct);
                var sender = result.RemoteEndPoint.ToString();

                if (sender != lastClient)
                {
                    lastClient = sender;
                    OnClientConnected?.Invoke(sender);
                    Log($"[Server] Client: {sender}");
                }

                ProcessPacket(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log($"[Server] Error: {ex.Message}");
            }
        }
    }

    private void ProcessPacket(byte[] data, IPEndPoint remoteEp)
    {
        if (!Protocol.TryParse(data, out var type, out float x, out float y))
            return;

        switch (type)
        {
            case PacketType.Move:
                ApplyMove(x, y);
                break;

            case PacketType.Scroll:
                // Y = vertical scroll, X = horizontal
                if (MathF.Abs(y) >= MathF.Abs(x))
                    MouseController.ScrollVertical(y);
                else
                    MouseController.ScrollHorizontal(x);
                break;

            case PacketType.LeftDown:   MouseController.LeftDown();   break;
            case PacketType.LeftUp:     MouseController.LeftUp();     break;
            case PacketType.RightDown:  MouseController.RightDown();  break;
            case PacketType.RightUp:    MouseController.RightUp();    break;
            case PacketType.MiddleDown: MouseController.MiddleDown(); break;
            case PacketType.MiddleUp:   MouseController.MiddleUp();   break;

            case PacketType.Ping:
                var pong = Protocol.BuildPongPacket();
                _udp.Send(pong, pong.Length, remoteEp);
                break;

            case PacketType.Discover:
                var ack = Protocol.BuildDiscoverAckPacket(_port);
                _udp.Send(ack, ack.Length, remoteEp);
                break;
        }
    }

    /// <summary>
    /// Applies sub-pixel-accurate movement by accumulating fractional pixels.
    /// </summary>
    private void ApplyMove(float dx, float dy)
    {
        _accX += dx;
        _accY += dy;

        int moveX = (int)_accX;
        int moveY = (int)_accY;

        if (moveX != 0 || moveY != 0)
        {
            MouseController.MoveDelta(moveX, moveY);
            _accX -= moveX;
            _accY -= moveY;
        }
    }

    private void Log(string msg) => OnLog?.Invoke(msg);

    public void Dispose()
    {
        Stop();
        _udp.Dispose();
        _cts?.Dispose();
    }
}
