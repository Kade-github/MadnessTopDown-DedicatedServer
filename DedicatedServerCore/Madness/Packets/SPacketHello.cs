using MessagePack;

namespace DedicatedServer.Madness.Packets
{
    [MessagePackObject]
    public class SPacketHello : Packet
    {
        public SPacketHello() : base(PacketType.SPacketHello)
        {
        }
    }
}