namespace PayrollCalculator.Domain.Users;

/// <summary>
/// What counts as an acceptable password: a minimum length and some character variety, nothing more.
/// </summary>
/// <remarks>
/// There is no forced expiry and no ban on reusing old passwords. NIST SP 800-63B advises against both,
/// because they push people towards predictable sequences (<c>Janvier2026!</c>, <c>Fevrier2026!</c>).
/// Letters of scripts without case, such as Arabic, count in the lowercase class.
/// </remarks>
public sealed record PasswordPolicy
{
    public static readonly PasswordPolicy Default = new();

    public int MinimumLength { get; init; } = 8;

    /// <summary>How many of lowercase, uppercase, digits and symbols must appear.</summary>
    public int RequiredCharacterClasses { get; init; } = 3;

    public IReadOnlyList<PasswordViolation> Check(string? password, string userName)
    {
        var candidate = password ?? string.Empty;
        var violations = new List<PasswordViolation>();

        if (candidate.Length < MinimumLength)
        {
            violations.Add(PasswordViolation.TooShort);
        }

        if (CountCharacterClasses(candidate) < RequiredCharacterClasses)
        {
            violations.Add(PasswordViolation.TooSimple);
        }

        if (candidate.Length > 0 && candidate.Equals(userName?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(PasswordViolation.SameAsUserName);
        }

        return violations;
    }

    /// <exception cref="UserException">With <see cref="UserError.PasswordRejected"/> and every violation.</exception>
    public void Enforce(string? password, string userName)
    {
        var violations = Check(password, userName);
        if (violations.Count > 0)
        {
            throw new UserException(UserError.PasswordRejected, violations);
        }
    }

    private static int CountCharacterClasses(string value)
    {
        bool lower = false, upper = false, digit = false, symbol = false;
        foreach (var c in value)
        {
            if (char.IsUpper(c))
            {
                upper = true;
            }
            else if (char.IsLetter(c))
            {
                lower = true;
            }
            else if (char.IsDigit(c))
            {
                digit = true;
            }
            else
            {
                symbol = true;
            }
        }

        return (lower ? 1 : 0) + (upper ? 1 : 0) + (digit ? 1 : 0) + (symbol ? 1 : 0);
    }
}
