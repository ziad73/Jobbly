using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Xunit;

namespace Jobbly.Tests;

public sealed class SavedJobTests
{
    private static SavedJob MakeSaved() => SavedJob.Create(Guid.CreateVersion7(), Guid.CreateVersion7());

    [Fact]
    public void CreateStartsInSavedState()
    {
        var saved = MakeSaved();

        Assert.Equal(SavedJobStatus.Saved, saved.Status);
        Assert.Null(saved.AppliedAt);
        Assert.Null(saved.Notes);
    }

    [Fact]
    public void FirstAppliedTransitionRecordsTimestamp()
    {
        var saved = MakeSaved();
        var before = DateTime.UtcNow;

        saved.Update(SavedJobStatus.Applied);

        Assert.Equal(SavedJobStatus.Applied, saved.Status);
        Assert.NotNull(saved.AppliedAt);
        Assert.True(saved.AppliedAt >= before);
    }

    [Fact]
    public void LaterTransitionsKeepOriginalAppliedAt()
    {
        var saved = MakeSaved();
        saved.Update(SavedJobStatus.Applied);
        var first = saved.AppliedAt;

        saved.Update(SavedJobStatus.Closed);

        Assert.Equal(SavedJobStatus.Closed, saved.Status);
        Assert.Equal(first, saved.AppliedAt);
    }

    [Fact]
    public void PartialUpdateKeepsUntouchedFields()
    {
        var saved = MakeSaved();
        saved.Update(SavedJobStatus.Applied, "Talked to hiring manager", null);

        saved.Update(notes: "Second follow-up sent");

        Assert.Equal(SavedJobStatus.Applied, saved.Status);
        Assert.Equal("Second follow-up sent", saved.Notes);
        Assert.NotNull(saved.AppliedAt);
    }
}