using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Security;

public sealed class BcryptPasswordHasherTests
{
    private const string ValidPassword = "ValidPassword1!";

    [Fact]
    public void Hash_ReturnsHash_ForValidPassword()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash(ValidPassword);

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Hash_DoesNotReturnPlainTextPassword()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash(ValidPassword);

        Assert.NotEqual(ValidPassword, hash);
    }

    [Fact]
    public void Hash_GeneratesDifferentHashes_ForSamePassword()
    {
        var hasher = CreateHasher();

        var firstHash = hasher.Hash(ValidPassword);
        var secondHash = hasher.Hash(ValidPassword);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Verify_ReturnsSuccess_ForCorrectPassword()
    {
        var hasher = CreateHasher(workFactor: 10);
        var hash = hasher.Hash(ValidPassword);

        var result = hasher.Verify(ValidPassword, hash);

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void Verify_ReturnsFailed_ForIncorrectPassword()
    {
        var hasher = CreateHasher(workFactor: 10);
        var hash = hasher.Hash(ValidPassword);

        var result = hasher.Verify("OtherPassword1!", hash);

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_ReturnsFailed_ForInvalidHash()
    {
        var hasher = CreateHasher();

        var result = hasher.Verify(ValidPassword, "invalid-hash");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Hash_RejectsEmptyPassword()
    {
        var hasher = CreateHasher();

        Assert.Throws<ArgumentException>(() => hasher.Hash(string.Empty));
    }

    [Fact]
    public void Hash_RejectsPasswordLongerThan64Characters()
    {
        var hasher = CreateHasher();
        var password = new string('A', 62) + "a1!";

        Assert.Throws<ArgumentException>(() => hasher.Hash(password));
    }

    [Fact]
    public void Hash_RejectsPasswordLongerThan72Utf8Bytes()
    {
        var hasher = CreateHasher();
        var password = "Aa1!" + new string('á', 35);

        Assert.Throws<ArgumentException>(() => hasher.Hash(password));
    }

    [Fact]
    public void Verify_ReturnsSuccessRehashNeeded_WhenHashWorkFactorIsLower()
    {
        var lowerWorkFactorHasher = CreateHasher(workFactor: 10);
        var currentHasher = CreateHasher(workFactor: 12);
        var hash = lowerWorkFactorHasher.Hash(ValidPassword);

        var result = currentHasher.Verify(ValidPassword, hash);

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Fact]
    public void Constructor_RejectsWorkFactorLowerThan10()
    {
        Assert.Throws<InvalidOperationException>(() => CreateHasher(workFactor: 9));
    }

    [Fact]
    public void Constructor_RejectsWorkFactorGreaterThan16()
    {
        Assert.Throws<InvalidOperationException>(() => CreateHasher(workFactor: 17));
    }

    private static BcryptPasswordHasher CreateHasher(int workFactor = 10)
    {
        return new BcryptPasswordHasher(
            Options.Create(new PasswordHashingOptions
            {
                WorkFactor = workFactor
            }));
    }
}
