using System;

namespace FilesHash.Services.LoggerService
{
    class ConsoleLoggerService : ILoggerService
    {
        public void Error(string Message)
        {
            Console.WriteLine($"[ FAIL ] [{CurrentDateTime()}] {Message}");
        }

        public void Info(string Message)
        {
            Console.WriteLine($"[ INFO ] [{CurrentDateTime()}] {Message}");
        }

        public void Success(string Message)
        {
            Console.WriteLine($"[ DONE ] [{CurrentDateTime()}] {Message}");
        }

        private string CurrentDateTime()
        {
            return DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        }
    }
}
