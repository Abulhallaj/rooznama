using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class WeekNumbersTests
{
    [Fact]
    public void The_week_that_contains_1_Farvardin_is_week_1()
    {
        // 1 Farvardin 1405 = Saturday 21 March 2026
        Assert.Equal(1, WeekNumbers.Iranian(new DateTime(2026, 3, 21)));
        Assert.Equal(1, WeekNumbers.Iranian(new DateTime(2026, 3, 27)));   // Friday of that week
        Assert.Equal(2, WeekNumbers.Iranian(new DateTime(2026, 3, 28)));
    }

    [Fact]
    public void A_week_that_spans_two_years_is_week_1_of_the_new_year()
    {
        // 1 Farvardin 1406 = Sunday 21 March 2027; its week starts on Saturday 20 March (29 Esfand 1405)
        Assert.Equal(52, WeekNumbers.Iranian(new DateTime(2027, 3, 13)));
        Assert.Equal(1, WeekNumbers.Iranian(new DateTime(2027, 3, 20)));
        Assert.Equal(1, WeekNumbers.Iranian(new DateTime(2027, 3, 26)));
        Assert.Equal(2, WeekNumbers.Iranian(new DateTime(2027, 3, 27)));
    }

    [Fact]
    public void Every_day_of_a_Saturday_to_Friday_week_has_the_same_number_and_weeks_count_up_by_one()
    {
        var saturday = new DateTime(2026, 3, 21);
        for (int w = 0; w < 52; w++)
        {
            var start = saturday.AddDays(7 * w);
            Assert.Equal(DayOfWeek.Saturday, start.DayOfWeek);
            for (int d = 0; d < 7; d++)
                Assert.Equal(w + 1, WeekNumbers.Iranian(start.AddDays(d)));
        }
    }

    [Fact]
    public void Of_uses_the_Iranian_rule_for_now_and_out_of_range_dates_give_0()
    {
        var d = new DateTime(2026, 11, 5);
        Assert.Equal(WeekNumbers.Iranian(d), WeekNumbers.Of(d));
        Assert.Equal(WeekNumbers.Iranian(d), WeekNumbers.Of(d, CalendarKind.Jalali));
        Assert.Equal(0, WeekNumbers.Iranian(DateTime.MinValue));
        Assert.Equal(0, WeekNumbers.Iranian(DateTime.MaxValue));
    }
}
