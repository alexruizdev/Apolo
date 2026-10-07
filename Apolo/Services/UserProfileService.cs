using Microsoft.Windows.Storage;
using Models;
using System.Threading.Tasks;

namespace Apolo.Services
{
    public sealed class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDataContainer _ls = ApplicationData.GetDefault().LocalSettings;
        public Task<UserProfile> LoadProfileAsync()
        {
            // Load from local settings or file
            var v = _ls.Values;
            var p = new UserProfile
            {
                FullName = v[nameof(UserProfile.FullName)] as string ?? "",
                Address = v[nameof(UserProfile.Address)]  as string ?? "",
                Email = v[nameof(UserProfile.Email)] as string ?? "",
                ZipCode = v[nameof(UserProfile.ZipCode)] as string ?? "",
                City = v[nameof(UserProfile.City)] as string ?? "",
                Phone = v[nameof(UserProfile.Phone)] as string ?? "",
                TaxId = v[nameof(UserProfile.TaxId)] as string ?? "",
                BankName = v[nameof(UserProfile.BankName)] as string ?? "",
                BankAccount = v[nameof(UserProfile.BankAccount)] as string ?? "",
                IvaPercent = v[nameof(UserProfile.IvaPercent)] as double? ?? 0,
                TravelAllowance = v[nameof(UserProfile.TravelAllowance)] as double? ?? 0,
                WeekendFee = v[nameof(UserProfile.WeekendFee)] as double? ?? 0,
                BillingFolder = v[nameof(UserProfile.BillingFolder)] as string ?? "",
                BackupFolder = v[nameof(UserProfile.BackupFolder)] as string ?? "",
                Language = v[nameof(UserProfile.Language)] as string ?? "",
                GenerateTicketWithInvoice = v[nameof(UserProfile.GenerateTicketWithInvoice)] as bool? ?? false,
                DeveloperMode = v[nameof(UserProfile.DeveloperMode)] as bool? ?? false,
                ShowIvaDisclaimer = v[nameof(UserProfile.ShowIvaDisclaimer)] as bool? ?? false,
                LessonTerm = v[nameof(UserProfile.LessonTerm)] as string ?? "",
                StudentTerm = v[nameof(UserProfile.StudentTerm)] as string ?? "",
                ShowInvoiceDateColumn = v[nameof(UserProfile.ShowInvoiceDateColumn)] as bool? ?? true,
                ShowInvoiceConceptColumn = v[nameof(UserProfile.ShowInvoiceConceptColumn)] as bool? ?? true,
                ShowInvoiceStudentColumn = v[nameof(UserProfile.ShowInvoiceStudentColumn)] as bool? ?? true,
                ShowInvoiceDurationColumn = v[nameof(UserProfile.ShowInvoiceDurationColumn)] as bool? ?? true,
                ShowInvoicePriceColumn = v[nameof(UserProfile.ShowInvoicePriceColumn)] as bool? ?? true,
                ShowTicketDateColumn = v[nameof(UserProfile.ShowTicketDateColumn)] as bool? ?? true,
                ShowTicketConceptColumn = v[nameof(UserProfile.ShowTicketConceptColumn)] as bool? ?? true,
                ShowTicketStudentColumn = v[nameof(UserProfile.ShowTicketStudentColumn)] as bool? ?? true,
                ShowTicketDurationColumn = v[nameof(UserProfile.ShowTicketDurationColumn)] as bool? ?? true,
                ShowTicketPriceColumn = v[nameof(UserProfile.ShowTicketPriceColumn)] as bool? ?? true
            };

            return Task.FromResult(p);
        }

        public Task SaveAsync(UserProfile profile)
        {
            var v = _ls.Values;
            var localSettings = ApplicationData.GetDefault().LocalSettings;
            v[nameof(UserProfile.FullName)]  = profile.FullName;
            v[nameof(UserProfile.Address)] = profile.Address;
            v[nameof(UserProfile.Email)] = profile.Email;
            v[nameof(UserProfile.ZipCode)] = profile.ZipCode;
            v[nameof(UserProfile.City)] = profile.City;
            v[nameof(UserProfile.Phone)] = profile.Phone;
            v[nameof(UserProfile.TaxId)] = profile.TaxId;
            v[nameof(UserProfile.BankName)] = profile.BankName;
            v[nameof(UserProfile.BankAccount)] = profile.BankAccount;
            v[nameof(UserProfile.IvaPercent)] = profile.IvaPercent;
            v[nameof(UserProfile.TravelAllowance)] = profile.TravelAllowance;
            v[nameof(UserProfile.WeekendFee)] = profile.WeekendFee;
            v[nameof(UserProfile.BillingFolder)] = profile.BillingFolder;
            v[nameof(UserProfile.BackupFolder)] = profile.BackupFolder;
            v[nameof(UserProfile.Language)] = profile.Language;
            v[nameof(UserProfile.GenerateTicketWithInvoice)] = profile.GenerateTicketWithInvoice;
            v[nameof(UserProfile.DeveloperMode)] = profile.DeveloperMode;
            v[nameof(UserProfile.ShowIvaDisclaimer)] = profile.ShowIvaDisclaimer;
            v[nameof(UserProfile.LessonTerm)] = profile.LessonTerm;
            v[nameof(UserProfile.StudentTerm)] = profile.StudentTerm;
            v[nameof(UserProfile.ShowInvoiceDateColumn)] = profile.ShowInvoiceDateColumn;
            v[nameof(UserProfile.ShowInvoiceConceptColumn)] = profile.ShowInvoiceConceptColumn;
            v[nameof(UserProfile.ShowInvoiceStudentColumn)] = profile.ShowInvoiceStudentColumn;
            v[nameof(UserProfile.ShowInvoiceDurationColumn)] = profile.ShowInvoiceDurationColumn;
            v[nameof(UserProfile.ShowInvoicePriceColumn)] = profile.ShowInvoicePriceColumn;
            v[nameof(UserProfile.ShowTicketDateColumn)] = profile.ShowTicketDateColumn;
            v[nameof(UserProfile.ShowTicketConceptColumn)] = profile.ShowTicketConceptColumn;
            v[nameof(UserProfile.ShowTicketStudentColumn)] = profile.ShowTicketStudentColumn;
            v[nameof(UserProfile.ShowTicketDurationColumn)] = profile.ShowTicketDurationColumn;
            v[nameof(UserProfile.ShowTicketPriceColumn)] = profile.ShowTicketPriceColumn;

            return Task.CompletedTask;
        }

    }
}
