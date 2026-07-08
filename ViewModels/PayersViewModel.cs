using Apolo.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using SQLitePCL;
using System.Collections.ObjectModel;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class PayersViewModel(IPayerRepository payerRepository, IStringLocalizer stringLocalizer) : 
        BaseViewModel(stringLocalizer)
    {
        readonly IPayerRepository _payerRepository = payerRepository;

        public ObservableCollection<PayerSummary> Payers { get; } = [];

        // Messages
        private static string Message_Load_Payers_Error => "Messages/Load_Payers_Error";
        private static string Message_Load_Payers_Success => "Messages/Load_Payers_Success";
        private static string Message_Add_Payer_Error => "Messages/Add_Payer_Error";
        private static string Message_Add_Payer_Success => "Messages/Add_Payer_Success";
        private static string Message_Delete_Payer_Error => "Messages/Delete_Payer_Error";
        private static string Message_Associated_Student_Reason => "Messages/Associated_Student_Reason";
        private static string Message_Delete_Payer_Success => "Messages/Delete_Payer_Success";
        private static string Message_Edit_Payer_Error => "Messages/Edit_Payer_Error";
        private static string Message_Edit_Payer_Success => "Messages/Edit_Payer_Success";

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
                SetExitFunction(_loc.Get(Message_PersonNameValidation), InfoBarType.Warning);
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
                throw new InvalidDataException($"{_loc.Get(Message_Payer_Not_Loaded, id.ToString())}.");
            }
            return (payer, Payers.IndexOf(payer));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Payers_Error);
                return;
            }

            SetEnterFunction();

            var items = await _payerRepository.GetPayersAsync();
            Payers.Clear();
            foreach (var item in items) Payers.Add(item);
            SetExitFunction($"{_loc.Get(Message_Load_Payers_Success, Payers.Count)}.", InfoBarType.Success);
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
                SetExitBusy(Message_Add_Payer_Error);
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
                Payers.Add(Helper.ConvertToPayerSummary(payer, 0));
                SetExitFunction($"{_loc.Get(Message_Add_Payer_Success, payer.FullName)}.", InfoBarType.Success);
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
                SetExitBusy(Message_Delete_Payer_Error);
                return;
            }

            SetEnterFunction();
            var (oldPayer, _) = GetPayer(id);

            try
            {
                await _payerRepository.DeleteAsync(id);

                Payers.Remove(oldPayer);
                SetExitFunction($"{_loc.Get(Message_Delete_Payer_Success, oldPayer.Name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
            catch (InvalidOperationException ex)
            {
                SetExitFunction($"{_loc.Get(Message_Delete_Payer_Error)}: {_loc.Get(Message_Associated_Student_Reason, ex.Message)}",
                    InfoBarType.Error);
            }
        }

        public async Task UpdatePayerAsync (Guid payerId, string firstName, string lastName, 
            string address, string zipCode, string city, string taxId)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Edit_Payer_Error);
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
                SetExitFunction($"{_loc.Get(Message_Edit_Payer_Success, oldPayer.Name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
