using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;

namespace Apolo.ViewModels
{
    public partial class PayersViewModel : ObservableObject
    {
        IPayerRepository _payerRepository;

        public ObservableCollection<PayerSummary> Payers { get; } = new();

        [ObservableProperty]
        private bool isBusy;
        [ObservableProperty]
        private string? errorMessage;

        public PayersViewModel(IPayerRepository payerRepository)
        {
            _payerRepository = payerRepository;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var list = await _payerRepository.GetPayersAsync();

                Payers.Clear();
                foreach (var item in list)
                {
                    Payers.Add(item);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }


        public async Task AddPayerAsync(
            string firstName,
            string lastName,
            string addressCode,
            string zipCode,
            string city,
            string taxId)
        {
            if (IsBusy) return;
            firstName = (firstName ?? "").Trim();
            lastName = (lastName ?? "").Trim();
            addressCode = (addressCode ?? "").Trim();
            zipCode = (zipCode ?? "").Trim();
            city = (city ?? "").Trim();
            taxId = (taxId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                ErrorMessage = "Enter at least a first or last name.";
                return;
            }

            var payer = new Payer
            {
                FirstName = firstName,
                LastName = lastName,
                Address = addressCode,
                ZipCode = zipCode,
                City = city,
                TaxId = taxId,
            };

            // Check student name
            if (Payers.Any(p => p.FullName == payer.FullName))
            {
                ErrorMessage = $"Payer name already exists {payer.FullName}.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _payerRepository.UpsertAsync(payer);

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
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task DeletePayerAsync(PayerSummary? payer)
        {
            if (payer is null)
                return;
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _payerRepository.DeleteAsync(payer.Id);

                var toRemove = Payers.FirstOrDefault(x => x.Id == payer.Id);
                if (toRemove != null) 
                    Payers.Remove(toRemove);
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Delete failed due to database constraints.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdatePayerAsync (Guid payerId, string firstName, string lastName, string address, string zipCode, string city, string taxId)
        {
            if (IsBusy) return;
            var first = (firstName ?? "").Trim();
            var last = (lastName ?? "").Trim();
            address = (address ?? "").Trim();
            zipCode = (zipCode ?? "").Trim();
            city = (city ?? "").Trim();
            taxId = (taxId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            {
                ErrorMessage = "Enter at least a first or last name.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _payerRepository.UpdateAsync(payerId, first, last, address, zipCode, city, taxId);

                // Update the UI item by replacing it in the collection
                var index = Payers.Select((item, i) => (item, i))
                    .FirstOrDefault(t => t.item.Id == payerId).i;
                if (index >= 0)
                {
                    var current = Payers[index];
                    var updated = new PayerSummary(current.Id, first, last, current.Outstanding, address, zipCode, city, taxId);
                    Payers[index] = updated;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message; ;
            }
            finally { IsBusy = false; }
        }

    }
}
