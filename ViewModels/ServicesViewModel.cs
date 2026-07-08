using Apolo.Services;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class ServicesViewModel : BaseViewModel
    {
        IServiceRepository _repository;

        public ObservableCollection<ServiceSummary> Services { get; } = new();

        public ServicesViewModel(IServiceRepository serviceRepository, IStringLocalizer stringLocalizer)
            : base(stringLocalizer)
        {
            _repository = serviceRepository;
        }

        private static string Message_Load_Services_Error => "Message/Load_Services_Error";
        private static string Message_Add_Service_Error => "Message/Add_Service_Error";
        private static string Message_Repeated_Service_Reason => "Message/Service_Repeated_Reason";
        private static string Message_Add_Service_Success => "Message/Add_Service_Success";
        private static string Message_Delete_Service_Error => "Message/Delete_Service_Error";
        private static string Message_Delete_Service_Success => "Message/Delete_Service_Success";
        private static string Message_Edit_Service_Error => "Message/Edit_Service_Error";
        private static string Message_Edit_Service_Success => "Message/Edit_Service_Success";

        public bool ValidateServiceInput(ref string name, decimal price)
        {
            var errors = new List<string>();

            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                errors.Add(_loc.Get(Message_ServiceNameValidation));
            if (price < 0)
                errors.Add(_loc.Get(Message_PriceValidation));

            if (errors.Count == 0)
                return  true;

            SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Warning);
            return false;
        }

        public (ServiceSummary service, int index) GetService(Guid id)
        {
            var service = Services.FirstOrDefault(s => s.Id == id);
            if (service is null)
            {
                SetExitFunction();
                throw new InvalidDataException(_loc.Get(Message_Service_Not_Loaded, id.ToString()));
            }
            return (service, Services.IndexOf(service));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Services_Error);
                return;
            }

            SetEnterFunction();

            var items = await _repository.GetServicesAsync();
            Services.Clear();
            foreach (var item in items) Services.Add(item);
            SetExitFunction();
        }

        public async Task AddServiceAsync(string name, bool isPricePerHour, decimal price)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Add_Service_Error);
                return;
            }

            SetEnterFunction();

            if (!ValidateServiceInput(ref name, price))
                return;

            try
            {
                var entity = new Service
                {
                    Name = name,
                    IsPricePerHour = isPricePerHour,
                    Price = price
                };
                await _repository.AddAsync(entity);
                
                Services.Add(Helper.ConvertToServiceSummary(entity));
                SetExitFunction($"{_loc.Get(Message_Add_Service_Success, name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction($"{_loc.Get(Message_Add_Service_Error)}: {_loc.Get(Message_Repeated_Service_Reason, name, ex.Message)}.",
                    InfoBarType.Error);
            }
        }

        [RelayCommand]
        public async Task DeleteServiceAsync(Guid id)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Delete_Service_Error);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetService(id);

            try
            {
                await _repository.DeleteAsync(id);

                Services.Remove(oldItem);
                SetExitFunction($"{_loc.Get(Message_Delete_Service_Success, oldItem.Name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdateServiceAsync (Guid id, string name, bool isPricePerHour, decimal price)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Edit_Service_Error);
                return;
            }

            SetEnterFunction();

            if (!ValidateServiceInput(ref name, price))
                return;

            var (oldItem, idx) = GetService(id);

            // Only check uniqueness if the name is DIFFERENT from the current one
            if (oldItem.Name.ToLower().Trim() != name.ToLower().Trim())
            {
                if (Services.Any(s => s.Id != id && s.Name.ToLower().Trim() == name.ToLower().Trim()))
                {
                    SetExitFunction($"{_loc.Get(Message_Edit_Service_Error)}: {_loc.Get(Message_Repeated_Service_Reason, name, string.Empty)}.",
                        InfoBarType.Error);
                    return;
                }
            }

            try
            {
                await _repository.UpdateAsync(id, name, isPricePerHour, price);

                // Update item in UI list
               
                var index = Services.IndexOf(oldItem);
                Services[index] = oldItem with
                {
                    Name = name,
                    IsPricePerHour = isPricePerHour,
                    Price = (double)price
                }; 
                SetExitFunction($"{_loc.Get(Message_Edit_Service_Success, name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
