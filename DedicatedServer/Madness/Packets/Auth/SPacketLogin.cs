using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class SPacketLogin : Packet
{
    [Key("status")] public Status StatusCode;

    public SPacketLogin() : base(PacketType.SPacketLogin)
    {
    }
}