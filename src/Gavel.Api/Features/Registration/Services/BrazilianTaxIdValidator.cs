namespace Gavel.Api.Features.Registration.Services;

public record ValidationResult(bool IsValid, string? ErrorMessage = null);

public interface ITaxIdValidator
{
    ValidationResult Validate(string taxId);
}

public sealed class BrazilianTaxIdValidator : ITaxIdValidator
{
    public ValidationResult Validate(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return new ValidationResult(false, "Tax ID cannot be empty.");

        // Clean non-digits
        var digitsOnly = new string(taxId.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 11)
            return ValidateCpf(digitsOnly);
        
        if (digitsOnly.Length == 14)
            return ValidateCnpj(digitsOnly);

        return new ValidationResult(false, "Invalid Tax ID length.");
    }

    private ValidationResult ValidateCpf(string cpf)
    {
        // Zero-allocation sequential check
        if (cpf == new string(cpf[0], cpf.Length))
            return new ValidationResult(false, "Sequential CPF is invalid.");

        int[] multipliers1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] multipliers2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (cpf[i] - '0') * multipliers1[i]; // ASCII subtraction trick

        int remainder = sum % 11;
        int d1 = remainder < 2 ? 0 : 11 - remainder;

        sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (cpf[i] - '0') * multipliers2[i];
        sum += d1 * multipliers2[9];

        remainder = sum % 11;
        int d2 = remainder < 2 ? 0 : 11 - remainder;

        bool isValid = cpf[9] - '0' == d1 && cpf[10] - '0' == d2;
        return isValid ? new ValidationResult(true) : new ValidationResult(false, "Invalid CPF checksum.");
    }

    private ValidationResult ValidateCnpj(string cnpj)
    {
        // Zero-allocation sequential check
        if (cnpj == new string(cnpj[0], cnpj.Length))
            return new ValidationResult(false, "Sequential CNPJ is invalid.");

        int[] multipliers1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] multipliers2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += (cnpj[i] - '0') * multipliers1[i];

        int remainder = sum % 11;
        int d1 = remainder < 2 ? 0 : 11 - remainder;

        sum = 0;
        for (int i = 0; i < 12; i++)
            sum += (cnpj[i] - '0') * multipliers2[i];
        sum += d1 * multipliers2[12];

        remainder = sum % 11;
        int d2 = remainder < 2 ? 0 : 11 - remainder;

        bool isValid = cnpj[12] - '0' == d1 && cnpj[13] - '0' == d2;
        return isValid ? new ValidationResult(true) : new ValidationResult(false, "Invalid CNPJ checksum.");
    }
}
