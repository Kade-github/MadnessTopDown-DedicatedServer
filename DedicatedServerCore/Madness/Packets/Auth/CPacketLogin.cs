using System;
using System.Diagnostics;
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
        Program.log.Info("Peer " + p.peer.IP + " is trying to log into an account with the name of " + Username);
        
        Stopwatch watch = new Stopwatch();
        watch.Start();
        Account? a = await Program.GetAccount(Username); // mysql call, but if the account is in the hourly checked cache it'll return that.
        SPacketLogin status;
        if (a == null)
        {
            Program.log.Info(p.peer.IP + " failed to log in!");
            status = new SPacketLogin();
            status.StatusCode = Status.BadRequest;
            Program.QueuePacket(p, status);
            return;
        }
        Program.log.Debug(watch.Elapsed.Milliseconds + "ms - login 1");
        watch.Restart();
        string base64 = HashPassword.HashIntoBase64(PasswordHash, a.PasswordSalt);
        if (base64 != a.PasswordHash)
        {
            status = new SPacketLogin();
            status.StatusCode = Status.Unauthorized;
            Program.QueuePacket(p, status);
            return;
        }
        Program.log.Debug(watch.Elapsed.Milliseconds + "ms - login 2");
        watch.Restart();
        if (a.Banned)
        {
            // todo: eventually ban that ip that tried to access the account
            Program.QueueDisconnect(p.peer, (uint)Status.PaymentRequired);
            return;
        }
        Program.log.Debug(watch.Elapsed.Milliseconds + "ms - login 3");
        watch.Restart();
        p.account = a;
        
        status = new SPacketLogin();
        status.StatusCode = Status.Okay;
        Program.QueuePacket(p, status);

        Program.log.Info(p.account.Username + " has logged in!");
        p.AddLog("Logged into " + p.account.Username);
        Program.log.Debug(watch.Elapsed.Milliseconds + "ms - login 4");
    }
}