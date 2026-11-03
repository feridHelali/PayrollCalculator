using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Users;
using PayrollCalculator.Infrastructure.Persistence;
using PayrollCalculator.Infrastructure.Security;

namespace PayrollCalculator.Infrastructure.Tests;

public sealed class Pbkdf2PasswordHasherTests
{
    // Few iterations keep the suite fast; the format and the algorithm are the same.
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1_000);

    [Fact]
    public void Hash_is_versioned_salted_and_verifiable()
    {
        var first = _hasher.Hash("Admin@2026");
        var second = _hasher.Hash("Admin@2026");

        Assert.StartsWith("pbkdf2-sha256$1000$", first, StringComparison.Ordinal);
        Assert.Equal(4, first.Split('$').Length);
        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("Admin@2026", first));
        Assert.True(_hasher.Verify("Admin@2026", second));
        Assert.False(_hasher.Verify("admin@2026", first));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("md5$1$abc$def")]
    [InlineData("pbkdf2-sha256$x$abc$def")]
    [InlineData("pbkdf2-sha256$1000$***$***")]
    public void Malformed_hashes_never_verify_and_never_throw(string stored)
    {
        Assert.False(_hasher.Verify("anything", stored));
    }

    [Fact]
    public void The_iterations_stored_in_the_hash_are_used_to_verify()
    {
        var older = new Pbkdf2PasswordHasher(iterations: 500).Hash("Admin@2026");
        Assert.True(_hasher.Verify("Admin@2026", older));
    }

    [Fact]
    public void Default_iterations_follow_owasp()
    {
        Assert.Equal(600_000, Pbkdf2PasswordHasher.DefaultIterations);
    }
}

public sealed class UserPersistenceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task User_round_trips_with_lockout_state()
    {
        var now = new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);
        int id;
        await using (var context = _db.NewContext())
        {
            var user = User.Register("sami", "سامي", "hash");
            for (var i = 0; i < User.MaxFailedAttempts; i++)
            {
                user.RecordSignIn(false, now);
            }

            new UserRepository(context).Add(user);
            await context.SaveChangesAsync();
            id = user.Id;
        }

        await using (var context = _db.NewContext())
        {
            var loaded = await new UserRepository(context).FindByUserNameAsync("sami");
            Assert.NotNull(loaded);
            Assert.Equal(id, loaded.Id);
            Assert.Equal("سامي", loaded.DisplayName);
            Assert.True(loaded.MustChangePassword);
            Assert.Equal(User.MaxFailedAttempts, loaded.FailedAttempts);
            Assert.True(loaded.IsLockedAt(now));
            Assert.Null(await new UserRepository(context).FindByUserNameAsync("nobody"));
            Assert.True(await new UserRepository(context).AnyAsync());
        }
    }

    [Fact]
    public async Task User_names_are_unique_in_the_database()
    {
        await using var context = _db.NewContext();
        context.Users.Add(User.Register("sami", "A", "h"));
        context.Users.Add(User.Register("sami", "B", "h"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Seeder_and_sign_in_work_end_to_end_on_sqlite()
    {
        var hasher = new Pbkdf2PasswordHasher(iterations: 1_000);
        await using (var context = _db.NewContext())
        {
            Assert.True(await new UserSeeder(new UserRepository(context), context, hasher).SeedAsync());
        }

        var session = new CurrentUser();
        await using (var context = _db.NewContext())
        {
            var auth = new AuthenticationService(new UserRepository(context), context, hasher, PasswordPolicy.Default, TimeProvider.System, session);
            Assert.Equal(SignInOutcome.MustChangePassword, await auth.SignInAsync("admin", UserSeeder.DefaultPassword));
            await auth.ChangePasswordAsync("admin", UserSeeder.DefaultPassword, "Mon-Nouveau-2026", "Mon-Nouveau-2026");
        }

        await using (var context = _db.NewContext())
        {
            var auth = new AuthenticationService(new UserRepository(context), context, hasher, PasswordPolicy.Default, TimeProvider.System, session);
            Assert.Equal(SignInOutcome.Succeeded, await auth.SignInAsync("ADMIN", "Mon-Nouveau-2026"));
            Assert.Equal(UserSeeder.DefaultDisplayName, session.Session?.DisplayName);
        }
    }
}
