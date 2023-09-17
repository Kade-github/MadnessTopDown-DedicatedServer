using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class SPacketLogout : Packet
{
    [Key("status")] public Status statusCode;
    public SPacketLogout() : base(PacketType.SPacketLogout)
    {
    }
}