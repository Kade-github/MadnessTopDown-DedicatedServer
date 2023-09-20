using System.Threading.Tasks;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class CPacketLogout : Packet
{
    public CPacketLogout() : base(PacketType.CPacketLogout)
    {
    }

    public override async Task Handle(Player p)
    {
        SPacketLogout logout;
        if (p.account == null)
        {
            Program.log.Info(p.peer.IP + " tried to log out, but it can't! (It doesn't even have an account)");
            logout = new SPacketLogout
            {
                statusCode = Status.BadRequest
            };
            PacketHandle.QueuePacket(p, logout);
            return;
        }

        p.AddLog("Logged out of " + p.account.Username);
        p.account = null;
        
        logout = new SPacketLogout
        {
            statusCode = Status.Okay
        };
        PacketHandle.QueuePacket(p, logout);
    }
}