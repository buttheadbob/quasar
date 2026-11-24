using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Quasar.Client.Ui;
using Quasar.Components.Layout;
using Quasar.Components.Shared;
using Quasar.Ui;
using Xunit;

namespace Quasar.Tests;

public sealed class QuasarControlDialogTests
{
    [Fact]
    public async Task UnavailableRestartIsDisabledAndExplainsRepair()
    {
        using var context = new QuasarBunitContext();
        context.Services.AddLogging();
        context.Services.AddQuasarUi();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var host = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        _ = dialogs.ShowAsync<QuasarControlDialog>("Quasar power",
            new QuasarDialogParameters<QuasarControlDialog>
            {
                { dialog => dialog.IsRestartAvailable, false },
                { dialog => dialog.RestartUnavailableReason, "Repair the native updater service." },
            });

        host.WaitForAssertion(() =>
        {
            Assert.True(host.FindAll("button")
                .Single(button => button.TextContent.Contains("Restart Quasar"))
                .HasAttribute("disabled"));
            Assert.Contains("Repair the native updater service.", host.Markup);
        });
    }

    [Theory]
    [InlineData("Shutdown Quasar", QuasarControlAction.ShutdownQuasar)]
    [InlineData("Restart Quasar", QuasarControlAction.RestartQuasar)]
    public async Task ConfirmReturnsSelectedAction(string buttonText, QuasarControlAction action)
    {
        using var context = new QuasarBunitContext();
        context.Services.AddLogging();
        context.Services.AddQuasarUi();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var host = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        var reference = await dialogs.ShowAsync<QuasarControlDialog>("Quasar power",
            new QuasarDialogParameters<QuasarControlDialog>
            {
                { dialog => dialog.IsRestartAvailable, true },
            });

        host.WaitForAssertion(() => Assert.Contains(buttonText, host.Markup));

        host.FindAll("button").Single(button => button.TextContent.Contains(buttonText)).Click();
        host.WaitForAssertion(() =>
            Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "quasarUi.dialogs.close"));
        await host.InvokeAsync(() =>
            host.FindComponent<QuasarDialogInstanceHost>().Instance.OnNativeClosedAsync("close"));

        var result = await reference.Result.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(result.Canceled);
        Assert.Equal(action, result.Data);
    }

    [Fact]
    public async Task RestartConfirmationWarnsBeforeApplyingThePendingUpdate()
    {
        using var context = new QuasarBunitContext();
        context.Services.AddLogging();
        context.Services.AddQuasarUi();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var host = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        var reference = await dialogs.ShowAsync<QuasarControlDialog>("Quasar power",
            new QuasarDialogParameters<QuasarControlDialog>
            {
                { dialog => dialog.IsRestartAvailable, true },
                { dialog => dialog.PendingUpdateVersion, "1.6.0.99" },
            });

        host.WaitForAssertion(() => Assert.Contains("Restart Quasar", host.Markup));
        host.FindAll("button").Single(button => button.TextContent.Contains("Restart Quasar")).Click();
        host.WaitForAssertion(() =>
            Assert.Contains("Quasar 1.6.0.99 will also be installed during this restart.", host.Markup));
        Assert.False(reference.Result.IsCompleted);

        host.FindAll("button").Single(button => button.TextContent.Contains("Update and Restart Quasar")).Click();
        host.WaitForAssertion(() =>
            Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "quasarUi.dialogs.close"));
        await host.InvokeAsync(() =>
            host.FindComponent<QuasarDialogInstanceHost>().Instance.OnNativeClosedAsync("close"));

        var result = await reference.Result.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(QuasarControlAction.RestartQuasar, result.Data);
    }
}
