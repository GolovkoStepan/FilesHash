namespace FilesHash.Services.DataBaseService
{
    interface IDataBaseService
    {
        void SaveFileProcessResult(string FileName, string HashSum);
        void SaveError(string FileName, string ErrorMessage);
        void ResetDataBase();
    }
}
