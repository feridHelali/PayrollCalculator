using PayrollCalculator.Application.Licensing;
using PayrollCalculator.Infrastructure.Licensing;

namespace PayrollCalculator.Infrastructure.Tests;

public sealed class LicensingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "payroll-licence-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void The_app_ships_the_hashes_of_the_generated_serials_and_never_the_serials()
    {
        Assert.True(EmbeddedSerialRegistry.All.Count >= 100);
        Assert.All(EmbeddedSerialRegistry.All, h => Assert.Matches("^[0-9a-f]{64}$", h));
        Assert.False(new EmbeddedSerialRegistry().IsKnown(SerialKey.Hash("0123456789ABCDEFGHJK")));
    }

    [Fact]
    public async Task The_activation_is_kept_in_the_data_folder()
    {
        var store = new FileLicenseStore(new DatabaseOptions { DatabasePath = Path.Combine(_root, "data", "payroll.db") });
        Assert.Null(await store.ReadAsync());

        var record = new LicenseRecord("01234-56789-ABCDE-FGHJK", new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));
        await store.WriteAsync(record);

        Assert.Equal(Path.Combine(_root, "data", "license.json"), store.FilePath);
        Assert.Equal(record, await store.ReadAsync());

        await File.WriteAllTextAsync(store.FilePath, "{ damaged");
        Assert.Null(await store.ReadAsync()); // asks for the serial again rather than failing
    }
}
