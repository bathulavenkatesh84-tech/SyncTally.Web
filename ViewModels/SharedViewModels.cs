using System.ComponentModel.DataAnnotations;

namespace SyncTally.Web.ViewModels;

/// <summary>One breadcrumb item (Url == null means "current page").</summary>
public record Crumb(string Label, string? Url = null);

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int Total { get; set; }
    public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

/// <summary>Input for the shared pager partial.</summary>
public class PagerViewModel
{
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = "Index";
    public int Page { get; set; }
    public int Pages { get; set; }
    /// <summary>Other query-string values to keep while paging (q, tag, sort …).</summary>
    public Dictionary<string, string?> Query { get; set; } = new();
}

public class ErrorViewModel
{
    public int StatusCode { get; set; } = 500;
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "An unexpected error occurred. Our team has been notified.";
    public string? RequestId { get; set; }
}

// ---------- Navigation ----------
public class MenuItem
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class MenuGroup
{
    public string Title { get; set; } = string.Empty;
    public List<MenuItem> Items { get; set; } = new();
}

public class NavMenuViewModel
{
    /// <summary>Four columns → groups → items, exactly like the original mega menu.</summary>
    public List<List<MenuGroup>> Columns { get; set; } = new();
    public string ActiveController { get; set; } = string.Empty;
}

// ---------- Features ----------
public class FeatureCardViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string LiveStatus { get; set; } = "live";
    public string Lead { get; set; } = string.Empty;
    public string GroupTitle { get; set; } = string.Empty;
}

public class FeatureGroupViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<FeatureCardViewModel> Features { get; set; } = new();
}

public class FeatureListViewModel
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public List<FeatureGroupViewModel> Groups { get; set; } = new();
    public int Total { get; set; }
}

public class TitleTextViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class FeatureDetailViewModel
{
    public FeatureCardViewModel Feature { get; set; } = new();
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    /// <summary>Title = benefit, Text = description.</summary>
    public List<TitleTextViewModel> Benefits { get; set; } = new();
    /// <summary>Title = headline value ("1 tap"), Text = label.</summary>
    public List<TitleTextViewModel> Stats { get; set; } = new();
    /// <summary>Title = question, Text = answer.</summary>
    public List<TitleTextViewModel> Faqs { get; set; } = new();
    public List<string> Points { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    public bool HasApp { get; set; }
    public string AppLabel { get; set; } = "Open in app";
    public List<FeatureCardViewModel> Related { get; set; } = new();
}

// ---------- Home / pricing ----------
public class JourneyStepViewModel
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class FaqViewModel
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class PlanViewModel
{
    public string Name { get; set; } = string.Empty;
    public decimal Price1Y { get; set; }
    /// <summary>3-year billing is 20% cheaper per year.</summary>
    public decimal Price3Y => Math.Round(Price1Y * 0.8m);
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
}

public class HomeViewModel
{
    public List<JourneyStepViewModel> Journey { get; set; } = new();
    public List<FeatureCardViewModel> Highlights { get; set; } = new();
    public List<FaqViewModel> Faqs { get; set; } = new();
    public string TestimonialQuote { get; set; } = string.Empty;
    public string TestimonialAuthor { get; set; } = string.Empty;
}

// ---------- Blog ----------
public class BlogCardViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public DateTime PublishedOn { get; set; }
    public int ReadMinutes { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public string Icon { get; set; } = "📰";
    public string Author { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
}

public class BlogCategoryViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class BlogListViewModel
{
    public string? Q { get; set; }
    public string? Tag { get; set; }
    public string Sort { get; set; } = "newest";
    public List<string> Tags { get; set; } = new();
    public List<BlogCategoryViewModel> Categories { get; set; } = new();
    public int AllCount { get; set; }
    /// <summary>Shown only on the unfiltered first page.</summary>
    public BlogCardViewModel? Featured { get; set; }
    public PagedResult<BlogCardViewModel> Result { get; set; } = new();
}

public class BlogDetailViewModel
{
    public BlogCardViewModel Post { get; set; } = new();
    public List<string> Body { get; set; } = new();
    public int Views { get; set; }
    public string AuthorRole { get; set; } = string.Empty;
    public List<BlogCardViewModel> Related { get; set; } = new();
}

// ---------- Partners ----------
public class PartnerViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Blurb { get; set; } = string.Empty;
}

public class PartnerApplyViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Name must be 2–120 characters.")]
    [Display(Name = "Your name / firm")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    [Display(Name = "Mobile")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(60)]
    public string City { get; set; } = string.Empty;
}

public class PartnerListViewModel
{
    public string? Q { get; set; }
    public string? Kind { get; set; }
    public string Sort { get; set; } = "name";
    public List<string> Kinds { get; set; } = new();
    public PagedResult<PartnerViewModel> Result { get; set; } = new();
    public PartnerApplyViewModel Apply { get; set; } = new();
}

// ---------- Contact / WhatsApp ----------
public class ContactViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters.")]
    [Display(Name = "Your name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a topic.")]
    public string Topic { get; set; } = "Sales & pricing";

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters.")]
    public string Message { get; set; } = string.Empty;

    public static readonly string[] Topics = { "Sales & pricing", "Tally setup help", "Support", "Partnership" };
}

/// <summary>JSON body posted by the "Chat on WhatsApp" dialog.</summary>
public class ChatLeadViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Topic { get; set; } = "See a live demo";

    [StringLength(500)]
    public string? Message { get; set; }

    public static readonly string[] Topics = { "See a live demo", "Know pricing", "Connect my Tally", "Get support", "Become a partner" };
}

public class ContactPageViewModel
{
    public ContactViewModel Form { get; set; } = new();
    public string SupportEmail { get; set; } = string.Empty;
    public string SupportPhone { get; set; } = string.Empty;
    public string SupportHours { get; set; } = string.Empty;
}


// ---------- Free trial ----------
public class TrialStartViewModel
{
    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters.")]
    [Display(Name = "Your name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Company name must be 2–150 characters.")]
    [Display(Name = "Company / firm name")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Work email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200)]
    [Display(Name = "Email (this will be your username)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    [Display(Name = "Mobile")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(60)]
    public string City { get; set; } = string.Empty;

    [Range(1, 500, ErrorMessage = "Enter a number between 1 and 500.")]
    [Display(Name = "Number of salesmen")]
    public int SalesmenCount { get; set; } = 3;

    [StringLength(40)]
    public string? Plan { get; set; }
}

public class TrialVerifyViewModel
{
    public Guid Ref { get; set; }

    /// <summary>E-mail with the middle hidden, for display only (never posted back).</summary>
    public string MaskedEmail { get; set; } = string.Empty;

    /// <summary>Seconds until a new code may be requested (drives the resend button).</summary>
    public int ResendInSeconds { get; set; }

    [Required(ErrorMessage = "Enter the 6-digit code from your email.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "The code is 6 digits.")]
    [Display(Name = "6-digit code")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Use at least one uppercase letter, one lowercase letter and one number.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class TrialWelcomeViewModel
{
    public Guid Ref { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime TrialEnd { get; set; }
    public int Days { get; set; }
    public string LoginUrl { get; set; } = string.Empty;
}
