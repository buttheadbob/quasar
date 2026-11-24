using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Quasar.Client.Ui;
using Quasar.Components.Shared;
using Quasar.Ui;
using Xunit;

namespace Quasar.Tests.Ui;

public sealed class QuasarUiHostTests
{
    [Fact]
    public async Task DialogHostRendersNativeDialogAndCallsShow()
    {
        using var context = CreateContext();
        var cut = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        var reference = await dialogs.ShowAsync<HostTestDialog>("Host dialog");
        cut.WaitForAssertion(() =>
        {
            var dialog = cut.Find("dialog");
            Assert.Equal("Host dialog", dialog.GetAttribute("aria-label"));
            Assert.Contains("quasar-dialog", dialog.ClassList);
            Assert.Contains("<p>Host dialog content</p>", cut.Markup);
        });
        Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "quasarUi.dialogs.show");

        reference.Close(QuasarDialogResult.Ok("done"));
        cut.WaitForAssertion(() =>
            Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "quasarUi.dialogs.close"));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("dialog")));
        var result = await reference.Result;
        Assert.False(result.Canceled);
        Assert.Equal("done", result.Data);
    }

    [Fact]
    public void DialogHostAppliesOptions()
    {
        using var context = CreateContext();
        var cut = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        _ = cut.InvokeAsync(() => dialogs.ShowAsync<HostTestDialog>("Custom",
            options: new QuasarDialogOptions
            {
                Class = "custom-dialog",
                Style = "--quasar-dialog-radius: 12px;",
                MaxWidth = QuasarDialogWidth.Large,
                FullWidth = true,
            }));

        cut.WaitForAssertion(() =>
        {
            var dialog = cut.Find("dialog");
            Assert.Contains("custom-dialog", dialog.ClassList);
            Assert.Contains("quasar-dialog-width-lg", dialog.ClassList);
            Assert.Contains("quasar-dialog-full-width", dialog.ClassList);
            Assert.Contains("--quasar-dialog-radius", dialog.GetAttribute("style"));
        });
    }

    [Fact]
    public void ToastHostRendersSeverityRolesAndDismisses()
    {
        using var context = CreateContext();
        var cut = context.Render<QuasarToastHost>();
        var toasts = context.Services.GetRequiredService<IQuasarToastService>();

        toasts.Add("A success", QuasarSeverity.Success);
        toasts.Add("A failure", QuasarSeverity.Error);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".quasar-toast").Count);
            Assert.Single(cut.FindAll("[role='status']"));
            Assert.Single(cut.FindAll("[role='alert']"));
            var text = string.Join(" | ", cut.FindAll(".quasar-toast").Select(element => element.TextContent));
            Assert.Contains("A success", text);
            Assert.Contains("A failure", text);
        }, TimeSpan.FromSeconds(5));

        cut.FindAll("button[aria-label='Dismiss notification']")[0].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".quasar-toast"));
            Assert.Contains("A success", cut.Markup);
        });
    }

    [Fact]
    public async Task DialogHostRendersModelessDragHandle()
    {
        using var context = CreateContext();
        var cut = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        await cut.InvokeAsync(() => dialogs.ShowAsync<HostTestDialog>("Modeless",
            options: new QuasarDialogOptions { Modal = false }));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("quasar-dialog-modeless", cut.Find("dialog").ClassList);
            Assert.NotNull(cut.Find(".quasar-dialog-drag-handle"));
        });
    }

    [Theory]
    [InlineData("Delete", true)]
    [InlineData("Keep", false)]
    [InlineData("Cancel", null)]
    public async Task MessageBoxRendersChoicesAndMapsResult(string buttonText, bool? expected)
    {
        using var context = CreateContext();
        var cut = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        var task = dialogs.ShowMessageBoxAsync("Delete backup?", "Permanently delete it?", "Delete", "Keep", "Cancel");
        cut.WaitForAssertion(() => Assert.Contains("Permanently delete it?", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == buttonText).Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "quasarUi.dialogs.close"));
        Assert.Equal(expected, await task);
    }

    [Fact]
    public async Task MessageBoxHidesOptionalNoButton()
    {
        using var context = CreateContext();
        var cut = context.Render<QuasarDialogHost>();
        var dialogs = context.Services.GetRequiredService<IQuasarDialogService>();

        var task = dialogs.ShowMessageBoxAsync("Question", "Choose", "OK", cancelText: "Cancel");
        cut.WaitForAssertion(() =>
        {
            var labels = cut.FindAll("button").Select(button => button.TextContent.Trim()).ToArray();
            Assert.Equal(["Cancel", "OK"], labels);
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "quasarUi.dialogs.close"));
        Assert.Null(await task);
    }

    private static QuasarBunitContext CreateContext()
    {
        var context = new QuasarBunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        context.Services.AddQuasarUi();
        return context;
    }

    public sealed class HostTestDialog : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Host dialog content");
            builder.CloseElement();
        }
    }
}
