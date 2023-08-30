using MessagePack;

namespace DedicatedServer.Madness.Packets
{
    public enum PacketType
    {
        Null = 0,
        CPacketHello = 1,
        SPacketHello = -1
    }
    
    
    [MessagePackObject]
    public class Packet
    {
        [Key("PacketType")] public PacketType type;

        protected Packet(PacketType _type)
        {
            type = _type;
        }
    }
}