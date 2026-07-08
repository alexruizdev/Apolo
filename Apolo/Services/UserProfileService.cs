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
                Language = v[nameof(UserProfile.Language)] as string ?? ""
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

            return Task.CompletedTask;
        }

    }
}
