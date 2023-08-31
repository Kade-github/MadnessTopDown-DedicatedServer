using System;
using MySqlConnector;

namespace DedicatedServer.Madness
{
    public class Account
    {
        public string Username = "";
        public string Email = "";
        public string PasswordReset = "";
        public string PasswordHash = "";
        public string LastIP = "";
        
        public bool Banned = false;
        public bool EmailConfirmed = false;

        public DateTime CreationDate = DateTime.Now;

        protected Account()
        {
            
        }
        

    }
}