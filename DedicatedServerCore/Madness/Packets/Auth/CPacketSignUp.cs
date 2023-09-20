using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Helpers;
using DedicatedServer.Madness.Mail;
using DedicatedServer.Madness.Secrets;
using MessagePack;
using MySqlConnector;
using Org.BouncyCastle.Crypto.Generators;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Server;

namespace DedicatedServer.Madness.Auth.Packets;

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
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }
								
        if (Username.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }

        if (Username.Length >= 16)
        {
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }
								
        if (Username.Length < 4)
        {
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }

        if (!SMTP.IsEmailGood(Email))
        {
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.BadRequest);
            return;
        }
        
        Account ac = PlayerHandle.tempAccounts.FirstOrDefault(a => a.Email == Email || a.Username == Username);
        if (ac != null)
        {
            SPacketSignUp status = new SPacketSignUp();
            status.statusCode = Status.UserAlreadyExists;
            PacketHandle.QueuePacket(p, status);
            return;
        }
        
        MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);
        var query = new MySqlCommand("SELECT * FROM users WHERE username = LOWER(@Username) or email = LOWER(@Email)");
        query.Parameters.AddWithValue("@Username", Username.ToLower());
        query.Parameters.AddWithValue("@Email", Email.ToLower());
        query.Connection = connection;

        await query.PrepareAsync();
        
        var reader = await query.ExecuteReaderAsync();

        if (reader.HasRows)
        {
            SPacketSignUp status = new SPacketSignUp();
            status.statusCode = Status.UserAlreadyExists;
            PacketHandle.QueuePacket(p, status);
            await connection.CloseAsync();

            await connection.DisposeAsync();
            return;
        }
        await connection.CloseAsync();

        await connection.DisposeAsync();
        
        
        
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        string base64 = HashPassword.HashIntoBase64(Password, salt);

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
        
        PlayerHandle.tempAccounts.Add(a);

        try
        {
            Thread t = new Thread(() =>
            {
                SMTP.SendMessage(
                    "<h1>Hello " + Username + "</h1>\n<p>Here is your verification key: " + a.EmailConfirmation +
                    "</p>\n\n<p>You have <b>2 hours</b> to verify the account, otherwise it will be discarded.</p>\n\n<h2>Thanks for playing our game!</h2>\n\n<p><a href='https://madnessriotrage.com/'>MadnessRiotRage</a> email sent from in-game.</p>",
                    "Email Verification - Madness: Riot Rage", Email, "verification@madnessriotrage.com",
                    "Verification", Username);
                Program.log.Info("Sent email to " + Email + ", with verification code: " + a.EmailConfirmation);
            });
            t.Start();
            SPacketSignUp status = new SPacketSignUp();
            status.statusCode = Status.Okay;
            PacketHandle.QueuePacket(p, status);
            
            p.AddLog("Registered " + Username);
        }
        catch
        {
            PacketHandle.QueueDisconnect(p.peer, (uint)Status.BadRequest);
        }

    }
}