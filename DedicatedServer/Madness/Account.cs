using System;
using MySqlConnector;

namespace DedicatedServer.Madness
{
    public class Account
    {
        public string Username = "";
        public string Email = "";
        public string PasswordReset = "";
        public string PasswordSalt = "";
        public string PasswordHash = "";
        public string LastIP = "";
        
        public string EmailConfirmation = "";
        
        public bool Banned = false;
        public bool EmailConfirmed = false;

        public long CreationDate = 0;

        public Account()
        {
            
        }
        

    }
}