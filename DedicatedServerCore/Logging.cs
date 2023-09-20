using System;
using System.IO;
using System.Text;

namespace DedicatedServer
{
    public class Logging
    {
        private FileStream stream;

        /// <summary>
        /// Private function to add data to the FileStream.
        /// </summary>
        /// <param name="value">Data</param>
        private void AddText(string value)
        {
            Program.currentLog += value + "\n";
            if (Program.currentLog.Split(Environment.NewLine).Length > 100)
                Program.currentLog = Program.currentLog.Substring(0, Program.currentLog.Trim().IndexOf(Environment.NewLine, StringComparison.Ordinal));
            byte[] info = new UTF8Encoding(true).GetBytes(value + "\n");
            stream.Write(info, 0, info.Length);
            
            Console.WriteLine(value);
        }

        public static string FormatString(string s)
        {
            return "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] " + s;
        }
        
        /// <summary>
        /// Get the date and time formatted without the stupid stuff.
        /// </summary>
        /// <returns>The formatted time and date.</returns>
        private string GetDateTimeFormat()
        {
            return (DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString()).Replace(" ", "").Replace(",", "")
                .Replace(":", "");
        }

        /// <summary>
        /// Closes the FileStream, and flushes the contents to the file.
        /// </summary>
        public void Dispose()
        {
            stream.Flush();
            stream.Close();
        }
        
        /// <summary>
        /// Creates a directory called "logs" if it doesn't exist,
        /// then create a file with the current date and time and open it
        /// as a filestream.
        /// </summary>
        public Logging()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            string name = GetDateTimeFormat() + ".log";

            stream = File.Create("logs/" + name);
        }

        /// <summary>
        /// Writes an info message to the log.
        /// </summary>
        /// <param name="text">The info message.</param>
        public void Info(string text)
        {
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Info] " + text;
            
            AddText(l);
        }
        
        /// <summary>
        /// Writes a Debug message to the log.
        /// </summary>
        /// <param name="text">The info message.</param>
        public void Debug(string text)
        {
            #if DEBUG
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Debug] " + text;
            
            AddText(l);
            #endif
        }
        
        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        /// <param name="text">The error message</param>
        public void Error(string text)
        {
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Error] " + text;
            
            AddText(l);
        }
        
        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        /// <param name="text">The warning message</param>
        public void Warning(string text)
        {
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Warning] " + text;
            
            AddText(l);
        }
        
        /// <summary>
        /// Writes a fatal message to the log, calls Dispose() on end.
        /// </summary>
        /// <param name="text">The fatal message</param>
        public void Fatal(string text)
        {
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Fatal] " + text;
            
            AddText(l);
            Console.WriteLine(l);
            
            Dispose();
        }
    }
}