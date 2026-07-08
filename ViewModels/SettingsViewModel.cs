using Apolo.Services;
using CommunityToolkit.Mvvm.Input;
using Models;
using Repository;
using Serilog;
using System.Diagnostics;
using System.Text;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class SettingsViewModel(IGeneralRepository repository, IUserProfileService userProfile,
        Excel.IReader excelReader, Excel.IWriter excelWriter) : UserProfileViewModel(userProfile)
    {
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

            await repository.ClearDatabaseAsync();

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

            await repository.ClearArchiveAsync();

            SetExitFunction("Archive has been clear successfully.", InfoBarType.Success);
        }

        public static async Task<string> GenerateExportSummary(string folderPath,
            int serviceCount, int payerCount,
            int studentCount, int specificationCount,
            int lessonCount, int invoiceCount)
        {
            // 1. Create a descriptive filename with a timestamp
            string fileName = $"Summary_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            string fullPath = Path.Combine(folderPath, fileName);

            // 2. Build the content using a StringBuilder
            StringBuilder sb = new();
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

            try
            {
                var watch = Stopwatch.StartNew();

                await Task.Run(async () => await excelReader.ReadExcel(file));

                // Insert data into database
                await repository.ImportAllDataAsync(
                    excelReader.Services,
                    excelReader.Payers,
                    excelReader.Students,
                    excelReader.Specifications,
                    excelReader.Lessons,
                    excelReader.Invoices);

                string path = await GenerateExportSummary(root,
                    excelReader.Services.Count,
                    excelReader.Payers.Count,
                    excelReader.Students.Count,
                    excelReader.Specifications.Count,
                    excelReader.Lessons.Count,
                    excelReader.Invoices.Count);

                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

                SetExitFunction($"Import completed ({elapsedTime}). Summary saved to {path}", InfoBarType.Success);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Failed to import from Excel due to file lock.");
                SetExitFunction("The file is in use. Please close it in Excel and try again.", InfoBarType.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during Excel import.");
                SetExitFunction($"Import failed: {ex.Message}", InfoBarType.Error);
            }

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

            await Task.Run(async () => await excelReader.ReadExcel(file));

            // Insert data into database
            await repository.ImportArchiveAsync(
                excelReader.Payers,
                excelReader.Students,
                excelReader.Lessons,
                excelReader.Invoices);

            string path = await GenerateExportSummary(root,
                excelReader.Services.Count,
                excelReader.Payers.Count,
                excelReader.Students.Count,
                excelReader.Specifications.Count,
                excelReader.Lessons.Count,
                excelReader.Invoices.Count);

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

            var data = await repository.ExportArchiveAsync();
            excelWriter.WriteExcel(templatePath, Profile.BackupFolder, in data, archive: true);

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

            try
            {
                var watch = Stopwatch.StartNew();

                string templatePath = Path.Combine(installedPath, "Assets", "Excel", "Template.xlsx");

                var data = await repository.GetAllDataAsync();
                excelWriter.WriteExcel(templatePath, Profile.BackupFolder, in data);

                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

                SetExitFunction($"Export completed ({elapsedTime}). File saved to {Profile.BackupFolder}", InfoBarType.Success);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Failed to export to Excel due to file lock.");
                SetExitFunction("File is in use. Please close Excel and try again.", InfoBarType.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during database export.");
                SetExitFunction($"Export failed: {ex.Message}", InfoBarType.Error);
            }

        }

        public async Task<List<PayerActivityInfo>> GetPayersActivity() => await repository.GetPayersWithActivityAsync();

        public async Task<List<PayerOption>> GetPayersFromArchive() => await repository.GetPayersFromArchiveAsync();

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
                await repository.ArchiveOldDataAsync(payersIds);
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
                await repository.RetrieveDataFromArchiveAsync(payersIds);
                SetExitFunction("Data retrieved successfully from archive.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
