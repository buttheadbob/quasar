namespace Quasar.Ui;

/// <summary>
/// An immutable toast entry tracked by <see cref="IQuasarToastService"/>.
/// </summary>
/// <remarks>
/// The host renders this record. It contains no mutable state, so a snapshot
/// can be read safely while timers add or remove other toasts.
/// </remarks>
/// <param name="Id">Stable toast identifier.</param>
/// <param name="Message">Plain-text message.</param>
/// <param name="Severity">Severity used for color, icon, and accessibility role.</param>
/// <param name="Options">Per-toast options.</param>
/// <param name="CreatedAt">Creation time from the host's time provider.</param>
public sealed record QuasarToast(
    Guid Id,
    string Message,
    QuasarSeverity Severity,
    QuasarToastOptions Options,
    DateTimeOffset CreatedAt);
