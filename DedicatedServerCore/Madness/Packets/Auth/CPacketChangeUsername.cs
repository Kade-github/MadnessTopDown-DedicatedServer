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

        p.account!.Username = Username;
        
        SPacketChangeUsername username2 = new SPacketChangeUsername();
        username2.status = Status.Okay;
        Program.QueuePacket(p, username2);

        await p.account.Export();

    }
}