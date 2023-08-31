﻿using MailKit.Net.Smtp;
using MimeKit;

namespace DedicatedServer.Madness.Mail
{
    public class SMTP
    {
        public static void SendMessage(string message, string subject, string to, string from, string name)
        {
            var m = new MimeMessage();
            m.From.Add(new MailboxAddress(name, from));
            m.To.Add(new MailboxAddress("user", to));

            m.Subject = subject;

            m.Body = new TextPart("plain")
            {
                Text = message
            };

            using var client = new SmtpClient ();
            client.Connect ("127.0.0.1", 25, false);
                
            client.Send (m);
            client.Disconnect (true);
        }
    }
}