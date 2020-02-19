namespace FilesHash.Common
{
    class ProgramError
    {
        public string FileName { get; private set; }
        public string Message { get; private set; }

        public ProgramError(string fileName, string message)
        {
            FileName = fileName;
            Message = message;
        }

        public override string ToString()
        {
            return $"Имя и путь: {FileName}, Ошибка: {Message}";
        }
    }
}
