namespace SyncTally.Web.Models;

/// <summary>Outcome of a trial step, returned by ITrialService so controllers stay thin.</summary>
public class TrialResult
{
    public bool Ok { get; set; }
    /// <summary>Message to show the visitor when Ok is false (or an info note when Ok is true).</summary>
    public string? Message { get; set; }
    /// <summary>Model property the message belongs to ("" = whole form).</summary>
    public string Field { get; set; } = string.Empty;
    public Guid? Reference { get; set; }
    public string? Email { get; set; }
    public bool AlreadyActive { get; set; }
    /// <summary>True when the e-mail is already a user of the application (shows the "use another email" panel).</summary>
    public bool EmailExists { get; set; }

    public static TrialResult Success(Guid reference, string? email = null, string? note = null) =>
        new() { Ok = true, Reference = reference, Email = email, Message = note };

    public static TrialResult EmailTaken() =>
        new() { Ok = false, EmailExists = true, Field = "Email", Message = "This email address is already registered with SyncTally." };

    public static TrialResult Fail(string message, string field = "") =>
        new() { Ok = false, Message = message, Field = field };
}
