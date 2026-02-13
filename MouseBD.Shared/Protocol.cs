namespace MouseBD.Shared;

/// <summary>
/// UDP packet types for touchpad communication.
/// </summary>
public enum PacketType : byte
{
    Move        = 0x01,
    LeftDown    = 0x02,
    LeftUp      = 0x03,
    RightDown   = 0x04,
    RightUp     = 0x05,
    Scroll      = 0x06,
    Ping        = 0x07,
    Pong        = 0x08,
    MiddleDown  = 0x09,
    MiddleUp    = 0x0A,
    Discover    = 0x0B,  // broadcast: phone looking for server
    DiscoverAck = 0x0C,  // server response: [type][port_lo][port_hi]
}

/// <summary>
/// Binary protocol helpers.
///
/// Packet layout:
///   [1 byte: PacketType]
///   [4 bytes: float X]  (only for Move/Scroll)
///   [4 bytes: float Y]  (only for Move/Scroll)
///
/// For button packets (LeftDown/Up etc.) payload is just the type byte.
/// </summary>
public static class Protocol
{
    public const int DefaultPort = 27015;
    public const string MagicHeader = "MBD1";

    // --- Serialization ---

    public static byte[] BuildMovePacket(float dx, float dy)
        => BuildXYPacket(PacketType.Move, dx, dy);

    public static byte[] BuildScrollPacket(float dx, float dy)
        => BuildXYPacket(PacketType.Scroll, dx, dy);

    public static byte[] BuildButtonPacket(PacketType type)
    {
        return new[] { (byte)type };
    }

    public static byte[] BuildPingPacket()
        => new[] { (byte)PacketType.Ping };

    public static byte[] BuildPongPacket()
        => new[] { (byte)PacketType.Pong };

    public static byte[] BuildDiscoverPacket()
        => new[] { (byte)PacketType.Discover };

    public static byte[] BuildDiscoverAckPacket(int port)
        => new[] { (byte)PacketType.DiscoverAck, (byte)(port & 0xFF), (byte)((port >> 8) & 0xFF) };

    /// <summary>Write a Move packet into a pre-allocated 9-byte span (zero allocation).</summary>
    public static void WriteMovePacket(Span<byte> buf, float dx, float dy)
        => WriteXYPacket(buf, PacketType.Move, dx, dy);

    /// <summary>Write a Scroll packet into a pre-allocated 9-byte span (zero allocation).</summary>
    public static void WriteScrollPacket(Span<byte> buf, float dx, float dy)
        => WriteXYPacket(buf, PacketType.Scroll, dx, dy);

    // --- Deserialization ---

    public static bool TryParse(ReadOnlySpan<byte> data, out PacketType type, out float x, out float y)
    {
        type = PacketType.Move;
        x = 0f;
        y = 0f;

        if (data.Length < 1) return false;

        type = (PacketType)data[0];

        if (type is PacketType.Move or PacketType.Scroll)
        {
            if (data.Length < 9) return false;
            x = BitConverter.ToSingle(data[1..5]);
            y = BitConverter.ToSingle(data[5..9]);
        }

        return true;
    }

    // --- Helpers ---

    private static byte[] BuildXYPacket(PacketType type, float x, float y)
    {
        var buf = new byte[9];
        buf[0] = (byte)type;
        BitConverter.TryWriteBytes(buf.AsSpan(1), x);
        BitConverter.TryWriteBytes(buf.AsSpan(5), y);
        return buf;
    }

    private static void WriteXYPacket(Span<byte> buf, PacketType type, float x, float y)
    {
        buf[0] = (byte)type;
        BitConverter.TryWriteBytes(buf[1..5], x);
        BitConverter.TryWriteBytes(buf[5..9], y);
    }
}
