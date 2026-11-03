using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Application.Tests;

/// <summary>Readable, reversible "hash" so tests can assert on it. Counts calls to prove timing-equalisation.</summary>
internal sealed class FakeHasher : IPasswordHasher
{
    public int HashCalls { get; private set; }

    public string Hash(string password)
    {
        HashCalls++;
        return "hashed:" + password;
    }

    public bool Verify(string password, string storedHash) => storedHash == "hashed:" + password;
}

internal sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}
