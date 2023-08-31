using System;
using System.Threading.Tasks;
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
        UserAlreadyExists = 600,
        FuckYou = 1337
    }
    
    public enum PacketType
    {
        Null = 0,
        CPacketHello = 1,
        CPacketSignUp = 2,
        CPacketVerifyEmail = 3,
        CPacketHeartBeat = 1000,
        SPacketHello = -1,
        SPacketSignUp = -2,
        SPacketVerifyEmail = -3,
        SPacketStatus = -1000,
    }
    
    
    [MessagePackObject]
    public class Packet
    {
        [Key("PacketType")] public PacketType type;

        protected Packet(PacketType _type)
        {
            type = _type;
        }

        public virtual async Task Handle(Player p)
        {
            Console.WriteLine(type + " doesn't have a handle!");
        }
    }
}