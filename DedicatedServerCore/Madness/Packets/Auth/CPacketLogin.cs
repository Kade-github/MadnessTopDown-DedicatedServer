using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DedicatedServer.Madness.Helpers;
using MessagePack;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;

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
        

        Account? a = await PlayerHandle.GetAccount(Username); // mysql call, but if the account is in the hourly checked cache it'll return that.
        SPacketLogin status;
        if (a == null)
        {
            status = new SPacketLogin();
            status.StatusCode = Status.BadRequest;
            PacketHandle.QueuePacket(p, status);
            p.AddLog("Failed to login to " + Username + " because the account doesn't exist.");
            return;
        }
        
        string base64 = HashPassword.HashIntoBase64(PasswordHash, a.PasswordSalt);
        if (base64 != a.PasswordHash)
        {
            status = new SPacketLogin();
            status.StatusCode = Status.Unauthorized;
            PacketHandle.QueuePacket(p, status);
            p.AddLog("Failed to login to " + Username + " the password was incorrect.");
            return;
        }

        if (a.Banned)
        {
            await PlayerHandle.BanPlayer(a);
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.PaymentRequired);
            p.AddLog("Failed to login to " + Username + " because the account is banned.");
            return;
        }

        p.account = a;
        
        status = new SPacketLogin();
        status.StatusCode = Status.Okay;
        PacketHandle.QueuePacket(p, status);
        
        p.AddLog("Logged into " + p.account.Username);
    }
}