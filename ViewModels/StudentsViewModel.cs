using Apolo.Services;
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

        // Messages
        private static string Message_Load_Students_Error => "Messages/Load_Students_Error";
        private static string Message_Load_Students_Success => "Messages/Load_Students_Success";
        private static string Message_Add_Student_Error => "Messages/Add_Student_Error";
        private static string Message_Add_Student_Success => "Messages/Add_Student_Success";
        private static string Message_Add_Student_Extra => "Messages/Add_Student_Extra";
        private static string Message_Delete_Student_Error => "Messages/Delete_Student_Error";
        private static string Message_Delete_Student_Success => "Messages/Delete_Student_Success";
        private static string Message_Edit_Student_Error => "Messages/Edit_Student_Error";
        private static string Message_Edit_Student_Success => "Messages/Edit_Student_Success";

        public StudentsViewModel(IStudentRepository studentRepository, IPayerRepository payerRepository, IStringLocalizer stringLocalizer)
            : base(stringLocalizer)
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
                SetExitBusy(Message_Load_Students_Error);
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

            SetExitFunction($"{_loc.Get(Message_Load_Students_Success, Students.Count)}.", InfoBarType.Success);
        }

        public async Task AddStudentAsync(string firstName, string lastName, Guid? payerId)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Add_Student_Error);
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
                    Payers.Add(Helper.ConvertToPayerOption(payer));
                    payerId = payer.Id;
                    additionalMessage = $" {_loc.Get(Message_Add_Student_Extra)}.";
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
                SetExitFunction($"{_loc.Get(Message_Add_Student_Success, entity.FullName)}.{additionalMessage}", InfoBarType.Success);
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
                SetExitBusy(Message_Delete_Student_Error);
                return;
            }

            SetEnterFunction();

            var (oldStudent, index) = GetStudent(id);

            try
            {
                await _studentRepository.DeleteAsync(id);

                Students.Remove(oldStudent);
                SetExitFunction($"{_loc.Get(Message_Delete_Student_Success, oldStudent.Name)}.", InfoBarType.Success);
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
                SetExitBusy(Message_Edit_Student_Error);
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
                SetExitFunction($"{_loc.Get(Message_Edit_Student_Success, oldStudent.Name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
