using PayrollCalculator.Application.Common;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Application.Tests;

public sealed class UserServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly FakeHasher _hasher = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.Zero));

    private UserService Service() => new(_store.Users, _store, _hasher, PasswordPolicy.Default, _clock);

    [Fact]
    public async Task Create_adds_a_user_with_a_temporary_password()
    {
        var id = await Service().CreateAsync(" Sami ", "سامي", "Temp@2026");

        var user = Assert.Single(_store.Users.Items);
        Assert.Equal(id, user.Id);
        Assert.Equal("sami", user.UserName);
        Assert.Equal("hashed:Temp@2026", user.PasswordHash);
        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public async Task Create_refuses_a_taken_user_name()
    {
        await Service().CreateAsync("sami", "سامي", "Temp@2026");

        var ex = await Assert.ThrowsAsync<UserException>(() => Service().CreateAsync("SAMI", "Other", "Temp@2026"));
        Assert.Equal(UserError.UserNameTaken, ex.Error);
    }

    [Fact]
    public async Task Create_enforces_the_password_policy()
    {
        var ex = await Assert.ThrowsAsync<UserException>(() => Service().CreateAsync("sami", "سامي", "abc"));
        Assert.Equal(UserError.PasswordRejected, ex.Error);
        Assert.Empty(_store.Users.Items);
    }

    [Fact]
    public async Task Create_validates_the_user_name_before_anything_else()
    {
        var ex = await Assert.ThrowsAsync<UserException>(() => Service().CreateAsync("bad name", "x", "abc"));
        Assert.Equal(UserError.UserNameInvalid, ex.Error);
    }

    [Fact]
    public async Task Reset_sets_a_new_temporary_password_and_unlocks()
    {
        var id = await Service().CreateAsync("sami", "سامي", "Temp@2026");
        var user = _store.Users.Items[0];
        user.ChangePassword("hashed:Mine@2026", _clock.Now);
        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            user.RecordSignIn(false, _clock.Now);
        }

        await Service().ResetPasswordAsync(id, "Reset@2026");

        Assert.Equal("hashed:Reset@2026", user.PasswordHash);
        Assert.True(user.MustChangePassword);
        Assert.False(user.IsLockedAt(_clock.Now));
    }

    [Fact]
    public async Task Reset_enforces_the_policy_and_needs_an_existing_user()
    {
        var id = await Service().CreateAsync("sami", "سامي", "Temp@2026");

        await Assert.ThrowsAsync<UserException>(() => Service().ResetPasswordAsync(id, "123"));
        await Assert.ThrowsAsync<NotFoundException>(() => Service().ResetPasswordAsync(999, "Reset@2026"));
    }

    [Fact]
    public async Task List_is_sorted_and_reports_each_status()
    {
        await Service().CreateAsync("zied", "زياد", "Temp@2026");
        await Service().CreateAsync("amel", "أمل", "Temp@2026");
        await Service().CreateAsync("badr", "بدر", "Temp@2026");
        var amel = _store.Users.Items.Single(u => u.UserName == "amel");
        amel.ChangePassword("hashed:Mine@2026", _clock.Now);
        var badr = _store.Users.Items.Single(u => u.UserName == "badr");
        badr.ChangePassword("hashed:Mine@2026", _clock.Now);
        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            badr.RecordSignIn(false, _clock.Now);
        }

        var list = await Service().ListAsync();

        Assert.Equal(["amel", "badr", "zied"], list.Select(u => u.UserName));
        Assert.Equal([UserStatus.Active, UserStatus.Locked, UserStatus.MustChangePassword], list.Select(u => u.Status));
        Assert.Equal(_clock.Now, list[0].LastSignedInOn);
    }

    [Fact]
    public void Generated_temporary_passwords_satisfy_the_policy_and_differ()
    {
        var samples = Enumerable.Range(0, 200).Select(_ => TemporaryPassword.Generate()).ToList();

        Assert.All(samples, p => Assert.Empty(PasswordPolicy.Default.Check(p, "admin")));
        Assert.All(samples, p => Assert.Equal(TemporaryPassword.Length, p.Length));
        Assert.True(samples.Distinct().Count() > 195);
    }

    [Fact]
    public async Task Seeder_creates_the_default_admin_once()
    {
        var seeder = new UserSeeder(_store.Users, _store, _hasher);

        Assert.True(await seeder.SeedAsync());
        Assert.False(await seeder.SeedAsync());

        var admin = Assert.Single(_store.Users.Items);
        Assert.Equal(UserSeeder.DefaultUserName, admin.UserName);
        Assert.True(_hasher.Verify(UserSeeder.DefaultPassword, admin.PasswordHash));
        Assert.True(admin.MustChangePassword);
        Assert.Empty(PasswordPolicy.Default.Check(UserSeeder.DefaultPassword, UserSeeder.DefaultUserName));
    }

    [Fact]
    public async Task Seeder_does_nothing_when_users_exist()
    {
        await Service().CreateAsync("sami", "سامي", "Temp@2026");

        Assert.False(await new UserSeeder(_store.Users, _store, _hasher).SeedAsync());
        Assert.Single(_store.Users.Items);
    }
}
