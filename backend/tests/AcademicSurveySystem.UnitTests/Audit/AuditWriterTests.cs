using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Domain.Audit.Entities;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Audit;

public sealed class AuditWriterTests
{
    [Fact]
    public void AuditEntry_RequiresUtcOccurrenceAndSafeRequiredFields()
    {
        Assert.Throws<DomainException>(() => new AuditEntry(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(-3)),
            null,
            "Admin",
            "identity.user.created",
            "identity",
            "User",
            Guid.NewGuid(),
            "Creó un usuario.",
            null));

        Assert.Throws<DomainException>(() => new AuditEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            null,
            "Admin",
            "",
            "identity",
            "User",
            Guid.NewGuid(),
            "Creó un usuario.",
            null));
    }

    [Fact]
    public async Task WriteAsync_AddsEntryWithoutSavingAutomatically()
    {
        await using var context = CreateContext();
        var actor = new CurrentAuditActorAccessor();
        var actorId = Guid.NewGuid();
        actor.SetCurrent(new AuditActor(actorId, "Admin User"));
        var writer = new AuditWriter(context, actor);

        await writer.WriteAsync(
            "identity.user.created",
            "identity",
            "User",
            Guid.NewGuid(),
            "Creó un usuario.",
            new { roleCodes = new[] { "administrator" } },
            CancellationToken.None);

        Assert.Single(context.ChangeTracker.Entries<AuditEntry>());
        Assert.Empty(await context.AuditEntries.ToArrayAsync());

        await context.SaveChangesAsync();

        var entry = await context.AuditEntries.SingleAsync();
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal("Admin User", entry.ActorDisplayName);
        Assert.Contains("administrator", entry.MetadataJson);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("hash")]
    [InlineData("jwt")]
    [InlineData("authorization")]
    [InlineData("accessCode")]
    [InlineData("comment")]
    [InlineData("answer")]
    [InlineData("otherText")]
    [InlineData("token")]
    [InlineData("secret")]
    public async Task WriteAsync_RejectsSensitiveMetadataTerms(string propertyName)
    {
        await using var context = CreateContext();
        var writer = new AuditWriter(context, new CurrentAuditActorAccessor());
        var metadata = new Dictionary<string, object?> { [propertyName] = "sensitive" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync(
            "identity.user.updated",
            "identity",
            "User",
            Guid.NewGuid(),
            "Actualizó un usuario.",
            metadata,
            CancellationToken.None));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
