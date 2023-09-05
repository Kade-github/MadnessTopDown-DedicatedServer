using System;
using System.Linq;
using System.Threading.Tasks;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Secrets;
using MessagePack;
using MySqlConnector;
using DedicatedServer.Madness.Packets;
namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject]
public class CPacketVerifyEmail : Packet
{
    [Key("email")] public string Email;
    [Key("code")] public string VerifyCode;
    public CPacketVerifyEmail() : base(PacketType.CPacketVerifyEmail)
    {
    }
    
    public override async Task Handle(Player p)
    {
        Account a = Program.tempAccounts.FirstOrDefault(a => a.Email == Email && a.EmailConfirmation == VerifyCode);
        if (a == null)
        {
            SPacketVerifyEmail status = new SPacketVerifyEmail();
            status.StatusCode = Status.BadRequest;
            Program.QueuePacket(p, status);
            return;
        }

        if (a.EmailConfirmed)
        {
            SPacketVerifyEmail status = new SPacketVerifyEmail();
            status.StatusCode = Status.BadRequest;
            Program.QueuePacket(p, status);
            return;
        }

        a.EmailConfirmed = true;

        SPacketVerifyEmail s = new SPacketVerifyEmail();
        s.StatusCode = Status.Okay;
        Program.QueuePacket(p, s);
        Program.log.Info(a.Username + " has verified their email!");
        
        await a.Export();
    }
}