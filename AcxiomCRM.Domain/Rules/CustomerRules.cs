using System.Text.RegularExpressions;

namespace AcxiomCRM.Domain.Rules;

public static class CustomerRules
{
    // Accepts 10-15 digits, optional leading +, parentheses, hyphens, and spaces
    private static readonly Regex PhoneRegex = new(@"^\+?([0-9()\-. \s]{7,25})$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public const string InvalidEmailError = "Enter a valid email address.";
    public const string InvalidPhoneError = "Enter a valid phone number (10-15 digits).";
    public const string DuplicateEmailError = "A customer with this email address already exists.";
    public const string DuplicatePhoneError = "A customer with this phone number already exists.";
    public const string DuplicateCustomerError = "A customer with identical business name and email already exists.";

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRegex.IsMatch(email.Trim());
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        return digitsOnly.Length >= 10 && digitsOnly.Length <= 15 && PhoneRegex.IsMatch(phone.Trim());
    }
}
