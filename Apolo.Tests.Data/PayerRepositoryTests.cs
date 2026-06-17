using Models;
using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class PayerRepositoryTests : RepositoryTests
    {
        private PayerRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new PayerRepository(_context);
        }

        [TestMethod]
        public async Task GetPayersAsync_CalculatesUnpaidTotalsCorrectly()
        {
            var id = _data.Payers[0].Id;
            // Act
            var results = await _repository.GetPayersAsync();
            var payerSummary = results.First(p => p.Id == id);

            // Assert
            // Verify the debt is calculated (assuming 50 is the price)
            Assert.AreEqual("John", payerSummary.FirstName);
            Assert.AreEqual("Doe", payerSummary.LastName);
            Assert.AreEqual(297, payerSummary.Outstanding);
            Assert.AreEqual("123 Main St", payerSummary.Address);
            Assert.AreEqual("10001", payerSummary.Zip);
            Assert.AreEqual("New York", payerSummary.City);
            Assert.AreEqual("TX123456", payerSummary.TaxId);
        }

        [TestMethod]
        public async Task GetPayerSummaryNoOutstandingAsync()
        {
            var id = _data.Payers[4].Id;
            // Act
            var payerSummary = await _repository.GetPayerSummaryNoOutstandingAsync(id);

            // Assert
            // Verify the debt is calculated (assuming 50 is the price)
            Assert.AreEqual("Luca", payerSummary.FirstName);
            Assert.AreEqual("Rossi", payerSummary.LastName);
            Assert.AreEqual(0, payerSummary.Outstanding);
            Assert.AreEqual("Via Roma 15", payerSummary.Address);
            Assert.AreEqual("00100", payerSummary.Zip);
            Assert.AreEqual("Rome", payerSummary.City);
            Assert.AreEqual("IT11223344", payerSummary.TaxId);
        }

        [TestMethod]
        public async Task GetPayerSummaryNoOutstandingAsync_InvalidPayer()
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.GetPayerSummaryNoOutstandingAsync(Guid.NewGuid());
            });
        }

        [TestMethod]
        public async Task AddAsync_ValidPayer_SavesSuccessfully()
        {
            // Arrange
            var payer = new Payer { FirstName = "Test", LastName = "Payer" };

            // Act
            await _repository.AddAsync(payer);

            // Assert
            var saved = _context.Payers.FirstOrDefault(p => p.Id == payer.Id);
            Assert.IsNotNull(saved);
            Assert.AreEqual("Test", saved.FirstName);
            Assert.AreEqual("Payer", saved.LastName);
            Assert.IsNull(saved.Address);
            Assert.IsNull(saved.ZipCode);
            Assert.IsNull(saved.City);
            Assert.IsNull(saved.TaxId);
        }

        [TestMethod]
        public async Task AddAsync_ExistingPayer()
        {
            var payer = _data.Payers[0];

            // Act
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.AddAsync(payer);
            });

            var payerInDb = await _context.Payers.FindAsync([payer.Id], cancellationToken: TestContext.CancellationToken);

            Assert.HasCount(10, _context.Payers);
            Assert.IsNotNull(payerInDb);
        }

        // --- DELETE TESTS ---

        [TestMethod]
        public async Task DeleteAsync_ExistingId_RemovesFromDb()
        {
            var id = _data.Payers[8].Id;
            // Act
            await _repository.DeleteAsync(id);

            // Assert
            Assert.IsFalse(_context.Services.Any(s => s.Id == id));
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentId_ThrowsException()
        {
            // Act
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });
        }

        [TestMethod]
        public async Task DeleteAsync_WithAssociatedStudents_ThrowsInvalidOperationException()
        {
            var id = _data.Payers[0].Id;
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.DeleteAsync(id);
            });
        }

        [TestMethod]
        public async Task UpdateAsync_ValidChanges_UpdatesProperties()
        {
            var id = _data.Payers[1].Id;
            // Act
            await _repository.UpdateAsync(id, "New Name", "New Last Name", "New Address", "New Zip", "New City", "New Tax");

            // Assert
            var updated = _context.Payers.Find(id);
            Assert.IsNotNull(updated);
            Assert.AreEqual("New Name", updated.FirstName);
            Assert.AreEqual("New Last Name", updated.LastName);
            Assert.AreEqual("New Address", updated.Address);
            Assert.AreEqual("New Zip", updated.ZipCode);
            Assert.AreEqual("New City", updated.City);
            Assert.AreEqual("New Tax", updated.TaxId);
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentId_ThrowsException()
        {
            // Act
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.UpdateAsync(Guid.NewGuid(), 
                    "New Name", "New Last Name", "New Address", "New Zip", "New City", "New Tax");
            });
        }

        [TestMethod]
        public async Task GetPayerOptionsAsync_ReturnsAlphabeticalPayers()
        {
            // Act
            var options = (await _repository.GetPayerOptionsAsync()).ToList();

            // Assert
            Assert.AreEqual("Carlos Gomez", options[0].FullName);
            Assert.AreEqual("David Garcia", options[1].FullName);
            Assert.AreEqual("Emma Brown", options[2].FullName);
        }

        [TestMethod]
        public async Task GetPayerOptionsByUnbilledLessons_ReturnsAlphabeticalPayers()
        {
            // Act
            var options = (await _repository.GetPayerOptionsByUnbilledLessons()).ToList();

            // Assert
            Assert.AreEqual("Carlos Gomez - 1 lesson", options[0].FullName);
            Assert.AreEqual("David Garcia", options[1].FullName);
            Assert.AreEqual("Emma Brown", options[2].FullName);
        }

        public TestContext TestContext { get; set; }
    }
}
