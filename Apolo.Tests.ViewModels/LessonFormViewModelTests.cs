using Models;
using Moq;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class LessonNewFormViewModelTests : LessonsViewModelBaseTests
    {
        private LessonFormViewModel _formViewModel = null!;

        [TestInitialize]
        public override void TestInit()
        {
            base.TestInit();

            var data = new DummyData();

            foreach (var s in data.ServiceSummaries) _viewModel.Services.Add(s);
            foreach (var s in data.StudentOptions) _viewModel.Students.Add(s);

            _formViewModel = new LessonFormViewModel(_viewModel);
        }

        [TestMethod]
        public void FillForm()
        {
            // Mode
            Assert.IsFalse(_formViewModel.IsEditMode);

            // Default student - empty
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
            Assert.IsTrue(_formViewModel.OpenInfoBar);
            Assert.IsNull(_formViewModel.SelectedStudent);
            Assert.IsNull(_formViewModel.SelectedSpecification);
            Assert.HasCount(11, _formViewModel.FilteredStudents);
            Assert.IsFalse(_formViewModel.IsSpecificationEnabled);
            Assert.IsFalse(_formViewModel.IsPrimaryButtonEnabled);

            // Default service - first in the list
            Assert.IsNotNull(_formViewModel.SelectedService);
            Assert.AreEqual("Math Tutoring", _formViewModel.SelectedService.Name);
            Assert.AreEqual("Math Tutoring", _formViewModel.Name);
            Assert.AreEqual(40, _formViewModel.GetBasePrice());
            Assert.AreEqual(60, _formViewModel.GetDuration());
            Assert.AreEqual(0, _formViewModel.Tip);
            Assert.IsFalse(_formViewModel.IsOnline);
            Assert.IsFalse(_formViewModel.IsWeekendOrHoliday);
            Assert.IsEmpty(_formViewModel.Notes);

            // Get specifications
            var specification = new SpecificationOption(Guid.NewGuid(), "Exam Preparation Package - Alice", 
                _formViewModel.Services[5].Id, 150, 90, true, true);
            _mockSpecificationRepo.Setup(r => r.GetSpecificationsForStudentAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync([specification]);

            // Search student
            _formViewModel.StudentSearchText = "doe";
            Assert.HasCount(2, _formViewModel.FilteredStudents);
            _formViewModel.SelectedStudent = _formViewModel.FilteredStudents[0];
            Assert.IsNull(_formViewModel.InfoMessage);
            Assert.AreEqual(InfoBarType.Success, _formViewModel.InfoBarType);
            Assert.IsFalse(_formViewModel.OpenInfoBar);
            Assert.HasCount(1, _formViewModel.Specifications);

            // Select specification
            _formViewModel.SelectedSpecification = _formViewModel.Specifications[0];
            Assert.IsNotNull(_formViewModel.SelectedService);
            Assert.AreEqual("Exam Preparation Package", _formViewModel.SelectedService.Name);
            Assert.AreEqual("Exam Preparation Package", _formViewModel.Name);
            Assert.AreEqual(150, _formViewModel.GetBasePrice());
            Assert.AreEqual(90, _formViewModel.GetDuration());
            Assert.AreEqual(0, _formViewModel.Tip);
            Assert.IsTrue(_formViewModel.IsOnline);
            Assert.IsTrue(_formViewModel.IsWeekendOrHoliday);
            Assert.IsEmpty(_formViewModel.Notes);

            Assert.IsTrue(_formViewModel.IsPrimaryButtonEnabled);

            // Error
            _formViewModel.SelectedService = null;
            Assert.IsNotNull(_formViewModel.InfoMessage); 
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
            Assert.IsTrue(_formViewModel.OpenInfoBar);

        }
    }

    [TestClass]
    public class LessonEditFormViewModelTests : LessonsViewModelBaseTests
    {
        private LessonFormViewModel _formViewModel = null!;

        [TestInitialize]
        public override void TestInit()
        {
            base.TestInit();

            var data = new DummyData();

            foreach (var s in data.ServiceSummaries) _viewModel.Services.Add(s);
            foreach (var s in data.StudentOptions) _viewModel.Students.Add(s);

            var lesson = new LessonSummary(Guid.NewGuid(), new DateOnly(2024, 6, 10), "Math Tutoring - Alice", 45, false, _viewModel.Students[0].Id,
                "Alice Doe", null, string.Empty, true, 90, 30, true, 5, false, 10, 2.5m, "Small note.");

            _formViewModel = new LessonFormViewModel(_viewModel, lesson);
        }

        [TestMethod]
        public void FillForm()
        {
            // Mode
            Assert.IsTrue(_formViewModel.IsEditMode);

            // Default student - empty
            Assert.IsNull(_formViewModel.InfoMessage);
            Assert.AreEqual(InfoBarType.Success, _formViewModel.InfoBarType);
            Assert.IsFalse(_formViewModel.OpenInfoBar);
            Assert.IsNull(_formViewModel.SelectedStudent);
            Assert.IsNull(_formViewModel.SelectedSpecification);
            Assert.HasCount(0, _formViewModel.FilteredStudents);
            Assert.HasCount(0, _formViewModel.Specifications);
            Assert.IsFalse(_formViewModel.IsSpecificationEnabled);
            Assert.IsTrue(_formViewModel.IsPrimaryButtonEnabled);

            // Default service - first in the list
            Assert.IsNull(_formViewModel.SelectedService);
            Assert.AreEqual("Math Tutoring - Alice", _formViewModel.Name);
            Assert.AreEqual(30, _formViewModel.GetBasePrice());
            Assert.AreEqual(90, _formViewModel.GetDuration());
            Assert.AreEqual(2.5, _formViewModel.Tip);
            Assert.IsTrue(_formViewModel.IsOnline);
            Assert.IsFalse(_formViewModel.IsWeekendOrHoliday);
            Assert.AreEqual("Small note.", _formViewModel.Notes);
            Assert.AreEqual(5, _formViewModel.TravelAllowance);
            Assert.AreEqual(10, _formViewModel.WeekendFee);
            Assert.AreEqual(new DateTime(2024, 6, 10), _formViewModel.Date);

            // Errors
            _formViewModel.Duration = 0;
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
            Assert.IsTrue(_formViewModel.OpenInfoBar);
            _formViewModel.Duration = 60;
            _formViewModel.Price = 0;
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
            _formViewModel.Price = 60;
            _formViewModel.Tip = -1;
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
            _formViewModel.Tip = 60;
            _formViewModel.Name = "";
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
        }
    }
}
