namespace FilesHash.Common
{
    class FileProcessResult
    {
        public string FileName { get; private set; }
        public string HashSum { get; private set; }

        public FileProcessResult(string fileName, string hashSum)
        {
            FileName = fileName;
            HashSum = hashSum;
        }

        public override string ToString()
        {
            return $"Имя и путь: {FileName}, Hash sum: {HashSum}";
        }
    }
}
