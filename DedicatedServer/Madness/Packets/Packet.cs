using MessagePack;

namespace DedicatedServer.Madness.Packets
{
    public enum Status
    {
        Okay = 200,
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        TooManyRequests = 429,
        FuckYou = 1337
    }
    
    public enum PacketType
    {
        Null = 0,
        CPacketHello = 1,
        CPacketSignUp = 2,
        SPacketHello = -1,
        SPacketSignUp = -2
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