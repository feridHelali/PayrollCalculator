using PayrollCalculator.Application.Licensing;

namespace PayrollCalculator.Application.Tests;

public class LicenseServiceTests
{
    private const string Serial = "0123456789ABCDEFGHJK";

    private sealed class Registry(params string[] serials) : ISerialRegistry
    {
        private readonly HashSet<string> _hashes = serials.Select(SerialKey.Hash).ToHashSet();

        public bool IsKnown(string hash) => _hashes.Contains(hash);
    }

    private sealed class Store : ILicenseStore
    {
        public LicenseRecord? Record { get; set; }

        public Task<LicenseRecord?> ReadAsync(CancellationToken ct = default) => Task.FromResult(Record);

        public Task WriteAsync(LicenseRecord record, CancellationToken ct = default)
        {
            Record = record;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void A_serial_is_read_whatever_its_case_dashes_and_look_alike_letters()
    {
        Assert.Equal(Serial, SerialKey.Normalize(" 01234-56789-abcde-fghjk "));
        Assert.Equal(Serial, SerialKey.Normalize("O1234-56789-ABCDE-FGHJK")); // O read as 0
        Assert.Equal("1" + Serial[1..], SerialKey.Normalize("L1234-56789-ABCDE-FGHJK")); // L read as 1
        Assert.Null(SerialKey.Normalize("01234-56789-ABCDE-FGHJ")); // 19 characters
        Assert.Null(SerialKey.Normalize("01234-56789-ABCDE-FGHJU")); // U is not in the alphabet
        Assert.Equal("01234-56789-ABCDE-FGHJK", SerialKey.Format(Serial));
        Assert.Equal("*****-*****-*****-FGHJK", SerialKey.Mask(Serial));
    }

    [Fact]
    public void The_hash_is_the_one_of_the_generator_script()
    {
        // Computed by tools/generate-serials.ps1's Get-SerialHash for the same serial.
        Assert.Equal("d3394f9c0816e48ae35826082b833346773cd68344295e26c97af33fcbeac030", SerialKey.Hash(Serial));
    }

    [Fact]
    public async Task A_known_serial_activates_the_copy_and_is_kept()
    {
        var store = new Store();
        var service = new LicenseService(new Registry(Serial), store, TimeProvider.System);
        Assert.Null(await service.CurrentAsync());

        Assert.Equal(ActivationOutcome.InvalidFormat, await service.ActivateAsync("123"));
        Assert.Equal(ActivationOutcome.Unknown, await service.ActivateAsync("ZZZZZ-ZZZZZ-ZZZZZ-ZZZZZ"));
        Assert.Null(store.Record);

        Assert.Equal(ActivationOutcome.Activated, await service.ActivateAsync("01234 56789 abcde fghjk"));
        Assert.Equal("01234-56789-ABCDE-FGHJK", store.Record!.Serial);
        Assert.Equal("*****-*****-*****-FGHJK", (await service.CurrentAsync())!.MaskedSerial);
    }

    [Fact]
    public async Task A_kept_serial_that_this_version_no_longer_accepts_asks_again()
    {
        var store = new Store { Record = new LicenseRecord("01234-56789-ABCDE-FGHJK", DateTimeOffset.UnixEpoch) };

        Assert.Null(await new LicenseService(new Registry(), store, TimeProvider.System).CurrentAsync());
    }
}
