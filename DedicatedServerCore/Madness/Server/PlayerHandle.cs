using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DedicatedServer.Madness.Cryptography;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.MessagePack;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Secrets;
using ENet;
using MySqlConnector;
using RSA = DedicatedServer.Madness.Cryptography.RSA;

namespace DedicatedServer.Madness.Server;

public class PlayerHandle {

    public static List < Account > tempAccounts = new();
    public static List < Account ? > cachedAccounts = new();

    public static List < Player > Players = new();

    public static async Task BanPlayer(Account a)
    {
        a.Banned = true;

        await a.Export();
        
        // set banned in database

        if (await IsBanned(a.LastIP))
            return;
        
        MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);
        var insert = new MySqlCommand("INSERT INTO banned_ips (ip, username) VALUES (@Ip, @Username)");
        insert.Connection = connection;
        insert.Parameters.AddWithValue("@Ip", a.LastIP);
        insert.Parameters.AddWithValue("@Username", a.Username);

        await insert.PrepareAsync();

        await insert.ExecuteNonQueryAsync();
        
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }
    
    public static async Task BanIp(string ip)
    {
        // set banned in database

        if (await IsBanned(ip))
            return;
        
        MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);
        var insert = new MySqlCommand("INSERT INTO banned_ips (ip) VALUES (@Ip)");
        insert.Connection = connection;
        insert.Parameters.AddWithValue("@Ip", ip);

        await insert.PrepareAsync();

        await insert.ExecuteNonQueryAsync();
        
        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    public static async Task<bool> IsBanned(string ip)
    {
        MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);

        var query = new MySqlCommand("SELECT * FROM banned_ips WHERE ip = LOWER(@Ip)");
        query.Parameters.AddWithValue("@Ip", ip);
        query.Connection = connection;
        await query.PrepareAsync();
        
        var reader = await query.ExecuteReaderAsync();
        bool ret = reader.HasRows;
        await connection.CloseAsync();
        await connection.DisposeAsync();
        return ret;
    }
    
    public static async Task<bool> IsBannedUsername(string username)
    {
        MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);

        var query = new MySqlCommand("SELECT * FROM banned_ips WHERE username = LOWER(@Username)");
        query.Parameters.AddWithValue("@Username", username);
        query.Connection = connection;
        await query.PrepareAsync();
        
        var reader = await query.ExecuteReaderAsync();
        bool ret = reader.HasRows;
        await connection.CloseAsync();
        await connection.DisposeAsync();
        return ret;
    }

    public static async Task SetUpdated(string username) {
        Account a = cachedAccounts.FirstOrDefault(u => u.Username == username);

        if (a == null)
            return;

        await a.Update(); 
    }

    public static async Task < Account ? > GetAccount(string username, string email) {
        Account ? a;
        a = cachedAccounts.FirstOrDefault(u => String.Equals(u.Username, username, StringComparison.CurrentCultureIgnoreCase));
        if (a != null) return a;

        a = await Account.GetAccount(username, email); // MYSQL Call
        if (a == null)
            return null; // its null
        cachedAccounts.Add(a);

        return a;
    }

    public static async Task < Account ? > GetAccount(string username) {
        Account ? a;
        a = cachedAccounts.FirstOrDefault(u => String.Equals(u.Username, username, StringComparison.CurrentCultureIgnoreCase));
        if (a != null)
            return a;

        a = await Account.GetAccount(username); // MYSQL Call
        if (a == null)
            return null; // its null
        cachedAccounts.Add(a);

        return a;
    }

    public static void DeletePlayer(Peer p) {
        lock(Players) {
            Players.RemoveAll(pl => pl.peer.IP == p.IP && pl.peer.ID == p.ID);
        }
    }

    public static Player ? FindPlayer(Peer p) {
        Player ? pl = null;
        lock(Players) {
            pl = Players.FirstOrDefault(pl => pl.peer.IP == p.IP && pl.peer.ID == p.ID);
        }

        return pl;
    }

    /// <summary>
    /// Clean up tempAccounts, along with sending queued packets and disconnecting clients.
    /// </summary>
    /// <param name="check">Check stopwatch for temp accounts</param>
    public static void CheckTimersAndQueues(Stopwatch check) {
        if (!check.IsRunning)
            check.Start();

        if (check.Elapsed.Minutes >= 1) // check if temp accounts are being used
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List < Account > toRemove = new();

            foreach(Account a in tempAccounts) {
                if (a.EmailConfirmed) // remove early so we dont care, and have to loop through less.
                {
                    toRemove.Add(a);
                    continue;
                }
                if (currentTimestamp - a.CreationDate > 7200) // 2 hours
                {
                    toRemove.Add(a);
                }
            }

            tempAccounts.RemoveAll(m => toRemove.FirstOrDefault(mm => mm.Username == m.Username) != null); // linq :)
            check.Restart();
        }

        if (Monitor.TryEnter(Players)) {
            try {
                foreach(Player p in Players) {
                    // start player timers
                    if (!p.connectTime.IsRunning)
                        p.connectTime.Start();
                    if (!p.heartBeatWatch.IsRunning)
                        p.heartBeatWatch.Start();

                    if (p.timeSinceHeartbeat >= p.lastHeartbeat) {

                        if (p.heartBeatWatch.Elapsed.Seconds >= 1) {
                            p.heartBeatWatch.Restart();
                            p.lastHeartbeat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            SPacketHeartbeat heartBeat = new SPacketHeartbeat();
                            heartBeat.Number = RandomNumberGenerator.GetInt32(80000);
                            p.heartbeatNumber = heartBeat.Number;
                            if (p.connectTime.Elapsed.Seconds % 300 == 0) {
                                heartBeat.NewKey = AES.GenerateAIDS();
                                p.next_aes = heartBeat.NewKey;
                            }

                            PacketHandle.QueuePacket(p, heartBeat);
                        }
                    } else {
                        long diff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - p.timeSinceHeartbeat;
                        if (p.timeSinceHeartbeat == 0)
                            diff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - p.lastHeartbeat;
                        if (diff > 35) {
                            p.failedHeartbeats++;
                            if (p.failedHeartbeats > 3)
                                PacketHandle.QueueDisconnect(p.peer, (uint) Status.FuckYou);
                            else {
                                p.heartBeatWatch.Restart();
                                SPacketHeartbeat heartBeat = new SPacketHeartbeat();
                                heartBeat.Number = RandomNumberGenerator.GetInt32(80000);
                                p.heartbeatNumber = heartBeat.Number;
                                if (p.connectTime.Elapsed.Seconds % 300 == 0) {
                                    heartBeat.NewKey = AES.GenerateAIDS();
                                    p.next_aes = heartBeat.NewKey;
                                }

                                PacketHandle.QueuePacket(p, heartBeat);
                            }
                        }
                    }
                }
            } finally {
                Monitor.Exit(Players);
            }
        }
    }

    public static void HandlePacket(Player p, byte[] enc) {

        Madness.Packets.Packet pa;
        byte[] dec;
        if (p.next_aes.Length != 0) // should be a heartbeat packet
        {
            try {
                dec = AES.DecryptAESPacket(enc, p.next_aes);
                pa = Decreal.DeserializePacket(dec);
                if (pa.type != PacketType.CPacketHeartbeat)
                    return;
                pa.Handle(p);
                return;
            } catch {
                // no go
                return;
            }
        }
        dec = AES.DecryptAESPacket(enc, p.current_aes);

        pa = Decreal.DeserializePacket(dec);

        if (p.account == null)
        {
            if (pa.type != PacketType.CPacketHeartbeat && pa.type != PacketType.CPacketLogin && pa.type != PacketType.CPacketSignUp && pa.type != PacketType.CPacketVerifyEmail && pa.type != PacketType.CPacketResetPassword)
            {
                BanIp(p.peer.IP);
                PacketHandle.QueueDisconnect(p.peer, (uint) Status.Unauthorized);
                return;
            }
        }

        pa.Handle(p);
    }

    public static void NewConnection(Peer p, byte[] hello) {
        CPacketHello h = Decreal.DeserializePacket < CPacketHello > (hello);

        if (h.Version != Program.SupportedClientVersion) {
            PacketHandle.QueueDisconnect(p, (uint) Status.WrongVersion);
            return;
        }

        byte[] aesKey = RSA.Decrypt(h.AESKey);

        Player yooo = new Player(p);
        yooo.current_aes = aesKey;

        lock(Players) {
            Players.Add(yooo);
        }

        Program.log.Debug("Peer " + p.IP + " has connected with a good packet.");

        // Send packet back to say we got it

        SPacketHello sh = new SPacketHello();

        PacketHandle.SendPacket(yooo, sh);

    }
}