using System;
using System.Threading.Tasks;
using DedicatedServer.Madness.Helpers;
using MessagePack;
using DedicatedServer.Madness.Packets;
namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class CPacketLogin : Packet
{
    [Key("username")] public string Username;
    [Key("passwordHash")] public byte[] PasswordHash;
    public CPacketLogin() : base(PacketType.CPacketLogin)
    {
    }

    public override async Task Handle(Player p)
    {
        Account a = await Account.GetAccount(Username); // mysql call, but if the account is in the hourly checked cache it'll return that.;
        SPacketLogin status;
        if (a == null)
        {
            status = new SPacketLogin();
            status.StatusCode = Status.BadRequest;
            Program.QueuePacket(p, status);
            return;
        }
        string base64 = HashPassword.HashIntoBase64(PasswordHash, a.PasswordSalt);
        if (base64 != a.PasswordHash)
        {
            status = new SPacketLogin();
            status.StatusCode = Status.Unauthorized;
            Program.QueuePacket(p, status);
            return;
        }
        if (a.Banned)
        {
            // todo: eventually ban that ip that tried to access the account
            Program.QueueDisconnect(p.peer, (uint)Status.PaymentRequired);
            return;
        }

        p.account = a;
        
        status = new SPacketLogin();
        status.StatusCode = Status.Okay;
        Program.QueuePacket(p, status);

        p.playerLog += Logging.FormatString(p.peer.IP + " has logged in.");
    }
}