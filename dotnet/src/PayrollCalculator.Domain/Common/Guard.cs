namespace PayrollCalculator.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }

        return value.Trim();
    }

    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static int Positive(int value, string field) =>
        value > 0 ? value : throw new DomainException($"{field} must be greater than zero.");

    public static T NotNegative<T>(T value, string field) where T : System.Numerics.INumber<T> =>
        value >= T.Zero ? value : throw new DomainException($"{field} cannot be negative.");
}
