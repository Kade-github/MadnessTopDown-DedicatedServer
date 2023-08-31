using System.Linq;
using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject]
public class SPacketVerifyEmail : Packet
{

    [Key("status")] public Status StatusCode;

    public SPacketVerifyEmail() : base(PacketType.SPacketVerifyEmail)
    {
    }
    
}