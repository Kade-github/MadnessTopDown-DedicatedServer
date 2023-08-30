namespace DedicatedServer.Madness.Packets
{
    public enum PacketType
    {
        C_Null = 1,
        C_Hello = 2,
        S_Null = -1,
        S_Hello = -2
    }
    
    public class Packet
    {
        public PacketType type;
    }
}