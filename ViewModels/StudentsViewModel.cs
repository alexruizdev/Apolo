using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class StudentsViewModel : BaseViewModel
    {
        IStudentRepository _studentRepository;
        IPayerRepository _payerRepository;

        public ObservableCollection<StudentSummary> Students { get; } = new();
        public ObservableCollection<PayerOption> Payers { get; } = new();

        public StudentsViewModel(IStudentRepository studentRepository, IPayerRepository payerRepository)
        {
            _studentRepository = studentRepository;
            _payerRepository = payerRepository;
        }

        public bool ValidateStudentInput(ref string firstName, ref string lastName)
        {
            firstName = (firstName ?? "").Trim();
            lastName = (lastName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                SetExitFunction("Enter at least a first or last name.", InfoBarType.Warning);
                return false;
            }

            return true;
        }

        public (StudentSummary value, int index) GetStudent(Guid id)
        {
            var student = Students.FirstOrDefault(s => s.Id == id);
            if (student is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Student not loaded.");
            }
            return (student, Students.IndexOf(student));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load students while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            // Payer options
            var payerItems = await _payerRepository.GetPayerOptionsAsync();

            Payers.Clear();
            foreach (var p in payerItems) Payers.Add(p);

            // Students
            var studentItems = await _studentRepository.GetSudentsAsync();
            Students.Clear();
            foreach (var s in studentItems) Students.Add(s);

            SetExitFunction($"{Students.Count} loaded", InfoBarType.Success);
        }

        public async Task AddStudentAsync(string firstName, string lastName, Guid? payerId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add student while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateStudentInput(ref firstName, ref lastName))
                return;

            try
            {
                string additionalMessage = string.Empty;
                // Create a payer if not selected
                if (!payerId.HasValue)
                {
                    var payer = new Payer
                    {
                        FirstName = firstName,
                        LastName = lastName
                    };

                    await _payerRepository.AddAsync(payer);
                    Payers.Add(new PayerOption(payer.Id, payer.FullName));
                    payerId = payer.Id;
                    additionalMessage = " Created payer with same name.";
                }

                // Create student
                var entity = new Student
                {
                    FirstName = firstName,
                    LastName = lastName
                };

                entity.PayerId = payerId.Value;
                await _studentRepository.AddAsync(entity);

                var payerName = Payers.First(p => p.Id == payerId.Value).FullName;
                Students.Add(new StudentSummary(entity.Id, firstName, lastName, payerId.Value, payerName));
                SetExitFunction($"Student '{entity.FullName}' added successfully.{additionalMessage}", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        [RelayCommand]
        public async Task DeleteStudentAsync(Guid id)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't delete student while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldStudent, index) = GetStudent(id);

            try
            {
                await _studentRepository.DeleteAsync(id);

                Students.Remove(oldStudent);
                SetExitFunction($"Student '{oldStudent.FullName}' deleted successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdateStudentAsync (Guid id, string firstName,  string lastName, Guid newPayerId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update student while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateStudentInput(ref firstName, ref lastName))
                return;

            var (oldStudent, index) = GetStudent(id);

            try
            {
                await _studentRepository.UpdateAsync(id, newPayerId, firstName, lastName);

                var payerName = Payers.First(p => p.Id == newPayerId).FullName;
                Students[index] = oldStudent with
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PayerId = newPayerId,
                    PayerName = payerName
                }; 
                SetExitFunction($"Student '{oldStudent.FullName}' updated successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
