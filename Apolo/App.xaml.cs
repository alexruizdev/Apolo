using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Repository;
using System;
using System.IO;
using ViewModels;
using Windows.Storage;

namespace Apolo
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
            InitializeDatabase();
            InitializeComponent();
        }

        private static IServiceProvider ConfigureServices()
        {
            var builder = new ServiceCollection();

            // Use a helper to build the connection string with proper flags
            string GetConnectionString(string path)
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

            var dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "app.context");
            var archiveDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "archive.context");

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

            builder.AddSingleton<MainWindow>();

            var provider = builder.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);
            return provider;
        }

        private void InitializeDatabase()
        {
            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApoloContext>();
                context.Database.EnsureCreated();

                var archive = scope.ServiceProvider.GetRequiredService<ApoloArchiveContext>();
                archive.Database.EnsureCreated();

                context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                archive.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            MainWindow = m_window;
            m_window.Activate();
        }

        public Window? MainWindow { get; private set; }

        private Window? m_window;
    }
}
