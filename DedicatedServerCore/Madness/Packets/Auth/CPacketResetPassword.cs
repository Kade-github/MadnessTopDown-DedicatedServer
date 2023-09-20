using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DedicatedServer.Madness.Helpers;
using DedicatedServer.Madness.Mail;
using MessagePack;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class CPacketResetPassword : Packet
{
    [Key("username")] public string Username;
    public CPacketResetPassword() : base(PacketType.CPacketResetPassword)
    {
    }

    public override async Task Handle(Player p)
    {
        Account? a = await PlayerHandle.GetAccount(Username);

        SPacketResetPassword st;
        if (a == null)
        {
            st = new SPacketResetPassword();
            st.StatusCode = Status.NotFound;
            PacketHandle.QueuePacket(p,st);
            return;
        }

        if (a.PasswordReset.Length != 0)
        {
            st = new SPacketResetPassword();
            st.StatusCode = Status.UserAlreadyExists;
            PacketHandle.QueuePacket(p,st);
            return;
        }

        a.PasswordReset = RString.RandomString(6);
        
        Thread t = new Thread(() =>
        {
            SMTP.SendMessage(
                "<h1>Hello " + Username + "</h1>\n<p>Here is your password reset code: " + a.PasswordReset +
                "</p>\n\n<p><b>If you didn't request this, you can safely discard this email.</b></p>\n\n<p><a href='https://madnessriotrage.com/'>MadnessRiotRage</a> email sent from in-game (" + p.peer.IP + ")</p>",
                "Password Reset - Madness: Riot Rage", a.Email, "accounts@madnessriotrage.com",
                "Account Management", Username);
            Program.log.Info("Sent email to " + a.Email + ", with password reset code: " + a.PasswordReset);
        });
        t.Start();
        
        st = new SPacketResetPassword();
        st.StatusCode = Status.Okay;
        PacketHandle.QueuePacket(p,st);
    }
}