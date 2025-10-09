using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Models;
using Repository;
using System;
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
            var builder = new ServiceCollection();
            builder.AddDbContext<ApoloContext>();
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
