using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DedicatedServer.Madness.DB;
using DedicatedServer.Madness.Packets;
using DedicatedServer.Madness.Secrets;
using MySqlConnector;

namespace DedicatedServer.Madness
{
    public class Account
    {
        public string Username = "";
        public string Email = "";
        public string PasswordReset = "";
        public byte[] PasswordSalt;
        public string PasswordHash = "";
        public string LastIP = "";

        public Stopwatch lastUsed = new();
        
        public string EmailConfirmation = "";

        public bool Admin = false;
        public bool Banned = false;
        public bool EmailConfirmed = false;

        public long CreationDate = 0;

        public Account()
        {
            
        }

        public Account(string u, string e, byte[] s, string ph, string li, bool ad, bool ba, long cre)
        {
            Username = u;
            Email = e;
            PasswordSalt = s;
            PasswordHash = ph;
            LastIP = li;
            Admin = ad;
            Banned = ba;
            CreationDate = cre;
        }

        
        /// <summary>
        /// Mysql call to update the account from the database.
        /// </summary>
        public async Task Update()
        {
            MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);

            var query = new MySqlCommand("SELECT * FROM users WHERE username = LOWER(@Username)");
            query.Parameters.AddWithValue("@Username", Username.ToLower());
            query.Connection = connection;

            await query.PrepareAsync();
        
            var reader = await query.ExecuteReaderAsync();

            if (reader.HasRows)
            {
                string _username = reader.GetString("username");
                string _email = reader.GetString("email");
                string _passwordHash = reader.GetString("password_hash");
                byte[] _salt = Convert.FromBase64String(reader.GetString("salt"));
                long _creationDate = reader.GetInt64("creationdate");
                string _lastIp = reader.GetString("last_ip");
                bool _admin = reader.GetBoolean("admin");
                bool _banned = reader.GetBoolean("banned");
                Username = _username;
                Email = _email;
                PasswordHash = _passwordHash;
                PasswordSalt = _salt;
                CreationDate = _creationDate;
                LastIP = _lastIp;
                Admin = _admin;
                Banned = _banned;
                EmailConfirmed = true;
            }

            await connection.CloseAsync();

            await connection.DisposeAsync();
        }

        /// <summary>
        /// MySQL call to export an account to the database.
        /// </summary>
        public async Task Export()
        {
            MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);
            var insert = new MySqlCommand("INSERT INTO users (username, email, password_hash, salt, creationdate, email_confirmed, last_ip) VALUES (@Username, @Email, @PasswordHash, @Salt, @CreationDate, 1, @LastIp)");
            insert.Connection = connection;
            insert.Parameters.AddWithValue("@Username", Username);
            insert.Parameters.AddWithValue("@Email", Email);
            insert.Parameters.AddWithValue("@PasswordHash", PasswordHash);
            insert.Parameters.AddWithValue("@Salt", Convert.ToBase64String(PasswordSalt));
            insert.Parameters.AddWithValue("@CreationDate", CreationDate);
            insert.Parameters.AddWithValue("@LastIp", LastIP);
            await insert.PrepareAsync();

            await insert.ExecuteNonQueryAsync();
        
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }
        
        /// <summary>
        /// Mysql call to get an account from the database.
        /// </summary>
        public static async Task<Account?> GetAccount(string username)
        {
            MySqlConnection connection = await SQLConnection.OpenConnection(Constants.DBHost, Constants.DBPort);

            var query = new MySqlCommand("SELECT * FROM users WHERE username = LOWER(@Username)");
            query.Parameters.AddWithValue("@Username", username.ToLower());
            query.Connection = connection;
            await query.PrepareAsync();
        
            var reader = await query.ExecuteReaderAsync();
            Account? ret = null;
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string _username = reader.GetString("username");
                    string _email = reader.GetString("email");
                    string _passwordHash = reader.GetString("password_hash");
                    byte[] _salt = Convert.FromBase64String(reader.GetString("salt"));
                    long _creationDate = reader.GetInt64("creationdate");
                    string _lastIp = reader.GetString("last_ip");
                    bool _admin = reader.GetBoolean("admin");
                    bool _banned = reader.GetBoolean("banned");
                    ret = new Account(_username, _email, _salt, _passwordHash, _lastIp, _admin, _banned, _creationDate);
                }
            }
            await connection.CloseAsync();
            await connection.DisposeAsync();
            return ret;
        }
    }
}