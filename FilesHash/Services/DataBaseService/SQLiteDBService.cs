using System.Data.SQLite;
using System.IO;

namespace FilesHash.Services.DataBaseService
{
    class SQLiteDBService : IDataBaseService
    {
        private const string DataBaseFilePath = @"database.db";

        public SQLiteDBService()
        {
            CreateDataBase();
            CreateDBSchema();
        }

        public void SaveError(string FileName, string ErrorMessage)
        {
            using (SQLiteConnection Connect = new SQLiteConnection($@"Data Source={DataBaseFilePath}; Version=3;"))
            {
                string SqlCommandText = $"INSERT INTO program_errors(fileName, errorMessage) VALUES (\"{FileName}\", \"{ErrorMessage.Replace("\"", "'")}\");";
                SQLiteCommand SqlCommand = new SQLiteCommand(SqlCommandText, Connect);

                Connect.Open();
                SqlCommand.ExecuteNonQuery();
                Connect.Close();
            }
        }

        public void SaveFileProcessResult(string FileName, string HashSum)
        {
            using (SQLiteConnection Connect = new SQLiteConnection($@"Data Source={DataBaseFilePath}; Version=3;"))
            {
                string SqlCommandText = $"INSERT INTO files_hash_sum(fileName, hashSum) VALUES (\"{FileName}\", \"{HashSum}\");";
                SQLiteCommand SqlCommand = new SQLiteCommand(SqlCommandText, Connect);

                Connect.Open();
                SqlCommand.ExecuteNonQuery();
                Connect.Close();
            }
        }

        public void ResetDataBase()
        {
            using (SQLiteConnection Connect = new SQLiteConnection($@"Data Source={DataBaseFilePath}; Version=3;"))
            {
                string[] SqlCommandsText = new string[2]
                {
                    "DELETE FROM files_hash_sum;",
                    "DELETE FROM program_errors;"
                };

                Connect.Open();
                foreach (string SqlCommandText in SqlCommandsText)
                {
                    SQLiteCommand SqlCommand = new SQLiteCommand(SqlCommandText, Connect);
                    SqlCommand.ExecuteNonQuery();
                }
                Connect.Close();
            }
        }

        private void CreateDataBase()
        {
            if (!File.Exists(DataBaseFilePath))
            {
                SQLiteConnection.CreateFile(DataBaseFilePath);
            }
        }

        private void CreateDBSchema()
        {
            using (SQLiteConnection Connect = new SQLiteConnection($@"Data Source={DataBaseFilePath}; Version=3;"))
            {
                string[] SqlCommandsText = new string[2]
                {
                    "CREATE TABLE IF NOT EXISTS files_hash_sum (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, fileName TEXT, hashSum TEXT);",
                    "CREATE TABLE IF NOT EXISTS program_errors (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, fileName TEXT, errorMessage TEXT);"
                };

                Connect.Open();
                foreach (string SqlCommandText in SqlCommandsText)
                {
                    SQLiteCommand SqlCommand = new SQLiteCommand(SqlCommandText, Connect);
                    SqlCommand.ExecuteNonQuery();
                }
                Connect.Close();
            }
        }

    }
}
