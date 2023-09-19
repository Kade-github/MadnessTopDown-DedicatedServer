using MessagePack;

namespace DedicatedServer.Madness.Packets
{
    [MessagePackObject]
    public class CPacketHello : Packet
    {
        [Key("Nothing")] public byte[] AESKey;
        [Key("Version")] public string Version;
        
        public CPacketHello() : base(PacketType.CPacketHello)
        {
        }
    }
}