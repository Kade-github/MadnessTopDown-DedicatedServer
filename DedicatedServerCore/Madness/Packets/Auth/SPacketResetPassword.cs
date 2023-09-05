using MessagePack;
using DedicatedServer.Madness.Packets;
namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class SPacketResetPassword : Packet
{
    [Key("status")] public Status StatusCode;

    public SPacketResetPassword() : base(PacketType.SPacketResetPassword)
    {
    }
}