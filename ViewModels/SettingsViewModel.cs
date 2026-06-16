using Apolo.Services;
using CommunityToolkit.Mvvm.Input;
using Models;
using Repository;
using System.Diagnostics;
using System.Text;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class SettingsViewModel : UserProfileViewModel
    {
        IGeneralRepository _repository;
        Excel.IReader _excelReader;
        Excel.IWriter _excelWriter;

        public SettingsViewModel(IGeneralRepository repository, IUserProfileService userProfile, 
            Excel.IReader excelReader, Excel.IWriter excelWriter)
            : base(userProfile)
        {
            _repository = repository;
            _excelReader = excelReader;
            _excelWriter = excelWriter;
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't save settings while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            await _userProfileService.SaveAsync(Profile);

            SetExitFunction("User profile saved successfully.", InfoBarType.Success);
        }

        [RelayCommand]
        public async Task DeleteAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't delete settings while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            Profile = new UserProfile();

            await _userProfileService.SaveAsync(Profile);

            SetExitFunction("User profile deleted successfully.", InfoBarType.Success);
        }

        public async Task ClearDatabaseAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't clear database while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            await _repository.ClearDatabaseAsync();

            SetExitFunction("Database has been clear successfully.", InfoBarType.Success);
        }

        public async Task ClearArchiveAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't clear archive while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            await _repository.ClearArchiveAsync();

            SetExitFunction("Archive has been clear successfully.", InfoBarType.Success);
        }

        public async Task<string> GenerateExportSummary(string folderPath,
            int serviceCount, int payerCount,
            int studentCount, int specificationCount,
            int lessonCount, int invoiceCount)
        {
            // 1. Create a descriptive filename with a timestamp
            string fileName = $"Summary_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            string fullPath = Path.Combine(folderPath, fileName);

            // 2. Build the content using a StringBuilder
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===========================================");
            sb.AppendLine("       APOLO APP - IMPORT SUMMARY          ");
            sb.AppendLine("===========================================");
            sb.AppendLine($"Date: {DateTime.Now:f}");
            sb.AppendLine();
            sb.AppendLine("RESULTS:");
            sb.AppendLine($"- Services Imported: {serviceCount}");
            sb.AppendLine($"- Payers Imported: {payerCount}");
            sb.AppendLine($"- Students Imported: {studentCount}");
            sb.AppendLine($"- Specifications Imported: {specificationCount}");
            sb.AppendLine($"- Lessons Imported: {lessonCount}");
            sb.AppendLine($"- Invoices Processed: {invoiceCount}");
            sb.AppendLine();
            sb.AppendLine("STATUS: Success");
            sb.AppendLine("===========================================");
            sb.AppendLine("All data has been saved to the Excel file.");

            // 3. Write the file
            await File.WriteAllTextAsync(fullPath, sb.ToString());

            return fullPath;
        }

        public async Task ImportDatabaseFromExcel(string file)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't import database from Excel while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (string.IsNullOrWhiteSpace(file))
            {
                SetExitFunction("No file selected.", InfoBarType.Warning);
                return;
            }

            var root = Path.GetDirectoryName(file);
            if (!Directory.Exists(root))
            {
                SetExitFunction($"Directory '{root}' does not exist.", InfoBarType.Error);
                return;
            }

            var watch = Stopwatch.StartNew();

            await Task.Run(async () => await _excelReader.ReadExcel(file));

            // Insert data into database
            await _repository.ImportAllDataAsync(
                _excelReader.Services,
                _excelReader.Payers,
                _excelReader.Students,
                _excelReader.Specifications,
                _excelReader.Lessons,
                _excelReader.Invoices);

            string path = await GenerateExportSummary(root,
                _excelReader.Services.Count,
                _excelReader.Payers.Count,
                _excelReader.Students.Count,
                _excelReader.Specifications.Count,
                _excelReader.Lessons.Count,
                _excelReader.Invoices.Count);

            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);

            SetExitFunction($"Import completed ({elapsedTime}). Summary saved to {path}", InfoBarType.Success);
        }

        public async Task ImportArchiveFromExcel(string file)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't import archive from Excel while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (string.IsNullOrWhiteSpace(file))
            {
                SetExitFunction("No file selected.", InfoBarType.Warning);
                return;
            }

            var root = Path.GetDirectoryName(file);
            if (!Directory.Exists(root))
            {
                SetExitFunction($"Directory '{root}' does not exist.", InfoBarType.Error);
                return;
            }

            var watch = Stopwatch.StartNew();

            await Task.Run(async () => await _excelReader.ReadExcel(file));

            // Insert data into database
            await _repository.ImportArchiveAsync(
                _excelReader.Payers,
                _excelReader.Students,
                _excelReader.Lessons,
                _excelReader.Invoices);

            string path = await GenerateExportSummary(root,
                _excelReader.Services.Count,
                _excelReader.Payers.Count,
                _excelReader.Students.Count,
                _excelReader.Specifications.Count,
                _excelReader.Lessons.Count,
                _excelReader.Invoices.Count);

            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);

            SetExitFunction($"Import completed ({elapsedTime}). Summary saved to {path}", InfoBarType.Success);
        }

        public async Task ExportArchiveToExcel(string installedPath)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't export archive while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BackupFolder))
            {
                SetExitFunction($"Directory '{Profile.BackupFolder}' does not exist.", InfoBarType.Error);
                return;
            }

            var watch = Stopwatch.StartNew();

            string templatePath = Path.Combine(installedPath, "Assets", "Excel", "Template.xlsx");

            var data = await _repository.ExportArchiveAsync();
            _excelWriter.WriteExcel(templatePath, Profile.BackupFolder, in data, archive: true);

            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);

            SetExitFunction($"Export completed ({elapsedTime}). File saved to {Profile.BackupFolder}", InfoBarType.Success);
        }

        public async Task ExportDatabaseToExcel(string installedPath)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't export database while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BackupFolder))
            {
                SetExitFunction($"Directory '{Profile.BackupFolder}' does not exist.", InfoBarType.Error);
                return;
            }

            var watch = Stopwatch.StartNew();

            string templatePath = Path.Combine(installedPath, "Assets", "Excel", "Template.xlsx");

            var data = await _repository.GetAllDataAsync();
            _excelWriter.WriteExcel(templatePath, Profile.BackupFolder, in data);

            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);

            SetExitFunction($"Export completed ({elapsedTime}). File saved to {Profile.BackupFolder}", InfoBarType.Success);
        }

        public async Task<List<PayerActivityInfo>> GetPayersActivity() => await _repository.GetPayersWithActivityAsync();

        public async Task<List<PayerOption>> GetPayersFromArchive() => await _repository.GetPayersFromArchiveAsync();

        public async Task ArchiveOldData(List<Guid> payersIds)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't archive data while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (payersIds.Count == 0)
            {
                SetExitFunction("No payers were selected.", InfoBarType.Info);
                return;
            }

            try
            {
                await _repository.ArchiveOldDataAsync(payersIds);
                SetExitFunction("Archived data successfully.", InfoBarType.Success);
            }
            catch (Exception ex) 
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task RetrieveDataFromArchive(List<Guid> payersIds)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't retrieve data from archive while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (payersIds.Count == 0)
            {
                SetExitFunction("No payers were selected.", InfoBarType.Info);
                return;
            }

            try
            {
                await _repository.RetrieveDataFromArchiveAsync(payersIds);
                SetExitFunction("Data retrieved successfully from archive.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
