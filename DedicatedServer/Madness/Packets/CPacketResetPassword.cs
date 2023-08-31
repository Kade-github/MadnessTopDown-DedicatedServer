using System.Collections.Generic;
using System.Threading.Tasks;
using MessagePack;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject()]
public class CPacketResetPassword : Packet
{
    [Key("username")] public string Username;
    protected CPacketResetPassword() : base(PacketType.CPacketResetPassword)
    {
    }

    public override async Task Handle(Player p)
    {
        Account a = await Program.GetAccount(Username);

        if (a == null)
        {
            SPacketResetPassword st = new SPacketResetPassword();
            st.StatusCode = Status.BadRequest;
            Program.QueuePacket(p,st);
            return;
        }

        if (a.PasswordReset.Length != 0)
        {
            SPacketResetPassword st = new SPacketResetPassword();
            st.StatusCode = Status.UserAlreadyExists;
            Program.QueuePacket(p,st);
            return;
        }
        
        
    }
}