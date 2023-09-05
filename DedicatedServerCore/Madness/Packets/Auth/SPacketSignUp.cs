using MessagePack;
using DedicatedServer.Madness.Packets;
namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject]
public class SPacketSignUp : Packet
{
    [Key("status")] public Status statusCode;
    public SPacketSignUp() : base(PacketType.SPacketSignUp)
    {
    }
}