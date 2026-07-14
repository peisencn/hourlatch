using WiseAutoShutdown.Core.Security;
using Xunit;

namespace WiseAutoShutdown.Core.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Created_hash_verifies_only_the_original_password()
    {
        var hasher = new PasswordHasher(iterations: 210_000);
        var record = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", record));
        Assert.False(hasher.Verify("wrong", record));
        Assert.NotEqual("correct horse battery staple", record.HashBase64);
    }

    [Fact]
    public void Hashes_use_unique_salts()
    {
        var hasher = new PasswordHasher();

        Assert.NotEqual(hasher.Hash("same").SaltBase64, hasher.Hash("same").SaltBase64);
    }

    [Fact]
    public void Hash_rejects_empty_passwords()
    {
        var hasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(() => hasher.Hash(string.Empty));
    }

    [Theory]
    [InlineData("unsupported", "c2FsdA==", "aGFzaA==")]
    [InlineData("PBKDF2-SHA256", "not-base64", "aGFzaA==")]
    [InlineData("PBKDF2-SHA256", "c2FsdA==", "not-base64")]
    public void Verify_rejects_unsupported_or_malformed_records(
        string algorithm,
        string saltBase64,
        string hashBase64)
    {
        var hasher = new PasswordHasher();
        var record = new PasswordHashRecord(algorithm, 210_000, saltBase64, hashBase64);

        Assert.False(hasher.Verify("password", record));
    }
}
