using JobAgent.Application.Common;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Appliers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobAgent.UnitTest.Infrastructure;

public class ClaudeApplicationFormServiceTests
{
    [Fact]
    public async Task AnswerAsync_NoApiKey_ReturnsNull()
    {
        var service = new ClaudeApplicationFormService(
            Options.Create(new CredentialOptions { AnthropicApiKey = "" }),
            Options.Create(new ApplicantProfileOptions()),
            NullLogger<ClaudeApplicationFormService>.Instance);

        var result = await service.AnswerAsync(
            "Are you authorized to work?",
            null,
            new Job { Title = "Developer", Company = "Acme", Platform = Platform.Dou });

        // No key → cannot answer → null, so the applier won't submit a guess.
        result.Should().BeNull();
    }
}
