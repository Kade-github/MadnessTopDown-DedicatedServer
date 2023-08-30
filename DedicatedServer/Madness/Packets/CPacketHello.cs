namespace DedicatedServer.Madness.Packets
{
    public class CPacketHello : Packet
    {
        CPacketHello()
        {
            type = PacketType.C_Hello;
        }
    }
}