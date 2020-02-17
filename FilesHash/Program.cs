using FilesHash.Services.DataBaseService;
using FilesHash.Services.LoggerService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace FilesHash
{
    class Program
    {
        // Queues of names, errors and processing results
        private static readonly Queue<string> FileNames = new Queue<string>();
        private static readonly Queue<ProgramError> ProgramErrors = new Queue<ProgramError>();
        private static readonly Queue<FileProcessResult> FileProcessResults = new Queue<FileProcessResult>();
        // Services
        private static readonly IDataBaseService DataBaseService = new SQLiteDBService();
        private static readonly ILoggerService LoggerService = new ConsoleLoggerService();
        // Multithreading objects
        private static bool FileSearchingEnd = false;
        private static bool FilesProcessingEnd = false;
        private static readonly object FileNamesLocker = new object();
        private static readonly object ProgramErrorsLocker = new object();
        private static readonly object FileProcessResultsLocker = new object();

        static void Main()
        {
            Console.WriteLine("Очистить базу данных перед началом работы программы? [Yes]");
            string userAnswer = Console.ReadLine();

            if (userAnswer.ToLower() == "yes")
            {
                Console.Clear();
                DataBaseService.ResetDataBase();
                LoggerService.Success("База данных очищена!");
                Thread.Sleep(2000);
                Console.Clear();
            }

            Console.WriteLine("Введите целевую директорию.");
            string dir = Console.ReadLine();
            Console.Clear();

            if (!Directory.Exists(dir))
            {
                LoggerService.Error($"Директория [{dir}] не существует!");
                DataBaseService.SaveError(dir, "Данная директория не существует!");
                LoggerService.Success("Данные об ошибке сохранены в БД.");
                Console.ReadKey();

                return;
            }

            Thread FilesSearchWorker = new Thread(() => FileSearchWorkerContext(dir));
            Thread DBSaverWorker = new Thread(() => DBSaverWorkerContext());

            for (int i = 0; i < 5; i++)
            {
                new Thread(() => FilesProcessWorkerContext()).Start();
            }

            FilesSearchWorker.Start();
            DBSaverWorker.Start();

            Console.ReadKey();
        }

        private static void DBSaverWorkerContext()
        {
            while (!FilesProcessingEnd || FileProcessResults.Count > 0 || ProgramErrors.Count > 0)
            {
                if (FileProcessResults.Count == 0 && ProgramErrors.Count == 0)
                {
                    continue;
                }

                FileProcessResult currentFileProcessResult = null;
                ProgramError currentProgramError = null;

                lock (FileProcessResultsLocker)
                {
                    try
                    {
                        currentFileProcessResult = FileProcessResults.Dequeue();
                    }
                    catch (InvalidOperationException) { }
                }

                lock(ProgramErrorsLocker)
                { 
                    try
                    {
                        currentProgramError = ProgramErrors.Dequeue();
                    }
                    catch (InvalidOperationException) { }

                }

                if (currentFileProcessResult == null && currentProgramError == null)
                {
                    continue;
                }

                if (currentFileProcessResult != null)
                {
                    DataBaseService.SaveFileProcessResult(currentFileProcessResult.FileName, currentFileProcessResult.HashSum);
                    LoggerService.Success($"Данные сохранены. {currentFileProcessResult}");
                }

                if (currentProgramError != null)
                {
                    DataBaseService.SaveError(currentProgramError.FileName, currentProgramError.Message);
                    LoggerService.Success($"Ошибка сохранена. {currentProgramError}");
                }
            }
        }

        private static void FilesProcessWorkerContext()
        {
            while (!FileSearchingEnd || FileNames.Count > 0)
            {
                if (FileNames.Count == 0)
                {
                    continue;
                }

                string currentFileName = "";

                lock (FileNamesLocker)
                {
                    try
                    {
                        currentFileName = FileNames.Dequeue();
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                }

                string hashSum = ComputeMD5Checksum(currentFileName);

                if (hashSum != null)
                {
                    FileProcessResult currentFileProcessResult;

                    lock (FileProcessResultsLocker)
                    {
                        currentFileProcessResult = new FileProcessResult(currentFileName, hashSum);
                    }

                    FileProcessResults.Enqueue(currentFileProcessResult);
                    LoggerService.Info($"Расчет выполнен. {currentFileProcessResult}");
                }
            }

           
            FilesProcessingEnd = true;
        }

        private static void FileSearchWorkerContext(string dir)
        {
            ProcessDirectory(dir);
            FileSearchingEnd = true;
        }

        public static void ProcessDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                ProcessFile(fileName);
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory);
            }
        }

        public static void ProcessFile(string path)
        {
            LoggerService.Info($"Обнаружен файл: {path}");

            lock (FileNamesLocker)
            {
                FileNames.Enqueue(path);
            }
        }

        public static string ComputeMD5Checksum(string path)
        {
            try
            {
                using (var FileOpen = File.OpenRead(path))
                using (var MdHash = new MD5CryptoServiceProvider())
                {
                    return BitConverter.ToString(MdHash.ComputeHash(FileOpen)).Replace("-", string.Empty);
                }
            }
            catch (Exception e)
            {
                ProgramError programError = new ProgramError(path, e.Message);

                lock (ProgramErrorsLocker)
                {
                    ProgramErrors.Enqueue(programError);
                }

                LoggerService.Error($"Ошибка при обработке файла: {path}");

                return null;
            }
        }
    }

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