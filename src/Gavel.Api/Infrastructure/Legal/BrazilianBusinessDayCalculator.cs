using System.Collections.Concurrent;
using Gavel.Core.Domain.Services;

namespace Gavel.Api.Infrastructure.Legal;

/// <summary>
/// Implements Brazilian business day logic, considering weekends, fixed national holidays,
/// and moving religious holidays. Optimized with a thread-safe cache for moving holidays.
/// </summary>
public class BrazilianBusinessDayCalculator : IBusinessDayCalculator
{
    private static readonly ConcurrentDictionary<int, HashSet<(int Month, int Day)>> MovingHolidaysCache = new();

    private static readonly HashSet<(int Month, int Day)> FixedHolidays =
    [
        (1, 1),   // New Year
        (4, 21),  // Tiradentes
        (5, 1),   // Labor Day
        (9, 7),   // Independence Day
        (10, 12), // Our Lady of Aparecida
        (11, 2),  // All Souls' Day
        (11, 15), // Proclamation of the Republic
        (11, 20), // Zumbi dos Palmares Day
        (12, 25)  // Christmas
    ];

    public DateTimeOffset AddBusinessDays(DateTimeOffset start, int businessDays)
    {
        if (businessDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(businessDays), "Business days to add cannot be negative.");
        }

        var current = start;
        var addedDays = 0;

        while (addedDays < businessDays)
        {
            current = current.AddDays(1);
            if (IsBusinessDay(current))
            {
                addedDays++;
            }
        }

        return current;
    }

    private bool IsBusinessDay(DateTimeOffset date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        if (FixedHolidays.Contains((date.Month, date.Day)))
        {
            return false;
        }

        // Optimized Cache Resolution: Calculate moving holidays exactly once per year
        var movingHolidays = MovingHolidaysCache.GetOrAdd(date.Year, CalculateMovingHolidays);
        
        if (movingHolidays.Contains((date.Month, date.Day)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates Easter and derived moving holidays (Carnaval, Good Friday, Corpus Christi)
    /// using the Butcher-Meeus algorithm.
    /// </summary>
    private static HashSet<(int Month, int Day)> CalculateMovingHolidays(int year)
    {
        // Butcher-Meeus Algorithm for Easter Sunday
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        var easter = new DateTime(year, month, day);

        return
        [
            (easter.AddDays(-48).Month, easter.AddDays(-48).Day), // Carnaval (Monday)
            (easter.AddDays(-47).Month, easter.AddDays(-47).Day), // Carnaval (Tuesday)
            (easter.AddDays(-2).Month, easter.AddDays(-2).Day),   // Good Friday
            (easter.AddDays(60).Month, easter.AddDays(60).Day)    // Corpus Christi
        ];
    }
}
