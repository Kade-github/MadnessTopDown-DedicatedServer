using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject]
public class SPacketSignUp : Packet
{
    [Key("status")] public Status statusCode;
    public SPacketSignUp() : base(PacketType.SPacketSignUp)
    {
    }
}