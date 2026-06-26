using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Correspondence.DTOs;
using JobAgent.Application.Correspondence.Interfaces;
using JobAgent.Application.Correspondence.Services;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using App = JobAgent.Domain.Entities.Application;

namespace JobAgent.UnitTest.Application;

public class CorrespondenceServiceTests
{
    private static App OpenApplication(int id, int userId) => new()
    {
        Id = id,
        UserId = userId,
        Status = ApplicationStatus.Applied,
        Job = new Job { Company = "Acme", Title = ".NET Developer", Platform = Platform.Dou }
    };

    private static (Mock<IEmailClient>, Mock<IEmailTriageService>, Mock<ICorrespondenceRepository>, Mock<IApplicationRepository>)
        Mocks() => (new(), new(), new(), new());

    private static CorrespondenceService Build(
        Mock<IEmailClient> email, Mock<IEmailTriageService> triage,
        Mock<ICorrespondenceRepository> correspondence, Mock<IApplicationRepository> applications,
        GmailOptions options) =>
        new(email.Object, triage.Object, correspondence.Object, applications.Object,
            Options.Create(options), NullLogger<CorrespondenceService>.Instance);

    [Fact]
    public async Task SyncAsync_NoAccounts_DoesNothing()
    {
        var (email, triage, correspondence, applications) = Mocks();
        var service = Build(email, triage, correspondence, applications, new GmailOptions { Accounts = new() });

        await service.SyncAsync(new User { Id = 1 });

        email.Verify(e => e.FetchSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        applications.Verify(a => a.GetByStatusAsync(It.IsAny<ApplicationStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_InterviewInviteMatched_PersistsAndUpdatesStatus()
    {
        var (email, triage, correspondence, applications) = Mocks();

        applications.Setup(a => a.GetByStatusAsync(ApplicationStatus.Applied, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { OpenApplication(5, 1) });
        applications.Setup(a => a.GetByStatusAsync(ApplicationStatus.InterviewInvite, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<App>());

        correspondence.Setup(c => c.GetLastReceivedAtAsync("a@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);
        correspondence.Setup(c => c.ExistsAsync("a@gmail.com", "m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        email.Setup(e => e.FetchSinceAsync("a@gmail.com", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new EmailMessage("a@gmail.com", "m1", "t1", "recruiter@acme.com", "Interview", "Let's talk", DateTime.UtcNow)
            });

        triage.Setup(t => t.ClassifyAsync(It.IsAny<EmailMessage>(), It.IsAny<IReadOnlyList<CandidateApplication>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TriageResult(CorrespondenceCategory.InterviewInvite, 5, 90, "Sure, I'm available."));

        CorrespondenceMessage? saved = null;
        correspondence.Setup(c => c.AddAsync(It.IsAny<CorrespondenceMessage>(), It.IsAny<CancellationToken>()))
            .Callback<CorrespondenceMessage, CancellationToken>((m, _) => saved = m)
            .Returns(Task.CompletedTask);

        var service = Build(email, triage, correspondence, applications,
            new GmailOptions { Accounts = new() { "a@gmail.com" } });

        await service.SyncAsync(new User { Id = 1 });

        saved.Should().NotBeNull();
        saved!.ApplicationId.Should().Be(5);
        saved.Category.Should().Be(CorrespondenceCategory.InterviewInvite);
        saved.SuggestedReply.Should().Be("Sure, I'm available.");

        applications.Verify(a => a.UpdateStatusAsync(
            It.Is<App>(app => app.Id == 5), ApplicationStatus.InterviewInvite, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_LowConfidence_DoesNotChangeStatus()
    {
        var (email, triage, correspondence, applications) = Mocks();

        applications.Setup(a => a.GetByStatusAsync(ApplicationStatus.Applied, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { OpenApplication(5, 1) });
        applications.Setup(a => a.GetByStatusAsync(ApplicationStatus.InterviewInvite, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<App>());
        correspondence.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        email.Setup(e => e.FetchSinceAsync("a@gmail.com", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new EmailMessage("a@gmail.com", "m1", "t1", "noreply@jobs.com", "Newsletter", "...", DateTime.UtcNow)
            });

        // A weak/uncertain match (below threshold) must be stored but never alter status.
        triage.Setup(t => t.ClassifyAsync(It.IsAny<EmailMessage>(), It.IsAny<IReadOnlyList<CandidateApplication>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TriageResult(CorrespondenceCategory.Rejection, 5, 40, null));

        CorrespondenceMessage? saved = null;
        correspondence.Setup(c => c.AddAsync(It.IsAny<CorrespondenceMessage>(), It.IsAny<CancellationToken>()))
            .Callback<CorrespondenceMessage, CancellationToken>((m, _) => saved = m)
            .Returns(Task.CompletedTask);

        var service = Build(email, triage, correspondence, applications,
            new GmailOptions { Accounts = new() { "a@gmail.com" } });

        await service.SyncAsync(new User { Id = 1 });

        saved.Should().NotBeNull();
        saved!.ApplicationId.Should().BeNull(); // below confidence threshold → not linked
        applications.Verify(a => a.UpdateStatusAsync(It.IsAny<App>(), It.IsAny<ApplicationStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
