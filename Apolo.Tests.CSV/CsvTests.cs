using CSV;
using Models;
using FluentAssertions;

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

            foreach (var lesson in dummy.Lessons) 
                lesson.Notes ??= string.Empty;

            reader.Services.Should().BeEquivalentTo(dummy.Services);
            reader.Payers.Should().BeEquivalentTo(dummy.Payers);
            reader.Students.Should().BeEquivalentTo(dummy.Students);
            reader.Specifications.Should().BeEquivalentTo(dummy.Specifications);
            reader.Lessons.Should().BeEquivalentTo(dummy.Lessons);
            reader.Bills.Should().BeEquivalentTo(dummy.Bills);

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

            foreach (var lesson in dummy.ArchiveLessons)
                lesson.Notes ??= string.Empty;

            reader.Services.Should().BeEmpty();
            reader.Payers.Should().BeEquivalentTo(dummy.ArchivePayers);
            reader.Students.Should().BeEquivalentTo(dummy.ArchiveStudents);
            reader.Specifications.Should().BeEmpty();
            reader.Lessons.Should().BeEquivalentTo(dummy.ArchiveLessons);
            reader.Bills.Should().BeEquivalentTo(dummy.ArchiveBills);

            Directory.Delete(tempPath, true);
        }
    }
}
