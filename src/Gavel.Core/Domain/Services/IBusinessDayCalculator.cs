namespace Gavel.Core.Domain.Services;

/// <summary>
/// Domain service to handle business day logic according to Brazilian regulations.
/// </summary>
public interface IBusinessDayCalculator
{
    /// <summary>
    /// Calculates a future date based on the number of business days,
    /// skipping weekends and identified public holidays.
    /// </summary>
    DateTimeOffset AddBusinessDays(DateTimeOffset start, int businessDays);
}
