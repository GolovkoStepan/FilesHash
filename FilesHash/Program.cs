using FilesHash.Common;
using FilesHash.Services.DataBaseService;
using FilesHash.Services.LoggerService;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace FilesHash
{
    class Program
    {
        // Queues of names, errors and processing results
        private static readonly SynchronizedQueue<string> FileNames = new SynchronizedQueue<string>();
        private static readonly SynchronizedQueue<ProgramError> ProgramErrors = new SynchronizedQueue<ProgramError>();
        private static readonly SynchronizedQueue<FileProcessResult> FileProcessResults = new SynchronizedQueue<FileProcessResult>();
        // Services
        private static readonly IDataBaseService DataBaseService = new SQLiteDBService();
        private static readonly ILoggerService LoggerService = new ConsoleLoggerService();
        // Counters
        private static int FilesCounter = 0;
        private static int ProcessCounter = 0;
        private static int DBSaveCounter = 0;
        // End flags
        private static volatile bool FileSearchingEnd = false;
        private static volatile bool FilesProcessingEnd = false;
        // File processing threads count
        private const int FileProcessThreadsCounter = 5;

        static void Main()
        {
            Console.WriteLine("Очистить базу данных перед началом работы программы? [Yes]");
            string userAnswer = Console.ReadLine();

            if (userAnswer.ToLower() == "yes")
            {
                Console.Clear();
                DataBaseService.ResetDataBase();
                LoggerService.Success("База данных очищена!");
                Thread.Sleep(1500);
                Console.Clear();
            }

            Console.Clear();
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
            FilesSearchWorker.Start();

            Thread[] FileProcessThreads = new Thread[FileProcessThreadsCounter];
            for (int i = 0; i < FileProcessThreadsCounter; i++)
            {
                FileProcessThreads[i] = new Thread(() => FilesProcessWorkerContext());
                FileProcessThreads[i].Start();
            }

            Thread DBSaverWorker = new Thread(() => DBSaverWorkerContext());
            DBSaverWorker.Start();

            DBSaverWorker.Join();

            Console.WriteLine("");
            Console.WriteLine("=====================================");
            Console.WriteLine($"===    Найдено файлов: {FilesCounter}");
            Console.WriteLine($"=== Обработано файлов: {ProcessCounter}");
            Console.WriteLine($"===    Сохранено в БД: {DBSaveCounter}");
            Console.WriteLine("=====================================");
            if (FilesCounter == ProcessCounter && FilesCounter == DBSaveCounter)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Выполено успешно!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Выполено с ошибками!");
                Console.ResetColor();
            }

            Console.ReadKey();
        }

        private static void DBSaverWorkerContext()
        {
            while (!FilesProcessingEnd || FileProcessResults.Count > 0 || ProgramErrors.Count > 0)
            {
                if (FileProcessResults.Count > 0)
                {
                    if (FileProcessResults.TryDequeue(out FileProcessResult currentFileProcessResult))
                    {
                        DataBaseService.SaveFileProcessResult(currentFileProcessResult.FileName, currentFileProcessResult.HashSum);
                        LoggerService.Success($"Данные сохранены. {currentFileProcessResult}");
                    }
                    DBSaveCounter++;
                }

                if (ProgramErrors.Count > 0)
                {
                    if (ProgramErrors.TryDequeue(out ProgramError currentProgramError))
                    {
                        DataBaseService.SaveError(currentProgramError.FileName, currentProgramError.Message);
                        LoggerService.Success($"Ошибка сохранена. {currentProgramError}");
                    }
                    DBSaveCounter++;
                }
            }
        }

        private static void FilesProcessWorkerContext()
        {
            while (!FileSearchingEnd || FileNames.Count > 0)
            {
                if (FileNames.TryDequeue(out string currentFileName))
                {
                    string hashSum = ComputeMD5Checksum(currentFileName);

                    if (hashSum != null)
                    {
                        FileProcessResult currentFileProcessResult;

                        currentFileProcessResult = new FileProcessResult(currentFileName, hashSum);

                        FileProcessResults.Enqueue(currentFileProcessResult);
                        LoggerService.Info($"Расчет выполнен. {currentFileProcessResult}");
                        Interlocked.Increment(ref ProcessCounter);
                    }
                }
            }

            FilesProcessingEnd = true;
            FileProcessResults.StopEnqueue();
        }

        private static void FileSearchWorkerContext(string dir)
        {
            ProcessDirectory(dir);
            FileSearchingEnd = true;
            FileNames.StopEnqueue();
        }

        public static void ProcessDirectory(string targetDirectory)
        {
            try
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
            catch (Exception e)
            {
                ProgramError programError = new ProgramError(targetDirectory, e.Message);
                ProgramErrors.Enqueue(programError);
                LoggerService.Error($"Ошибка при обработке файла: {targetDirectory}");
            }
        }

        public static void ProcessFile(string path)
        {
            LoggerService.Info($"Обнаружен файл: {path}");
            FileNames.Enqueue(path);
            FilesCounter++;
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
                ProgramErrors.Enqueue(programError);
                LoggerService.Error($"Ошибка при обработке файла: {path}");

                return null;
            }
        }
    }
}