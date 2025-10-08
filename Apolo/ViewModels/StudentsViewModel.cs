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
    public partial class StudentsViewModel : ObservableObject
    {
        StudentRepository _repository;

        public ObservableCollection<StudentSummary> Students { get; } = new();
        public ObservableCollection<PayerOption> Payers { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        [ObservableProperty] private string newFirstName = string.Empty;
        [ObservableProperty] private string newLastName = string.Empty;
        [ObservableProperty] private int newCommute;
        [ObservableProperty] private Guid? newSelectedPayerId;

        public StudentsViewModel(StudentRepository studentRepository)
        {
            _repository = studentRepository;
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
                var payerItems = await _repository.GetPayerOptionsAsync();

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

        [RelayCommand]
        public async Task AddStudentAsync()
        {
            if (IsBusy) return;

            var first = (NewFirstName ?? "").Trim();
            var last = (NewLastName ?? "").Trim();
            var payerId = NewSelectedPayerId;

            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            {
                ErrorMessage = "Enter at least a first or last name.";
                return;
            }
            if (!payerId.HasValue) {
                ErrorMessage = "Student has to have a payer selected.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var entity = new Student
                {
                    FirstName = first,
                    LastName = last,
                    PayerId = payerId.Value,
                    CommuteMinutes = NewCommute
                };
                await _repository.UpsertAsync(entity);

                var payerName = Payers.First(p => p.Id == payerId.Value).FullName;
                Students.Add(new StudentSummary(entity.Id, first, last, payerId.Value, payerName, NewCommute));

                // Reset form
                NewFirstName = string.Empty;
                NewLastName = string.Empty;
                NewCommute = 0;
                NewSelectedPayerId = null;
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

        public async Task UpdateStudentAsync (Guid id, string firstName,  string lastName, int commute, Guid newPayerId)
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
                await _repository.UpdateAsync(id, newPayerId, first, last, commute);

                // Update item in UI list
                var idx = Students.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    var payerName = Payers.First(p => p.Id == newPayerId).FullName;
                    Students[idx] = new StudentSummary(id, first, last, newPayerId, payerName, commute);
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
