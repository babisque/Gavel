using Gavel.Core.Domain.Registration;
using Gavel.Api.Features.Registration.Services;
using TUnit.Assertions.Extensions;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Registration;

public class BrazilianTaxIdValidatorTests
{
    [Test]
    [Arguments("12345678900")] // Invalid CPF checksum
    [Arguments("11111111111")] // Sequential CPF
    [Arguments("12345678000199")] // Invalid CNPJ checksum
    public async Task Validate_WhenTaxIdIsInvalid_ShouldReturnFailure(string invalidTaxId)
    {
        // Arrange
        var validator = new BrazilianTaxIdValidator();

        // Act
        var result = validator.Validate(invalidTaxId);

        // Assert
        await That(result.IsValid).IsFalse();
        await That(result.ErrorMessage).IsNotNull();
    }

    [Test]
    [Arguments("000.000.000-00")] // Malformed
    [Arguments("abc")] // Not numeric
    public async Task Validate_WhenFormatIsIncorrect_ShouldReturnFailure(string malformedId)
    {
        // Arrange
        var validator = new BrazilianTaxIdValidator();

        // Act
        var result = validator.Validate(malformedId);

        // Assert
        await That(result.IsValid).IsFalse();
    }
}
