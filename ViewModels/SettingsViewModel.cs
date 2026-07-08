using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using Repository;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using ViewModels;

namespace Apolo.ViewModels
{
    

    public partial class SettingsViewModel : UserProfileViewModel
    {
        readonly IGeneralRepository _repository;
        readonly Excel.IReader _excelReader;
        readonly Excel.IWriter _excelWriter;
        readonly ILanguageService _languageService;

        public ObservableCollection<LanguageOption> Languages { get; } =
        [
            new LanguageOption { DisplayName = "System Default / Idioma del Sistema", Code = "" },
            new LanguageOption { DisplayName = "English", Code = "en-US" },
            new LanguageOption { DisplayName = "Español", Code = "es-ES" }
        ];

        [ObservableProperty] private LanguageOption _selectedLenguage;

        // Message
        private static string Message_Save_Settings_Error => "Messages/Save_Settings_Error";
        private static string Message_Save_Settings_Success => "Messages/Save_Settings_Success";
        private static string Message_Delete_Settings_Error => "Messages/Delete_Settings_Error";
        private static string Message_Delete_Settings_Success => "Messages/Delete_Settings_Success";
        private static string Message_Delete_Database_Error => "Messages/Delete_Database_Error";
        private static string Message_Delete_Database_Success => "Messages/Delete_Database_Success";
        private static string Message_Delete_Archive_Error => "Messages/Delete_Archive_Error";
        private static string Message_Delete_Archive_Success => "Messages/Delete_Archive_Success";
        private static string Message_Import_Database_Error => "Messages/Import_Database_Error";
        private static string Message_Import_Database_Success => "Messages/Import_Database_Success";
        private static string Message_Import_Archive_Error => "Messages/Import_Archive_Error";
        private static string Message_Import_Archive_Success => "Messages/Import_Archive_Success";
        private static string Message_Export_Database_Error => "Messages/Export_Database_Error";
        private static string Message_Export_Database_Success => "Messages/Export_Database_Success";
        private static string Message_Export_Archive_Error => "Messages/Export_Archive_Error";
        private static string Message_Export_Archive_Success => "Messages/Export_Archive_Success";
        private static string Message_Archive_Error => "Messages/Archive_Error";
        private static string Message_Archive_Success => "Messages/Archive_Success";
        private static string Message_Retrieve_Archive_Error => "Messages/Retrieve_Archive_Error";
        private static string Message_Retrieve_Archive_Success => "Messages/Retrieve_Archive_Success";
        private static string Message_Export_Header => "Messages/Settings_Export_Header";
        private static string Message_Export_Date => "Messages/Settings_Export_Date";
        private static string Message_Export_Results => "Messages/Settings_Export_Results";
        private static string Message_Export_Services => "Messages/Settings_Export_Services";
        private static string Message_Export_Payers => "Messages/Settings_Export_Payers";
        private static string Message_Export_Students => "Messages/Settings_Export_Students";
        private static string Message_Export_Specifications => "Messages/Settings_Export_Specifications";
        private static string Message_Export_Lessons => "Messages/Settings_Export_Lessons";
        private static string Message_Export_Invoices => "Messages/Settings_Export_Invoices";
        private static string Message_No_File_Reason => "Messages/No_File_Reason";
        private static string Message_No_Directory_Reason => "Messages/No_Directory_Reason";
        private static string Message_Excel_Used_Reason => "Messages/Excel_Used_Reason";
        private static string Message_Backup_Folder_Reason => "Messages/Backup_Folder_Reason";
        private static string Message_Payer_Selection_Reason => "Messages/Payer_Selection_Reason";


        public SettingsViewModel(IGeneralRepository repository, IUserProfileService userProfile,
            Excel.IReader excelReader, Excel.IWriter excelWriter, ILanguageService languageService, 
            IStringLocalizer stringLocalizer)
            : base(userProfile, stringLocalizer)
        {
            _repository = repository;
            _excelReader = excelReader;
            _excelWriter = excelWriter;
            _languageService = languageService;

            // Match the stored profile language string to our dropdown items
            SelectedLenguage = Languages.FirstOrDefault(l => l.Code == Profile.Language) ?? Languages.First();
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Save_Settings_Error);
                return;
            }

