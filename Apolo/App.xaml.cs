using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Models;
using Repository;
using System;
using System.IO;
using System.Linq;
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
            var dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "app.db");
            builder.AddDbContext<ApoloContext>(options => options.UseSqlite($"DataSource={dbPath}"));

            // Services
            builder.AddSingleton<IUserProfileService, UserProfileService>();

            // Repositories
            builder.AddSingleton<IPayerRepository, PayerRepository>();
            builder.AddSingleton<IStudentRepository, StudentRepository>();
            builder.AddSingleton<IServiceRepository, ServiceRepository>();
            builder.AddSingleton<ISpecificationRepository, SpecificationRepository>();
            builder.AddSingleton<ILessonRepository, LessonRepository>();
            builder.AddSingleton<IInvoiceRepository, InvoiceRepository>();
            builder.AddSingleton<IGeneralRepository, GeneralRepository>();

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
            builder.AddTransient<InvoicesViewModel>();
            builder.AddTransient<SettingsViewModel>();

            builder.AddSingleton<MainWindow>();

            Ioc.Default.ConfigureServices(builder.BuildServiceProvider());
            return Ioc.Default;
        }

        private void InitializeDatabase()
        {
            // Auto-create/update the DB schema
            using (var scope = Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApoloContext>();
                dbContext.Database.EnsureCreated();
                dbContext.Database.Migrate();
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
