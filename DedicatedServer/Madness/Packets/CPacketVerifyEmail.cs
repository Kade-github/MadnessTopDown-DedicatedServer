using System.Linq;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Secrets;
using MessagePack;
using MySqlConnector;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject]
public class CPacketVerifyEmail : Packet
{
    [Key("email")] public string Email;
    [Key("code")] public string VerifyCode;
    public CPacketVerifyEmail() : base(PacketType.CPacketVerifyEmail)
    {
    }
    
    public override void Handle(Player p)
    {
        Account a = Program.tempAccounts.FirstOrDefault(a => a.Email == Email && a.EmailConfirmation == VerifyCode);
        if (a == null)
        {
            SPacketVerifyEmail status = new SPacketVerifyEmail();
            status.StatusCode = Status.BadRequest;
            Program.SendPacket(p, status);
            return;
        }

        if (a.EmailConfirmed)
        {
            SPacketVerifyEmail status = new SPacketVerifyEmail();
            status.StatusCode = Status.BadRequest;
            Program.SendPacket(p, status);
            return;
        }

        a.EmailConfirmed = true;
        
        MySqlConnection connection = SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);
        var insert = new MySqlCommand("INSERT INTO users (username, email, password_hash, salt, creationdate, email_confirmed) VALUES (@Username, @Email, @PasswordHash, @Salt, @CreationDate, 1)");
        insert.Connection = connection;
        insert.Parameters.AddWithValue("@Username", a.Username);
        insert.Parameters.AddWithValue("@Email", a.Email);
        insert.Parameters.AddWithValue("@PasswordHash", a.PasswordHash);
        insert.Parameters.AddWithValue("@Salt", a.PasswordSalt);
        insert.Parameters.AddWithValue("@CreationDate", a.CreationDate);
        
        insert.Prepare();

        insert.ExecuteNonQuery();
        
        connection.Close();

        SPacketVerifyEmail s = new SPacketVerifyEmail();
        s.StatusCode = Status.Okay;
        Program.SendPacket(p, s);
    }
}