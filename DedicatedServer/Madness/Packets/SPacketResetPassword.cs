using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject()]
public class SPacketResetPassword : Packet
{
    [Key("status")] public Status StatusCode;

    public SPacketResetPassword() : base(PacketType.SPacketResetPassword)
    {
    }
}