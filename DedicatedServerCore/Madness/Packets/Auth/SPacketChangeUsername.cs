using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class SPacketChangeUsername : Packet
{
    [Key("status")] public Status status;
    
    public SPacketChangeUsername() : base(PacketType.SPacketChangeUsername)
    {
    }
}