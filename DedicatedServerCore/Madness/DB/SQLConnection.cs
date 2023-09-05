using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using DedicatedServer.Madness.Secrets;
using MySqlConnector;

namespace DedicatedServer.Madness.DB
{
    public class SQLConnection
    {
        public static async Task<MySqlConnection> OpenConnection(string addr, int port)
        {
            DbConnectionStringBuilder csb = new DbConnectionStringBuilder();
            csb.ConnectionString =
                $"Server={addr}; Port={port};database=users; UID={Constants.DBUser}; password={Constants.DBPassword};Connection Timeout=2;";
            MySqlConnection c  = new MySqlConnection(csb.ConnectionString);
            
            await c.OpenAsync();

            if (c.State != ConnectionState.Open)
            {
                throw new Exception("Failed to connect to server " + addr + ":" + port + ".");
            }
            return c;
        }
        
        
    }
}