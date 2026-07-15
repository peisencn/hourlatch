using System.Security.Cryptography;

namespace HourLatch.Core.Security;

public sealed class PasswordHasher
{
    private const string AlgorithmName = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 210_000;
    private readonly int _iterations;

    public PasswordHasher(int iterations = DefaultIterations)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        _iterations = iterations;
    }

    public PasswordHashRecord Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, _iterations, HashSize);

        return new PasswordHashRecord(
            AlgorithmName,
            _iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, PasswordHashRecord record)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(record);

        if (record.Algorithm != AlgorithmName || record.Iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(record.SaltBase64);
            var expectedHash = Convert.FromBase64String(record.HashBase64);
            var actualHash = Derive(password, salt, record.Iterations, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int outputLength)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            outputLength);
    }
}
