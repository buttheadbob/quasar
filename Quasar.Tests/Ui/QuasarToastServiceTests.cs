using Microsoft.Extensions.Time.Testing;
using Quasar.Client.Ui.Toasts;
using Quasar.Ui;
using Xunit;

namespace Quasar.Tests.Ui;

public sealed class QuasarToastServiceTests
{
    [Fact]
    public void AddRemoveAndClearUpdateSnapshot()
    {
        var service = new QuasarToastService(new FakeTimeProvider(), new QuasarToastHostOptions());
        var id = service.Add("Hello", QuasarSeverity.Success);

        var toast = Assert.Single(service.Snapshot);
        Assert.Equal(id, toast.Id);
        Assert.Equal("Hello", toast.Message);
        Assert.Equal(QuasarSeverity.Success, toast.Severity);

        Assert.True(service.Remove(id));
        Assert.Empty(service.Snapshot);
        Assert.False(service.Remove(id));

        service.Add("One");
        service.Add("Two");
        service.Clear();
        Assert.Empty(service.Snapshot);
    }

    [Fact]
    public void VisibleToastsAreCappedToTheOldestRemoved()
    {
        var service = new QuasarToastService(new FakeTimeProvider(), new QuasarToastHostOptions { MaxVisibleToasts = 2 });
        service.Add("One");
        service.Add("Two");
        service.Add("Three");

        Assert.Equal(["Two", "Three"], service.Snapshot.Select(toast => toast.Message));
    }

    [Fact]
    public async Task TimerRemovesToastUnlessInteractionIsRequired()
    {
        var time = new FakeTimeProvider();
        var service = new QuasarToastService(time, new QuasarToastHostOptions());
        service.Add("Timed", QuasarSeverity.Info, options => options.Duration = TimeSpan.FromSeconds(5));
        service.Add("Sticky", QuasarSeverity.Warning, options => options.RequireInteraction = true);

        time.Advance(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        Assert.DoesNotContain(service.Snapshot, toast => toast.Message == "Timed");
        Assert.Contains(service.Snapshot, toast => toast.Message == "Sticky");
    }

    [Fact]
    public async Task PauseAndResumeKeepTheRemainingTime()
    {
        var time = new FakeTimeProvider();
        var service = new QuasarToastService(time, new QuasarToastHostOptions());
        var id = service.Add("Paused", QuasarSeverity.Normal, options => options.Duration = TimeSpan.FromSeconds(10));

        service.Pause(id);
        time.Advance(TimeSpan.FromSeconds(30));
        await Task.Delay(20);
        Assert.Single(service.Snapshot);

        service.Resume(id);
        time.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50);
        Assert.Empty(service.Snapshot);
    }

    [Fact]
    public void MissingMessageIsRejected()
    {
        var service = new QuasarToastService(new FakeTimeProvider(), new QuasarToastHostOptions());

        Assert.Throws<ArgumentException>(() => service.Add(" "));
    }
}
