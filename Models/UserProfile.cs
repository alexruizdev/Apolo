using Models;

namespace Models
{
    public sealed class UserProfile
    {
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

}