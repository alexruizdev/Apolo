using Apolo.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Office2016.Excel;
using Models;
using Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Apolo.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly UserProfileService _service;

        private ApoloContext? _context;

        ServiceRepository _serviceRepository;
        PayerRepository _payerRepository;
        StudentRepository _studentRepository;
        SpecificationRepository _specificationRepository;
        LessonRepository _lessonRepository;
        InvoiceRepository _invoiceRepository;

        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string address = string.Empty;
        [ObservableProperty] private string zipCode = string.Empty;
        [ObservableProperty] private string city = string.Empty;
        [ObservableProperty] private string phone = string.Empty;
        [ObservableProperty] private string taxId = string.Empty;
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string bankName = string.Empty;
        [ObservableProperty] private string bankAccount = string.Empty;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? statusMessage;

        public SettingsViewModel(
            UserProfileService service, 
            ServiceRepository serviceRepository, 
            PayerRepository payerRepository,
            StudentRepository studentRepository,
            SpecificationRepository specificationRepository,
            LessonRepository lessonRepository,
            InvoiceRepository invoiceRepository)
        {
            _service = service;
            _context = Ioc.Default.GetService<ApoloContext>();
            _serviceRepository = serviceRepository;
            _payerRepository = payerRepository;
            _studentRepository = studentRepository;
            _specificationRepository = specificationRepository;
            _lessonRepository = lessonRepository;
            _invoiceRepository = invoiceRepository;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = null;

            try
            {
                var p = await _service.LoadProfileAsync();
                FullName = p.FullName;
                Address = p.Address;
                ZipCode = p.ZipCode;
                City = p.City;
                Phone = p.Phone;
                TaxId = p.TaxId;
                Email = p.Email;
                BankName = p.BankName;
                BankAccount = p.BankAccount;
                StatusMessage = "Settings loaded.";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = null;

            try
            {
                var p = new UserProfile
                {
                    FullName = FullName?.Trim() ?? string.Empty,
                    Address = Address?.Trim() ?? string.Empty,
                    ZipCode = ZipCode?.Trim() ?? string.Empty,
                    City = City?.Trim() ?? string.Empty,
                    Phone = Phone?.Trim() ?? string.Empty,
                    TaxId = TaxId?.Trim() ?? string.Empty,
                    Email = Email?.Trim() ?? string.Empty,
                    BankAccount = BankAccount?.Trim() ?? string.Empty,
                    BankName = BankName?.Trim() ?? string.Empty,
                };
                await _service.SaveAsync(p);
                StatusMessage = "Saved.";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task ClearDatabaseAsync()
        {
            if (IsBusy) return;
            if (_context == null) return;
            IsBusy = true;
            StatusMessage = null;


            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            _context.SaveChanges();

            IsBusy = false;
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

        [RelayCommand]
        public async Task<string> ImportDatabaseFromExcel(string file)
        {
            if (IsBusy)
                throw new Exception("System is busy.");
            if (_context == null)
                throw new Exception("Can't access database.");
            if (string.IsNullOrEmpty(file))
                throw new ArgumentException("No file selected.");
            var root = Path.GetDirectoryName(file);
            if (!Directory.Exists(root))
                throw new ArgumentException($"Directory {root} does not exist.");

            var reader = new Excel.Reader();
            reader.ReadExcelAndStore(file);

            // Insert data into database
            // Services
            foreach (var service in reader.Services)
            {
                await _serviceRepository.AddAsync(service);
            }

            // Payers
            foreach (var payer in reader.Payers)
            {
                await _payerRepository.UpsertAsync(payer);
            }

            // Students
            foreach (var student in reader.Students)
            {
                await _studentRepository.UpsertAsync(student);
            }

            // Specifications
            foreach (var specification in reader.Specifications)
            {
                await _specificationRepository.AddSpecificationAsync(specification);
            }

            // Lessons
            foreach (var lesson in reader.Lessons)
            {
                await _lessonRepository.UpsertAsync(lesson);
            }

            // Invoices
            foreach (var invoice in reader.Invoices)
            {
                await _invoiceRepository.UpsertAsync(invoice);
            }

            // TODO : display entries added
            return await GenerateExportSummary(root, 
                reader.Services.Count,
                reader.Payers.Count,
                reader.Students.Count,
                reader.Specifications.Count,
                reader.Lessons.Count,
                reader.Invoices.Count);
        }

        [RelayCommand]
        public async Task ExportDatabaseToExcel(string folder)
        {
            if (IsBusy)
                throw new Exception("System is busy.");
            if (_context == null)
                throw new Exception("Can't access database.");
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("No folder selected.");
            if (!Directory.Exists(folder))
                throw new ArgumentException($"Folder {folder} does not exist.");

            string templatePath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath,
                "Assets", "Excel", "Template.xlsx");


            var writer = new Excel.Writer();
            writer.Services.AddRange(await _serviceRepository.GetServicesAsync());
            writer.Payers.AddRange(await _payerRepository.GetPayersAsync());
            writer.Students.AddRange(await _studentRepository.GetSudentsAsync());
            writer.Specifications.AddRange(await _specificationRepository.GetSpecificationsAsync());
            writer.Lessons.AddRange(await _lessonRepository.GetLessonsAsync(false, null));
            writer.Invoices.AddRange(await _invoiceRepository.GetInvoicesAsync());

            writer.WriteExcel(templatePath, folder);
        }
    }
}
