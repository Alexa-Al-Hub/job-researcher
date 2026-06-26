using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.Jobs.Services;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobAgent.UnitTest.Application;

public class DescriptionServiceTests
{
    private static Mock<IScraper> ScraperFor(Platform platform)
    {
        var scraper = new Mock<IScraper>();
        scraper.SetupGet(s => s.Platform).Returns(platform);
        return scraper;
    }

    [Fact]
    public async Task FetchAsync_NoJobs_DoesNotCallScrapers()
    {
        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetMissingDescriptionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Job>());
        var scraper = ScraperFor(Platform.Dou);

        var service = new DescriptionService(new[] { scraper.Object }, repo.Object, NullLogger<DescriptionService>.Instance);
        await service.FetchAsync();

        scraper.Verify(s => s.FetchDescriptionsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.UpdateDescriptionsAsync(It.IsAny<IReadOnlyDictionary<int, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FetchAsync_GroupsByPlatform_AndPersistsFetchedDescriptions()
    {
        var jobs = new List<Job>
        {
            new() { Id = 1, Platform = Platform.Dou, Url = "https://dou/1" },
            new() { Id = 2, Platform = Platform.Dou, Url = "https://dou/2" },
            new() { Id = 3, Platform = Platform.Indeed, Url = "https://indeed/3" },
        };

        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetMissingDescriptionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(jobs);

        var dou = ScraperFor(Platform.Dou);
        dou.Setup(s => s.FetchDescriptionsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["https://dou/1"] = "Dou desc 1" });

        var indeed = ScraperFor(Platform.Indeed);
        indeed.Setup(s => s.FetchDescriptionsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["https://indeed/3"] = "Indeed desc 3" });

        IReadOnlyDictionary<int, string>? persisted = null;
        repo.Setup(r => r.UpdateDescriptionsAsync(It.IsAny<IReadOnlyDictionary<int, string>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyDictionary<int, string>, CancellationToken>((u, _) => persisted = u)
            .Returns(Task.CompletedTask);

        var service = new DescriptionService(new[] { dou.Object, indeed.Object }, repo.Object, NullLogger<DescriptionService>.Instance);
        await service.FetchAsync();

        // A single merged write across all platforms (DbContext is not thread-safe).
        repo.Verify(r => r.UpdateDescriptionsAsync(It.IsAny<IReadOnlyDictionary<int, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        persisted.Should().NotBeNull();
        persisted!.Should().ContainKey(1).WhoseValue.Should().Be("Dou desc 1");
        persisted.Should().ContainKey(3).WhoseValue.Should().Be("Indeed desc 3");
        persisted.Should().NotContainKey(2); // no description returned → not persisted
    }
}
