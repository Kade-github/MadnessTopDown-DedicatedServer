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
            byte[] info = new UTF8Encoding(true).GetBytes(value + "\n");
            stream.Write(info, 0, info.Length);
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
            Console.WriteLine(l);
        }
        
        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        /// <param name="text">The error message</param>
        public void Error(string text)
        {
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Error] " + text;
            
            AddText(l);
            Console.WriteLine(l);
        }
        
        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        /// <param name="text">The warning message</param>
        public void Warning(string text)
        {
            string l = "[" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "] [Warning] " + text;
            
            AddText(l);
            Console.WriteLine(l);
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