using mmex.net.core.Entities;
using mmex.net.core.Enums;

namespace mmex.net.core.Extensions;

public static class ScheduledExtensions
{
    public static RepeatFrequency GetFrequency(this ScheduledTransaction sched) =>
        (RepeatFrequency)(sched.Repeats % 100);

    public static RepeatExecutionMode GetExecutionMode(this ScheduledTransaction sched) =>
        (RepeatExecutionMode)(sched.Repeats / 100);

    /// <summary>
    /// Calculates the next occurrence date after <paramref name="current"/>.
    /// Returns null when the frequency is Once (no further occurrences).
    /// </summary>
    public static DateOnly? CalculateNextDate(this ScheduledTransaction sched, DateOnly current)
    {
        var freq = sched.GetFrequency();
        var x = sched.NumOccurrences ?? 1;

        return freq switch
        {
            RepeatFrequency.Once => null,
            RepeatFrequency.Daily => current.AddDays(1),
            RepeatFrequency.Weekly => current.AddDays(7),
            RepeatFrequency.BiWeekly => current.AddDays(14),
            RepeatFrequency.FourWeekly => current.AddDays(28),
            RepeatFrequency.Monthly => current.AddMonths(1),
            RepeatFrequency.BiMonthly => current.AddMonths(2),
            RepeatFrequency.Quarterly => current.AddMonths(3),
            RepeatFrequency.FourMonthly => current.AddMonths(4),
            RepeatFrequency.HalfYearly => current.AddMonths(6),
            RepeatFrequency.Yearly => current.AddYears(1),
            RepeatFrequency.InXDays => current.AddDays(x),
            RepeatFrequency.InXMonths => current.AddMonths(x),
            RepeatFrequency.EveryXDays => current.AddDays(x),
            RepeatFrequency.EveryXMonths => current.AddMonths(x),
            RepeatFrequency.MonthlyLastDay => LastDayOfNextMonth(current),
            RepeatFrequency.MonthlyLastBusinessDay => LastBusinessDayOfNextMonth(current),
            _ => current.AddMonths(1)
        };
    }

    private static DateOnly LastDayOfNextMonth(DateOnly d)
    {
        var next = d.AddMonths(1);
        return new DateOnly(next.Year, next.Month, DateTime.DaysInMonth(next.Year, next.Month));
    }

    private static DateOnly LastBusinessDayOfNextMonth(DateOnly d)
    {
        var last = LastDayOfNextMonth(d);
        return last.DayOfWeek switch
        {
            DayOfWeek.Saturday => last.AddDays(-1),
            DayOfWeek.Sunday => last.AddDays(-2),
            _ => last
        };
    }
}