            SetEnterFunction();

            Profile.Language = SelectedLenguage.Code;

            await _userProfileService.SaveAsync(Profile);

            _languageService.ApplyLanguage(Profile.Language);

            SetExitFunction($"{_loc.Get(Message_Save_Settings_Success)}.", InfoBarType.Success);
        }

        [RelayCommand]
        public async Task DeleteAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Delete_Settings_Error);
                return;
            }

            SetEnterFunction();

            Profile = new UserProfile();

            await _userProfileService.SaveAsync(Profile);

            SetExitFunction($"{_loc.Get(Message_Delete_Settings_Success)}.", InfoBarType.Success);
        }

        public async Task ClearDatabaseAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Delete_Database_Error);
                return;
            }

            SetEnterFunction();

            await _repository.ClearDatabaseAsync();

            SetExitFunction($"{_loc.Get(Message_Delete_Database_Success)}.", InfoBarType.Success);
        }

        public async Task ClearArchiveAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Delete_Archive_Error);
                return;
            }

            SetEnterFunction();

            await _repository.ClearArchiveAsync();

            SetExitFunction($"{_loc.Get(Message_Delete_Archive_Success)}.", InfoBarType.Success);
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
            StringBuilder sb = new();
            sb.AppendLine("===========================================");
            sb.AppendLine($"       APOLO APP - {_loc.Get(Message_Export_Header)}          ");
            sb.AppendLine("===========================================");
            sb.AppendLine($"{_loc.Get(Message_Export_Date)}: {DateTime.Now:f}");
            sb.AppendLine();
            sb.AppendLine($"{_loc.Get(Message_Export_Results)}:");
            sb.AppendLine($"- {_loc.Get(Message_Export_Services)}: {serviceCount}");
            sb.AppendLine($"- {_loc.Get(Message_Export_Payers)}: {payerCount}");
            sb.AppendLine($"- {_loc.Get(Message_Export_Students)}: {studentCount}");
            sb.AppendLine($"- {_loc.Get(Message_Export_Specifications)}: {specificationCount}");
            sb.AppendLine($"- {_loc.Get(Message_Export_Lessons)}: {lessonCount}");
            sb.AppendLine($"- {_loc.Get(Message_Export_Invoices)}: {invoiceCount}");
            sb.AppendLine();
            sb.AppendLine("===========================================");

            // 3. Write the file
            await File.WriteAllTextAsync(fullPath, sb.ToString());

            return fullPath;
        }

        public async Task ImportDatabaseFromExcel(string file)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Import_Database_Error);
                return;
            }

            SetEnterFunction();

            if (string.IsNullOrWhiteSpace(file))
            {
                SetExitFunction($"{_loc.Get(Message_Import_Database_Error)}: {_loc.Get(Message_No_File_Reason)}.", InfoBarType.Warning);
                return;
            }

            var root = Path.GetDirectoryName(file);
            if (!Directory.Exists(root))
            {
                SetExitFunction($"{_loc.Get(Message_Import_Database_Error)}: {_loc.Get(Message_No_Directory_Reason, root ?? string.Empty)}.", InfoBarType.Error);
                return;
            }

            try
            {
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
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

                SetExitFunction($"{_loc.Get(Message_Import_Database_Success, elapsedTime, path)}.", InfoBarType.Success);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Failed to import from Excel due to file lock.");
                SetExitFunction($"{_loc.Get(Message_Import_Database_Error)}:{_loc.Get(Message_Excel_Used_Reason)}.", InfoBarType.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during Excel import.");
                SetExitFunction($"{_loc.Get(Message_Import_Database_Error)}: {ex.Message}", InfoBarType.Error);
            }

        }

        public async Task ImportArchiveFromExcel(string file)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Import_Archive_Error);
                return;
            }

            SetEnterFunction();

            if (string.IsNullOrWhiteSpace(file))
            {
                SetExitFunction($"{_loc.Get(Message_Import_Archive_Error)}: {_loc.Get(Message_No_File_Reason)}.", InfoBarType.Warning);
                return;
            }

            var root = Path.GetDirectoryName(file);
            if (!Directory.Exists(root))
            {
                SetExitFunction($"{_loc.Get(Message_Import_Archive_Error)}: {_loc.Get(Message_No_Directory_Reason, root ?? string.Empty)}.", InfoBarType.Error);
                return;
            }

            try
            {

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

                SetExitFunction($"{_loc.Get(Message_Import_Archive_Success, elapsedTime, path)}.", InfoBarType.Success);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Failed to import from Excel due to file lock.");
                SetExitFunction($"{_loc.Get(Message_Import_Archive_Error)}:{_loc.Get(Message_Excel_Used_Reason)}.", InfoBarType.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during Excel import.");
                SetExitFunction($"{_loc.Get(Message_Import_Archive_Error)}: {ex.Message}", InfoBarType.Error);
            }
        }

        public async Task ExportArchiveToExcel(string installedPath)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Export_Database_Error);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BackupFolder))
            {
                SetExitFunction($"{_loc.Get(Message_Export_Database_Error)}: {_loc.Get(Message_Backup_Folder_Reason)}.", InfoBarType.Warning);
                return;
            }

            try
            {
                var watch = Stopwatch.StartNew();

                string templatePath = Path.Combine(installedPath, "Assets", "Excel", "Template.xlsx");

                var data = await _repository.ExportArchiveAsync();
                _excelWriter.WriteExcel(templatePath, Profile.BackupFolder, in data, archive: true);

                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

                SetExitFunction($"{_loc.Get(Message_Export_Database_Success, elapsedTime, Profile.BackupFolder)}.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during Excel export.");
                SetExitFunction($"{_loc.Get(Message_Export_Database_Error)}: {ex.Message}", InfoBarType.Error);
            }
        }

        public async Task ExportDatabaseToExcel(string installedPath)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Export_Archive_Error);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BackupFolder))
            {
                SetExitFunction($"{_loc.Get(Message_Export_Archive_Error)}: {_loc.Get(Message_Backup_Folder_Reason)}.", InfoBarType.Warning);
                return;
            }

            try
            {
                var watch = Stopwatch.StartNew();

                string templatePath = Path.Combine(installedPath, "Assets", "Excel", "Template.xlsx");

                var data = await _repository.GetAllDataAsync();
                _excelWriter.WriteExcel(templatePath, Profile.BackupFolder, in data);

                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

                SetExitFunction($"{_loc.Get(Message_Export_Archive_Success, elapsedTime, Profile.BackupFolder)}.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred during Excel export.");
                SetExitFunction($"{_loc.Get(Message_Export_Archive_Error)}: {ex.Message}", InfoBarType.Error);
            }

        }

        public async Task<List<PayerActivityInfo>> GetPayersActivity() => await _repository.GetPayersWithActivityAsync();

        public async Task<List<PayerOption>> GetPayersFromArchive() => await _repository.GetPayersFromArchiveAsync();

        public async Task ArchiveOldData(List<Guid> payersIds)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Archive_Error);
                return;
            }

            SetEnterFunction();

            if (payersIds.Count == 0)
            {
                SetExitFunction($"{_loc.Get(Message_Archive_Error)}: {_loc.Get(Message_Payer_Selection_Reason)}.", InfoBarType.Info);
                return;
            }

            try
            {
                await _repository.ArchiveOldDataAsync(payersIds);
                SetExitFunction($"{_loc.Get(Message_Archive_Success)}.", InfoBarType.Success);
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
                SetExitBusy(Message_Retrieve_Archive_Error);
                return;
            }

            SetEnterFunction();

            if (payersIds.Count == 0)
            {
                SetExitFunction($"{_loc.Get(Message_Retrieve_Archive_Error)}: {_loc.Get(Message_Payer_Selection_Reason)}.", InfoBarType.Info);
                return;
            }

            try
            {
                await _repository.RetrieveDataFromArchiveAsync(payersIds);
                SetExitFunction($"{_loc.Get(Message_Retrieve_Archive_Success)}.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
