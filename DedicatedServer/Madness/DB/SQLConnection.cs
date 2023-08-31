using DedicatedServer.Madness.Secrets;
using MySqlConnector;

namespace DedicatedServer.Madness.DB
{
    public class SQLConnection
    {
        public static MySqlConnection OpenConnection(string addr, int port)
        {
            MySqlConnection c  = new MySqlConnection(
                $"Server={addr}; Port={port};database=madness; UID={Constants.DBUser}; password={Constants.DBPassword}");
            
            c.Open();
            return c;
        }
        
        
    }
}