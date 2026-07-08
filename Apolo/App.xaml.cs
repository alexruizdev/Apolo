using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Repository;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ViewModels;
using Windows.Storage;

namespace Apolo
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }

        public App()
        {
            // Initialize Logger
            string logFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs", "apolo_log_.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            Log.Information("Apolo App starting up...");

            // Hook up global exception handlers
            UnhandledException += App_UnhandledExceptionAsync;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Perform automated backups before the DB is locked by EF Core
            PerformAutomatedBackup();

            Services = ConfigureServices;
            InitializeDatabase();
            InitializeComponent();
        }

        private static IServiceProvider ConfigureServices
        {
            get
            {
                var builder = new ServiceCollection();

                // Use a helper to build the connection string with proper flags
                static string GetConnectionString(string path)
                {
                    var connectionStringBuilder = new SqliteConnectionStringBuilder
                    {
                        DataSource = path,
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        DefaultTimeout = 5, // This is the equivalent of Busy Timeout (in seconds)
                        Cache = SqliteCacheMode.Shared
                    };
                    return connectionStringBuilder.ToString();
                }

                var dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "apolo.db");
                var archiveDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "apolo_archive.db");

                builder.AddDbContext<ApoloContext>(options =>
                    options.UseSqlite(GetConnectionString(dbPath)));

                builder.AddDbContext<ApoloArchiveContext>(options =>
                    options.UseSqlite(GetConnectionString(archiveDbPath)));

                // Services
                builder.AddSingleton<IUserProfileService, UserProfileService>();

                // Repositories
                builder.AddTransient<IPayerRepository, PayerRepository>();
                builder.AddTransient<IStudentRepository, StudentRepository>();
                builder.AddTransient<IServiceRepository, ServiceRepository>();
                builder.AddTransient<ISpecificationRepository, SpecificationRepository>();
                builder.AddTransient<ILessonRepository, LessonRepository>();
                builder.AddTransient<IBillingRepository, BillingRepository>();
                builder.AddTransient<IGeneralRepository, GeneralRepository>();
                builder.AddTransient<IDashboardRepository, DashboardRepository>();

                // Utilities
                builder.AddSingleton<PDF.IWriter, PDF.Writer>();
                builder.AddSingleton<PDF.IReportWriter, PDF.ReportWriter>();
                builder.AddSingleton<Excel.IReader, Excel.Reader>();
                builder.AddSingleton<Excel.IWriter, Excel.Writer>();

                // ViewModels
                builder.AddTransient<PayersViewModel>();
                builder.AddTransient<StudentsViewModel>();
                builder.AddTransient<ServicesViewModel>();
                builder.AddTransient<SpecificationsViewModel>();
                builder.AddTransient<LessonsViewModel>();
                builder.AddTransient<BillingViewModel>();
                builder.AddTransient<SettingsViewModel>();
                builder.AddTransient<DashboardViewModel>();
                builder.AddTransient<ProposalViewModel>();

                builder.AddSingleton<MainWindow>();

                var provider = builder.BuildServiceProvider();
                Ioc.Default.ConfigureServices(provider);
                return provider;
            }
        }

        private void InitializeDatabase()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApoloContext>();
            context.Database.Migrate();

            var archive = scope.ServiceProvider.GetRequiredService<ApoloArchiveContext>();
            archive.Database.Migrate();

            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            archive.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            MainWindow = m_window;
            m_window.Activate();
        }

        private async void App_UnhandledExceptionAsync(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "A fatal UI exception occurred.");
            e.Handled = true; // Attempt to prevent the app from crashing instantly

            if (MainWindow?.Content?.XamlRoot != null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Unexpected Error",
                    Content = $"A critical error occurred. The application has logged the issue, but it may behave unexpectedly if you continue.\n\nDetails: {e.Exception.Message}",
                    CloseButtonText = "Understood",
                    XamlRoot = MainWindow.Content.XamlRoot
                };

                try
                {
                    await dialog.ShowAsync();
                }
                catch (Exception dialogEx)
                {
                    // If the UI thread is too corrupted to show a dialog, catch it so we don't cause a secondary crash
                    Log.Error(dialogEx, "Failed to display the unhandled exception dialog.");
                }
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "An unobserved background task exception occurred.");
            e.SetObserved(); // Prevent the app from tearing down
        }

        private void PerformAutomatedBackup()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder.Path;
                var backupFolder = Path.Combine(localFolder, "Backups");

                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string today = DateTime.Now.ToString("yyyyMMdd");
                string appDbPath = Path.Combine(localFolder, "apolo.db");
                string archiveDbPath = Path.Combine(localFolder, "apolo_archive.db");

                string backupAppDbPath = Path.Combine(backupFolder, $"apolo_{today}.db");
                string backupArchiveDbPath = Path.Combine(backupFolder, $"apolo_archive_{today}.db");

                // Only perform the backup if today's backup doesn't exist yet
                if (!File.Exists(backupAppDbPath) && File.Exists(appDbPath))
                {
                    Log.Information("Performing daily automated database backup...");

                    // File.Copy is safe here because we run this BEFORE Entity Framework starts
                    File.Copy(appDbPath, backupAppDbPath);

                    if (File.Exists(archiveDbPath))
                    {
                        File.Copy(archiveDbPath, backupArchiveDbPath);
                    }

                    // Prune old backups (keep the last 7 days to save disk space)
                    var oldFiles = Directory.GetFiles(backupFolder, "*.db")
                                            .Select(f => new FileInfo(f))
                                            .Where(f => f.CreationTime < DateTime.Now.AddDays(-7));

                    foreach (var file in oldFiles)
                    {
                        file.Delete();
                    }

                    Log.Information("Automated backup completed and old files pruned successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to perform automated database backup.");
            }
        }

        public Window? MainWindow { get; private set; }

        private Window? m_window;
    }
}
