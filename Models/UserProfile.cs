using Models;

namespace Models
{
    public class LanguageOption
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
    public sealed class UserProfile
    {
        public string Language { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public double IvaPercent { get; set; } = 0;

        public double TravelAllowance { get; set; } = 0;
        public double WeekendFee { get; set; } = 0;
        public string BillingFolder { get; set; } = string.Empty;
        public string BackupFolder { get; set; } = string.Empty;
    }
}

namespace Apolo.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile> LoadProfileAsync();
        Task SaveAsync(UserProfile profile);
    }

    public interface ILanguageService
    {
        void ApplyLanguage(string languageCode);
    }

    public interface IStringLocalizer
    {
        string Get(string key);
        string Get(string key, params object[] args);
    }

}