using Apolo.Service;
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
            builder.AddSingleton<PayerRepository>();
            builder.AddSingleton<StudentRepository>();
            builder.AddSingleton<ServiceRepository>();
            builder.AddSingleton<SpecificationRepository>();
            builder.AddSingleton<LessonRepository>();
            builder.AddSingleton<InvoiceRepository>();
            builder.AddSingleton<PayersViewModel>();
            builder.AddSingleton<StudentsViewModel>();
            builder.AddSingleton<ServicesViewModel>();
            builder.AddSingleton<SpecificationsViewModel>();
            builder.AddSingleton<LessonsViewModel>();
            builder.AddSingleton<InvoicesViewModel>();
            builder.AddSingleton<SettingsViewModel>();
            builder.AddSingleton<MainWindow>();
            builder.AddSingleton<UserProfileService>();

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
