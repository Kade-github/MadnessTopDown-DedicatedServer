using System;
using System.Threading.Tasks;
using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject()]
public class CPacketHeartbeat : Packet
{
    [Key("number")] public int Number;
    public CPacketHeartbeat() : base(PacketType.CPacketHeartbeat)
    {
    }

    public override async Task Handle(Player p)
    {
        if (p.timeSinceHeartbeat != 0)
        {
            long diff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - p.timeSinceHeartbeat;
            if (diff > 30)
            {
                Program.QueueDisconnect(p.peer, (uint)Status.FuckYou);
                return;
            }
        }

        if (Number != p.heartbeatNumber)
        {
            // Ban the person
            Program.QueueDisconnect(p.peer, (uint)Status.PaymentRequired);
            return;
        }
        
        p.timeSinceHeartbeat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (p.next_aes.Length > 0)
        {
            p.current_aes = p.next_aes;
            p.next_aes = Array.Empty<Byte>();
        }
    }
}