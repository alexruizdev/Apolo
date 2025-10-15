using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Apolo.ViewModels
{
    public partial class PayersViewModel : ObservableObject
    {
        PayerRepository _payerRepository;

        public ObservableCollection<PayerSummary> Payers { get; } = new();

        [ObservableProperty]
        private string newFirstName = "";
        [ObservableProperty]
        private string newLastName = "";
        [ObservableProperty]
        private string newAddress = "";
        [ObservableProperty]
        private string newZipCode = "";
        [ObservableProperty]
        private string newCity = "";
        [ObservableProperty]
        private string newTaxId = "";
        [ObservableProperty]
        private bool isBusy;
        [ObservableProperty]
        private string? errorMessage;

        public PayersViewModel(PayerRepository payerRepository)
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


        [RelayCommand]
        public async Task AddPayerAsync()
        {
            if (IsBusy) return;
            var first = (NewFirstName ?? "").Trim();
            var last = (NewLastName ?? "").Trim();
            var address = (NewAddress ?? "").Trim();
            var zipCode = (NewZipCode ?? "").Trim();
            var city = (NewCity ?? "").Trim();
            var taxId = (NewTaxId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            {
                ErrorMessage = "Enter at least a first or last name.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var payer = new Payer
                {
                    FirstName = first,
                    LastName = last,
                    Address = address,
                    ZipCode = zipCode,
                    City = city,
                    TaxId = taxId,
                };

                await _payerRepository.UpsertAsync(payer);

                // Append to UI (no unpaid items yet)
                Payers.Add(new PayerSummary(payer.Id,payer.FirstName, payer.LastName, 0m, address, zipCode, city, taxId));

                NewFirstName = "";
                NewLastName = "";
                NewAddress = string.Empty;
                NewZipCode = string.Empty;
                NewCity = string.Empty;
                NewTaxId = string.Empty;
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
