namespace FilesHash.Services.LoggerService
{
    interface ILoggerService
    {
        void Error(string Message);
        void Info(string Message);
        void Success(string Message);
    }
}
