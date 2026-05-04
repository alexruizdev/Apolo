using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;

namespace Apolo.ViewModels
{
    public partial class StudentsViewModel : ObservableObject
    {
        IStudentRepository _repository;
        IPayerRepository _payerRepository;

        public ObservableCollection<StudentSummary> Students { get; } = new();
        public ObservableCollection<PayerOption> Payers { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public StudentsViewModel(IStudentRepository studentRepository, IPayerRepository payerRepository)
        {
            _repository = studentRepository;
            _payerRepository = payerRepository;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                // Payer options
                var payerItems = await _payerRepository.GetPayerOptionsAsync();

                Payers.Clear();
                foreach (var p in payerItems) Payers.Add(p);

                // Students
                var studentItems = await _repository.GetSudentsAsync();
                Students.Clear();
                foreach (var s in studentItems) Students.Add(s);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task AddStudentAsync(string firstName, string lastName, Guid? payerId)
        {
            if (IsBusy) return;

            var first = (firstName ?? "").Trim();
            var last = (lastName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            {
                ErrorMessage = "Enter at least a first or last name.";
                return;
            }

            var entity = new Student
            {
                FirstName = first,
                LastName = last
            };

            // Check student name
            if (Students.Any(s => s.FullName == entity.FullName))
            {
                ErrorMessage = $"Student name already exists {entity.FullName}.";
                return;
            }

            if (!payerId.HasValue) {
                if (Payers.Any(p => p.FullName == entity.FullName))
                {
                    ErrorMessage = $"Payers is not selected and there is already a payer with that name: {entity.FullName}.";
                    return;
                }
                IsBusy = true;
                ErrorMessage = null;
                try
                {
                    var payer = new Payer
                    {
                        FirstName = first,
                        LastName = last
                    };

                    await _payerRepository.UpsertAsync(payer);
                    Payers.Add(new PayerOption(payer.Id, payer.FullName));
                    payerId = payer.Id;

                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    IsBusy = false;
                    return;
                }
            }

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                entity.PayerId = payerId.Value;
                await _repository.UpsertAsync(entity);

                var payerName = Payers.First(p => p.Id == payerId.Value).FullName;
                Students.Add(new StudentSummary(entity.Id, first, last, payerId.Value, payerName));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task DeleteStudentAsync(StudentSummary? item)
        {
            if (item is null || IsBusy) { return; }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.DeleteAsync(item.Id);

                var toRemove = Students.FirstOrDefault(s => s.Id == item.Id);
                if (toRemove != null) Students.Remove(toRemove);
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

        public async Task UpdateStudentAsync (Guid id, string firstName,  string lastName, Guid newPayerId)
        {
            if (IsBusy) return;
            var first = (firstName ?? "").Trim();
            var last = (lastName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrEmpty(last))
            {
                ErrorMessage = "Enter at least a first or last name.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.UpdateAsync(id, newPayerId, first, last);

                // Update item in UI list
                var idx = Students.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    var payerName = Payers.First(p => p.Id == newPayerId).FullName;
                    Students[idx] = new StudentSummary(id, first, last, newPayerId, payerName);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }
    }
}
