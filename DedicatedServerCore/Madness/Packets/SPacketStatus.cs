using MessagePack;

namespace DedicatedServer.Madness.Packets;

public class SPacketStatus : Packet
{
    [Key("status")] public Status statusCode;

    public SPacketStatus() : base(PacketType.SPacketStatus)
    {
    }
}