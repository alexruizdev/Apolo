using CSV;
using Models;

namespace Apolo.Tests.CSV
{
    [TestClass]
    public class CsvTests
    {
        [TestMethod]
        public async Task ReadWrite_NonArchive_RoundtripsProperly()
        {
            var dummy = new DummyData();
            var data = (
                dummy.Services,
                dummy.Payers,
                dummy.Students,
                dummy.Specifications,
                dummy.Lessons,
                dummy.Bills
            );

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            var writer = new Writer();
            writer.WriteCSV(tempPath, in data, archive: false);
            
            // Writer creates a subdirectory starting with "Apolo_Export_"
            var subdirectories = Directory.GetDirectories(tempPath, "Apolo_Export_*");
            Assert.HasCount(1, subdirectories, "Export directory was not created.");
            var exportFolder = subdirectories[0];

            var reader = new Reader();
            await reader.ReadCSV(exportFolder, false);

            Assert.HasCount(dummy.Services.Count, reader.Services, "Services count mismatch");
            Assert.HasCount(dummy.Payers.Count, reader.Payers, "Payers count mismatch");
            Assert.HasCount(dummy.Students.Count, reader.Students, "Students count mismatch");
            Assert.HasCount(dummy.Specifications.Count, reader.Specifications, "Specifications count mismatch");
            Assert.HasCount(dummy.Lessons.Count, reader.Lessons, "Lessons count mismatch");
            Assert.HasCount(dummy.Bills.Count, reader.Bills, "Bills count mismatch");

            Directory.Delete(tempPath, true);
        }

        [TestMethod]
        public async Task ReadWrite_Archive_RoundtripsProperly()
        {
            var dummy = new DummyData();
            var data = (
                new List<Service>(), 
                dummy.ArchivePayers,
                dummy.ArchiveStudents,
                new List<Specification>(),
                dummy.ArchiveLessons,
                dummy.ArchiveBills
            );

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            var writer = new Writer();
            writer.WriteCSV(tempPath, in data, archive: true);
            
            // Writer creates a subdirectory starting with "Apolo_Archive_"
            var subdirectories = Directory.GetDirectories(tempPath, "Apolo_Archive_*");
            Assert.HasCount(1, subdirectories, "Archive directory was not created.");
            var exportFolder = subdirectories[0];

            var reader = new Reader();
            await reader.ReadCSV(exportFolder, true);

            Assert.IsEmpty(reader.Services, "Services count should be 0 in archive mode");
            Assert.HasCount(dummy.ArchivePayers.Count, reader.Payers, "Payers count mismatch");
            Assert.HasCount(dummy.ArchiveStudents.Count, reader.Students, "Students count mismatch");
            Assert.IsEmpty(reader.Specifications, "Specifications count should be 0 in archive mode");
            Assert.HasCount(dummy.ArchiveLessons.Count, reader.Lessons, "Lessons count mismatch");
            Assert.HasCount(dummy.ArchiveBills.Count, reader.Bills, "Bills count mismatch");

            Directory.Delete(tempPath, true);
        }
    }
}
