using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Helpers;
using DedicatedServer.Madness.Mail;
using DedicatedServer.Madness.Secrets;
using MessagePack;
using MySqlConnector;
using Org.BouncyCastle.Crypto.Generators;

namespace DedicatedServer.Madness.Packets;

[MessagePackObject]
public class CPacketSignUp : Packet
{
    
    [Key("Email")] public string Email;
    [Key("Username")] public string Username;
    [Key("Password")] public byte[] Password;
    public CPacketSignUp() : base(PacketType.CPacketSignUp)
    {
    }


    public override async Task Handle(Player p)
    {
        if (!Email.Contains("@") || !Email.Contains(".") || Email.Contains(" "))
        {
            Program.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }
								
        if (Username.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            Program.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }

        if (Username.Length >= 16)
        {
            Program.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }
								
        if (Username.Length < 4)
        {
            Program.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }

        if (!SMTP.IsEmailGood(Email))
        {
            Program.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }
        
        Account ac = Program.tempAccounts.FirstOrDefault(a => a.Email == Email || a.Username == Username);
        if (ac != null)
        {
            SPacketSignUp status = new SPacketSignUp();
            status.statusCode = Status.UserAlreadyExists;
            Program.QueuePacket(p, status);
            return;
        }
        
        MySqlConnection connection = SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);
        var query = new MySqlCommand("SELECT * FROM users WHERE username = @Username or email = @Email");
        query.Parameters.AddWithValue("@Username", Username);
        query.Parameters.AddWithValue("@Email", Email);
        query.Connection = connection;

        await query.PrepareAsync();
        
        var reader = await query.ExecuteReaderAsync();

        if (reader.HasRows)
        {
            SPacketSignUp status = new SPacketSignUp();
            status.statusCode = Status.UserAlreadyExists;
            Program.QueuePacket(p, status);
            await connection.CloseAsync();

            await connection.DisposeAsync();
            return;
        }
        await connection.CloseAsync();

        await connection.DisposeAsync();
        
        
        
        byte[] salt = new byte[16];
        Program.number.GetNonZeroBytes(salt);

        // generate a BCrypt hash with a cost of 15, a salt of 128, and an md5 hashed string as the base.
        byte[] hash = BCrypt.Generate(Password, salt, 15);

        string base64 = Convert.ToBase64String(hash);

        Account a = new Account();
        a.Email = Email;
        a.Username = Username;
        a.PasswordHash = base64;
        a.CreationDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        a.Banned = false;
        a.EmailConfirmed = false;
        a.PasswordSalt = salt;
        a.LastIP = p.peer.IP;
        a.EmailConfirmation = RString.RandomString(5);
        
        Program.tempAccounts.Add(a);

        try
        {
            Thread t = new Thread(() =>
            {
                SMTP.SendMessage(
                    "<h1>Hello " + Username + "</h1>\n<p>Here is your verification key: " + a.EmailConfirmation +
                    "</p>\n\n<p>You have <b>30 minutes</b> to verify the account, otherwise it will be discarded.</p>\n\n<h2>Thanks for playing our game!</h2>\n\n<p><a href='https://madnessriotrage.com/'>MadnessRiotRage</a> email sent from in-game.</p>",
                    "Email Verification - Madness: Riot Rage", Email, "verification@madnessriotrage.com",
                    "Verification", Username);
            });
            t.Start();
            SPacketSignUp status = new SPacketSignUp();
            status.statusCode = Status.Okay;
            Program.QueuePacket(p, status);
        }
        catch
        {
            Program.QueueDisconnect(p.peer, (uint)Status.BadRequest);
        }

    }
}