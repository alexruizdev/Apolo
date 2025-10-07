using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage;
using Models;
using Repository;
using System;
using System.IO;
using System.Linq;

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
            // Choose a local path for the SQLite file
            //var appData = ApplicationData.GetDefault().LocalFolder.Path;
            //var dbDir = Path.Combine(appData, "Apolo");
            //Directory.CreateDirectory(dbDir);
            //var dbPath = Path.Combine(dbDir, "app.db");

            var builder = new ServiceCollection();
            builder.AddDbContext<ApoloContext>();
            builder.AddSingleton<PayerRepository>();
            builder.AddSingleton<PayersViewModel>();
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

                // Seed initial data if the database is empty
                if (!dbContext.Payers.Any())
                {
                    var payerOne = new Payer
                    {
                        FirstName = "Payer",
                        LastName = "1"
                    };
                    var payerTwo = new Payer
                    {
                        FirstName = "Payer",
                        LastName = "2"
                    };
                    dbContext.Payers.AddRange(payerOne, payerTwo);
                    dbContext.SaveChanges();
                }
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            m_window.Activate();
        }

        private Window? m_window;
    }
}
