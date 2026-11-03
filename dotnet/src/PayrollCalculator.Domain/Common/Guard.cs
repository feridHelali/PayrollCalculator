namespace PayrollCalculator.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, DomainField field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainError.Required, field);
        }

        return value.Trim();
    }

    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static int Positive(int value, DomainField field) =>
        value > 0 ? value : throw new DomainException(DomainError.MustBePositive, field);

    public static T NotNegative<T>(T value, DomainField field) where T : System.Numerics.INumber<T> =>
        value >= T.Zero ? value : throw new DomainException(DomainError.CannotBeNegative, field);
}
