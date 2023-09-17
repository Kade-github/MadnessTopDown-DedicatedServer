using System;
using System.Linq;
using System.Threading.Tasks;
using DedicatedServer.Madness.Packets;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class CPacketChangeUsername : Packet
{
    [Key("Username")] public string Username;
    public CPacketChangeUsername() : base(PacketType.CPacketChangeUsername)
    {
        
    }
    
    public override async Task Handle(Player p)
    {
        if (p.account == null)
        {
            SPacketChangeUsername username = new SPacketChangeUsername();
            username.status = Status.BadRequest;
            Program.QueuePacket(p, username);
            return;
        }
        
        if (Username.Length is < 4 or > 32)
        {
            SPacketChangeUsername username = new SPacketChangeUsername();
            username.status = Status.BadRequest;
            Program.QueuePacket(p, username);
            return;
        }
        
        if (Username.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            SPacketChangeUsername username = new SPacketChangeUsername();
            username.status = Status.BadRequest;
            Program.QueuePacket(p, username);
            return;
        }
        
        if (Username == p.account?.Username)
        {
            SPacketChangeUsername username = new SPacketChangeUsername();
            username.status = Status.BadRequest;
            Program.QueuePacket(p, username);
            return;
        }

        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(p.account!.LastUsernameChange);

        if ((DateTime.Now - dateTimeOffset.UtcDateTime).Hours < 12)
        {
            SPacketChangeUsername username3 = new SPacketChangeUsername();
            username3.status = Status.Unauthorized;
            Program.QueuePacket(p, username3);
            return;
        }
        
        p.account.LastUsernameChange = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        
        p.account!.Username = Username;
        
        SPacketChangeUsername username2 = new SPacketChangeUsername();
        username2.status = Status.Okay;
        Program.QueuePacket(p, username2);

        await p.account.Export();

    }
}