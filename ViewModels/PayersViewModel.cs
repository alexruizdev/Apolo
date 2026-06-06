using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class PayersViewModel : BaseViewModel
    {
        IPayerRepository _payerRepository;

        public ObservableCollection<PayerSummary> Payers { get; } = new();

        public PayersViewModel(IPayerRepository payerRepository)
        {
            _payerRepository = payerRepository;
        }

        public bool ValidatePayerInput(ref string firstName, ref string lastName, 
            ref string address, ref string zipCode, ref string city, ref string taxId)
        {
            firstName = (firstName ?? "").Trim();
            lastName = (lastName ?? "").Trim();
            address = (address ?? "").Trim();
            zipCode = (zipCode ?? "").Trim();
            city = (city ?? "").Trim();
            taxId = (taxId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                SetExitFunction("Enter at least a first or last name.", InfoBarType.Warning);
                return false;
            }

            return true;
        }

        public (PayerSummary payer, int index) GetPayer(Guid id)
        {
            var payer = Payers.FirstOrDefault(s => s.Id == id);
            if (payer is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Payer not loaded.");
            }
            return (payer, Payers.IndexOf(payer));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load payers while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var items = await _payerRepository.GetPayersAsync();
            Payers.Clear();
            foreach (var item in items) Payers.Add(item);
            SetExitFunction($"{Payers.Count} loaded", InfoBarType.Success);
        }


        public async Task AddPayerAsync(
            string firstName,
            string lastName,
            string address,
            string zipCode,
            string city,
            string taxId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add payer while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidatePayerInput(ref firstName, ref lastName, ref address, ref zipCode, ref city, ref taxId))
            {
                return;
            }

            var payer = new Payer
            {
                FirstName = firstName,
                LastName = lastName,
                Address = address,
                ZipCode = zipCode,
                City = city,
                TaxId = taxId,
            };

            try
            {
                await _payerRepository.AddAsync(payer);

                // Append to UI (no unpaid items yet)
                Payers.Add(new PayerSummary(
                    payer.Id, 
                    payer.FirstName, 
                    payer.LastName, 
                    0m, 
                    payer.Address, 
                    payer.ZipCode, 
                    payer.City, 
                    payer.TaxId));
                SetExitFunction($"Payer '{payer.FullName}' added successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        [RelayCommand]
        public async Task DeletePayerAsync(Guid id)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't delete payer while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldPayer, index) = GetPayer(id);

            try
            {
                await _payerRepository.DeleteAsync(id);

                Payers.Remove(oldPayer);
                SetExitFunction($"Payer '{oldPayer.FullName}' deleted successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdatePayerAsync (Guid payerId, string firstName, string lastName, 
            string address, string zipCode, string city, string taxId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update payer while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidatePayerInput(ref firstName, ref lastName, ref address, ref zipCode, ref city, ref taxId))
            {
                return;
            }

            var (oldPayer, index) = GetPayer(payerId);

            try
            {
                await _payerRepository.UpdateAsync(payerId, firstName, lastName, address, zipCode, city, taxId);


                Payers[index] = oldPayer with
                {
                    FirstName = firstName, 
                    LastName = lastName,
                    Address = address,
                    Zip = zipCode,
                    City = city,
                    TaxId = taxId
                }; 
                SetExitFunction($"Payer '{oldPayer.FullName}' updated successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
