namespace PayrollCalculator.Application.Common;

/// <summary>Raised when a use case targets an aggregate that does not exist.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entity, int id) : base($"{entity} #{id} was not found.") { }

    public NotFoundException() { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }

    internal static async Task<T> OrThrow<T>(Task<T?> lookup, string entity, int id) where T : class =>
        await lookup.ConfigureAwait(false) ?? throw new NotFoundException(entity, id);
}
