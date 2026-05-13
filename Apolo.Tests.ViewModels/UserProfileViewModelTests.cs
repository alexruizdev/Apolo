using Apolo.Services;
using Models;
using Moq;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class UserProfileViewModelTests
    {
        private Mock<IUserProfileService> _mockUserProfileService = null!;
        private UserProfileViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
        {
            _mockUserProfileService = new Mock<IUserProfileService>();

            var userProfile = new UserProfile
            {
                TravelAllowance = 10,
                WeekendFee = 20
            };

            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);

            _viewModel = new UserProfileViewModel(
                _mockUserProfileService.Object);
        }

        // RefreshProfileAsync 
        [TestMethod]
        public async Task RefreshProfileAsync_UpdatesUserProfileData()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                TravelAllowance = 15,
                WeekendFee = 25
            };
            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);
            // Act
            await _viewModel.RefreshProfileAsync();
            // Assert
            Assert.AreEqual(15, _viewModel.TravelAllowance);
            Assert.AreEqual(25, _viewModel.WeekendFee);
        }
    }
}
