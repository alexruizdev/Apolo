using Models;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class SpecificationNewFormViewModelTests : SpecificationsViewModelBaseTests
    {
        private SpecificationFormViewModel _formViewModel = null!;
        [TestInitialize]
        public override void TestInit()
        {
            base.TestInit();

            var data = new DummyData();

            foreach (var s in data.ServiceSummaries) _viewModel.Services.Add(s);
            foreach (var s in data.StudentOptions) _viewModel.Students.Add(s);

            _formViewModel = new SpecificationFormViewModel(_viewModel);

        }
        
        [TestMethod]
        public void FillForm()
        {
            // Mode
            Assert.IsFalse(_formViewModel.IsEditMode);

            // Default values
            Assert.IsNotNull(_formViewModel.InfoMessage);
            Assert.AreEqual(InfoBarType.Error, _formViewModel.InfoBarType);
            Assert.IsTrue(_formViewModel.OpenInfoBar);

            Assert.IsTrue(string.IsNullOrWhiteSpace(_formViewModel.Name));
            Assert.IsNull(_formViewModel.SelectedService);
            Assert.IsNull(_formViewModel.SelectedStudent);
            Assert.IsTrue(string.IsNullOrWhiteSpace(_formViewModel.StudentSearchText));
            Assert.HasCount(11, _formViewModel.FilteredStudents);
            Assert.HasCount(11, _formViewModel.Students);
            Assert.HasCount(6, _formViewModel.Services);

            Assert.IsTrue(double.IsNaN(_formViewModel.Price));
            Assert.IsFalse(_formViewModel.IsPricePerHour);
            Assert.IsFalse(_formViewModel.IsOnline);
            Assert.IsFalse(_formViewModel.IsWeekendOrHoliday);

            // Fill form
            _formViewModel.Name = "Weekend";
            Assert.DoesNotContain("• Specification name cannot be empty.", _formViewModel.InfoMessage);

            _formViewModel.SelectedService = _formViewModel.Services[0];
            Assert.AreEqual("Math Tutoring", _formViewModel.Name);
            Assert.AreEqual(60, _formViewModel.Duration);

            _formViewModel.SelectedStudent = _formViewModel.Students[0];
            Assert.IsNull(_formViewModel.InfoMessage);
            Assert.IsFalse(_formViewModel.OpenInfoBar);

            _formViewModel.Price = 35.5;

            _formViewModel.Duration = 90;

            _formViewModel.IsOnline = true;

            _formViewModel.IsWeekendOrHoliday = true;
        }

    }

    [TestClass]
    public class SpecificationEditFormViewModelTests : SpecificationsViewModelBaseTests
    {
        private SpecificationFormViewModel _formViewModel = null!;
        [TestInitialize]
        public override void TestInit()
        {
            base.TestInit();

            var data = new DummyData();

            foreach (var s in data.ServiceSummaries) _viewModel.Services.Add(s);
            foreach (var s in data.StudentOptions) _viewModel.Students.Add(s);

            var specification = new SpecificationSummary(Guid.NewGuid(), "Test Specification",
                _viewModel.Students[0].Id, _viewModel.Students[0].FullName, _viewModel.Services[0].Id,
                _viewModel.Services[0].Name, 90, null, true, true, 9);

            _formViewModel = new SpecificationFormViewModel(_viewModel, specification);
        }

        [TestMethod]
        public void FillForm()
        {
            // Mode
            Assert.IsTrue(_formViewModel.IsEditMode);

            // Default values
            Assert.IsNull(_formViewModel.InfoMessage);
            Assert.AreEqual(InfoBarType.Success, _formViewModel.InfoBarType);
            Assert.IsFalse(_formViewModel.OpenInfoBar);

            Assert.AreEqual("Test Specification", _formViewModel.Name);
            Assert.IsNotNull(_formViewModel.SelectedService);
            Assert.IsNotNull(_formViewModel.SelectedStudent);
            Assert.IsTrue(string.IsNullOrWhiteSpace(_formViewModel.StudentSearchText));
            Assert.HasCount(0, _formViewModel.FilteredStudents);
            Assert.HasCount(11, _formViewModel.Students);
            Assert.HasCount(6, _formViewModel.Services);

            Assert.IsTrue(double.IsNaN(_formViewModel.Price));
            Assert.IsTrue(_formViewModel.IsPricePerHour);
            Assert.IsTrue(_formViewModel.IsOnline);
            Assert.IsTrue(_formViewModel.IsWeekendOrHoliday);

            Assert.IsNull(_formViewModel.GetBasePrice());
            _formViewModel.Price = 0;
            Assert.IsNull(_formViewModel.GetBasePrice());
        }
    }
}
