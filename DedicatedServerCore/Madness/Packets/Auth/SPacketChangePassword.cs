using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class SPacketChangePassword : Packet
{
    [Key("status")] public Status status;

    public SPacketChangePassword() : base(PacketType.SPacketChangePassword)
    {
    }
}