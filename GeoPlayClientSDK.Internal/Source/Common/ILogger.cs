using System;

namespace GeoPlayClientSDK.Internal.Common
{
    public interface ILogger
    {
        void Info(string message);
        void Error(string message);
        void Debug(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Info(string message) => Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} - {message}");
        public void Error(string message) => Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
        public void Debug(string message) => Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} - {message}");
    }
}