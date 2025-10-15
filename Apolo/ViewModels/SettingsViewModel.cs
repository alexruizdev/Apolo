using Apolo.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using System;
using System.Threading.Tasks;

namespace Apolo.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly UserProfileService _service;

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

        public SettingsViewModel(UserProfileService service)
        {
            _service = service;
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
    }
}
