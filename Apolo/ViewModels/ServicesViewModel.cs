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
    public partial class ServicesViewModel : ObservableObject
    {
        ServiceRepository _repository;

        public ObservableCollection<ServiceSummary> Services { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public ServicesViewModel(ServiceRepository serviceRepository)
        {
            _repository = serviceRepository;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var items = await _repository.GetServicesAsync();
                Services.Clear();
                foreach (var item in items) Services.Add(item);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task AddServiceAsync(string name, decimal price)
        {
            if (IsBusy) return;

            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorMessage = "Name is required.";
                return;
            }
            if (price < 0)
            {
                ErrorMessage = "Enter a valid non-negative price (e.g., 42.50).";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var entity = new Models.Service
                {
                    Name = name,
                    PricePerHour = price
                };
                await _repository.AddAsync(entity);

                Services.Add(new ServiceSummary(entity.Id, entity.Name, entity.PricePerHour));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task DeleteServiceAsync(ServiceSummary? item)
        {
            if (item is null || IsBusy) { return; }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.DeleteAsync(item.Id);

                var toRemove = Services.FirstOrDefault(s => s.Id == item.Id);
                if (toRemove != null) Services.Remove(toRemove);
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Delete failed due to related data. Check constraints.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdateServiceAsync (Guid id, string name, decimal price)
        {
            if (IsBusy) return;

            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Name is required.";
                return;
            }
            if (price <= 0)
            {
                ErrorMessage = "Enter a valid non-negative price (e.g., 42.50).";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.UpdateAsync(id, name, price);

                // Update item in UI list
                var idx = Services.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    Services[idx] = new ServiceSummary(id, name, price);
                }
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Save failed due to related data. Check constraints.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }
    }
}
