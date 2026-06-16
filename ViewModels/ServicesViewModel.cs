using CommunityToolkit.Mvvm.Input;
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

        public ServicesViewModel(IServiceRepository serviceRepository)
        {
            _repository = serviceRepository;
        }

        public bool ValidateServiceInput(ref string name, decimal price)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                SetExitFunction("Name is required.", InfoBarType.Warning);
                return false;
            }
            if (price < 0)
            {
                SetExitFunction("Enter a valid non-negative price (e.g., 42.50).", InfoBarType.Warning);
                return false;
            }
            return true;
        }

        public (ServiceSummary service, int index) GetService(Guid id)
        {
            var service = Services.FirstOrDefault(s => s.Id == id);
            if (service is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Service not loaded.");
            }
            return (service, Services.IndexOf(service));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load services while busy.", InfoBarType.Warning, false);
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
                SetExitFunction("Can't add service while busy.", InfoBarType.Warning, false);
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
                SetExitFunction($"Service '{name}' added successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        [RelayCommand]
        public async Task DeleteServiceAsync(Guid id)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't delete service while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetService(id);

            try
            {
                await _repository.DeleteAsync(id);

                Services.Remove(oldItem);
                SetExitFunction($"Service '{oldItem.Name}' deleted successfully.", InfoBarType.Success);
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
                SetExitFunction("Can't update service while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateServiceInput(ref name, price))
                return;

            var (oldItem, idx) = GetService(id);

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
                SetExitFunction($"Service '{name}' updated successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
