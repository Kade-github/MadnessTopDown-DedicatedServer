using DedicatedServer.Madness.Packets;
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
            Program.QueuePacket(p, logout);
            return;
        }

        p.AddLog("Logged out of " + p.account.Username);
        p.account = null;
        
        logout = new SPacketLogout
        {
            statusCode = Status.Okay
        };
        Program.QueuePacket(p, logout);
    }
}