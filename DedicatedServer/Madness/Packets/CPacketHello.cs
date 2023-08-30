using MessagePack;

namespace DedicatedServer.Madness.Packets
{
    [MessagePackObject]
    public class CPacketHello : Packet
    {
        [Key("Nothing")] public byte[] AESKey;
        
        public CPacketHello() : base(PacketType.CPacketHello)
        {
        }
    }
}