using DedicatedServer.Madness.Secrets;
using MySqlConnector;

namespace DedicatedServer.Madness.DB
{
    public class SQLConnection
    {
        public MySqlConnection connection;

        public SQLConnection(string addr, int port)
        {
            connection = new MySqlConnection(
                $"Server={addr}; Port={port};database=madness; UID={Constants.DBUser}; password={Constants.DBPassword}");
            
            connection.Open();
        }
    }
}