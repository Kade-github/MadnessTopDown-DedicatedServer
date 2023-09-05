using System.Linq;
using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject]
public class SPacketVerifyEmail : Packet
{

    [Key("status")] public Status StatusCode;

    public SPacketVerifyEmail() : base(PacketType.SPacketVerifyEmail)
    {
    }
    
}