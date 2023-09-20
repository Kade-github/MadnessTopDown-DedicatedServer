using System.Security.Cryptography;
using System.Threading.Tasks;
using DedicatedServer.Madness.Helpers;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;
using MessagePack;

namespace DedicatedServer.Madness.Auth.Packets;

[MessagePackObject()]
public class CPacketChangePassword : Packet
{
    [Key("username")] public string Username;
    [Key("hash")] public byte[] Password;
    [Key("code")] public string Code;
    public CPacketChangePassword() : base(PacketType.CPacketChangePassword)
    {
    }

    public override async Task Handle(Player p)
    {
        byte[] salt;
        string base64;
        SPacketResetPassword password;
        if (p.account == null)
        {
            if (Code.Length != 6)
            {
                password = new SPacketResetPassword();
                password.StatusCode = Status.BadRequest;
                PacketHandle.QueuePacket(p, password);
                return;
            }
            
            if (Username.Length is 0 or > 32)
            {
                password = new SPacketResetPassword();
                password.StatusCode = Status.BadRequest;
                PacketHandle.QueuePacket(p, password);
                return;
            }

            Account? a = await PlayerHandle.GetAccount(Username);

            if (a == null)
            {
                password = new SPacketResetPassword();
                password.StatusCode = Status.NotFound;
                PacketHandle.QueuePacket(p, password);
                return;
            }

            if (a.PasswordReset != Code || a.PasswordReset.Length == 0)
            {
                password = new SPacketResetPassword();
                password.StatusCode = Status.Forbidden;
                PacketHandle.QueuePacket(p, password);
                return;
            }

            a.PasswordReset = "";
            
            salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            base64 = HashPassword.HashIntoBase64(Password, salt);

            a.PasswordSalt = salt;
            a.PasswordHash = base64;
            
            password = new SPacketResetPassword();
            password.StatusCode = Status.Okay;
            PacketHandle.QueuePacket(p, password);

            await a.Export();
            
            return;
        }

        if (Code.Length != 0)
        {
            password = new SPacketResetPassword();
            password.StatusCode = Status.BadRequest;
            PacketHandle.QueuePacket(p, password);
            return;
        }

        if (Password.Length == 0)
        {
            password = new SPacketResetPassword();
            password.StatusCode = Status.BadRequest;
            PacketHandle.QueuePacket(p, password);
            return;
        }
        
        salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        base64 = HashPassword.HashIntoBase64(Password, salt);

        p.account.PasswordSalt = salt;
        p.account.PasswordHash = base64;
            
        SPacketChangePassword pass = new SPacketChangePassword();
        pass.status = Status.Okay;
        PacketHandle.QueuePacket(p, pass);

        await p.account.Export();
    }
}