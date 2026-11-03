using System.Windows;

namespace PayrollCalculator.Wpf.Controls;

/// <summary>Attached properties read by the theme's button templates.</summary>
public static class Ui
{
    /// <summary>A glyph from <see cref="Icons"/> shown before the button's content.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(Ui), new FrameworkPropertyMetadata(null));

    /// <summary>Marks the side-menu entry of the page on screen.</summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(Ui), new FrameworkPropertyMetadata(false));

    public static string? GetIcon(DependencyObject element) => (string?)element?.GetValue(IconProperty);

    public static void SetIcon(DependencyObject element, string? value) => element?.SetValue(IconProperty, value);

    public static bool GetIsActive(DependencyObject element) => element is not null && (bool)element.GetValue(IsActiveProperty);

    public static void SetIsActive(DependencyObject element, bool value) => element?.SetValue(IsActiveProperty, value);
}

/// <summary>
/// Segoe Fluent Icons glyphs (Windows 11), which share code points with Segoe MDL2 Assets (Windows 10 fallback).
/// Native glyphs cost nothing, scale cleanly and follow the system style; no icon package or image files.
/// </summary>
/// <remarks>Arrows are chosen for a right-to-left screen: "back" points right.</remarks>
public static class Icons
{
    public const string Affairs = "";        // Work (briefcase): a legal case
    public const string SalaryTables = "";   // ViewAll (grid): the categories × degrees grid
    public const string Agreements = "";     // Library: the agreements' texts
    public const string Allowances = "";     // PaymentCard: the primes and allowances
    public const string Ledger = "";         // Document: the monthly ledger of an affair
    public const string Users = "";          // People
    public const string User = "";           // Contact
    public const string AddUser = "";        // AddFriend
    public const string ResetPassword = "";  // Permissions (key)
    public const string Generate = "";       // Shuffle
    public const string SignOut = "";        // PowerButton
    public const string SignIn = "";         // Accept
    public const string Password = "";       // Lock
    public const string Import = "";         // Import
    public const string Save = "";
    public const string New = "";            // Add
    public const string Edit = "";
    public const string Delete = "";
    public const string Back = "";           // Forward glyph = an arrow pointing right, i.e. "back" in RTL
    public const string Timeline = "";       // Calculator
    public const string Export = "";         // Download
    public const string Close = "";          // Cancel
    public const string About = "";          // Info
    public const string Copy = "";
}
