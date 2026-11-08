using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class GoogleEventMapperTests
{
    static PersonalEvent AllDay(string title, DateTime day) =>
        new() { Title = title, IsAllDay = true, Start = day.Date, End = day.Date.AddDays(1), TimeZone = "Asia/Tehran" };

    static PersonalEvent Timed(DateTime start, DateTime end) =>
        new() { Title = "جلسه", IsAllDay = false, Start = start, End = end, TimeZone = "Asia/Tehran" };

    // ---------- to Google ----------
    [Fact]
    public void An_all_day_event_uses_dates_with_an_exclusive_end()
    {
        var g = GoogleEventMapper.ToGoogle(AllDay("تولد", new DateTime(2026, 11, 5)));
        Assert.Equal("2026-11-05", g.Start!.Date);
        Assert.Equal("2026-11-06", g.End!.Date);
        Assert.Null(g.Start.DateTime);
        Assert.Equal("تولد", g.Summary);
    }

    [Fact]
    public void A_timed_event_sends_wall_clock_time_and_the_IANA_zone()
    {
        var g = GoogleEventMapper.ToGoogle(Timed(new DateTime(2026, 11, 5, 9, 30, 0), new DateTime(2026, 11, 5, 10, 45, 0)));
        Assert.Equal("2026-11-05T09:30:00", g.Start!.DateTime);
        Assert.Equal("2026-11-05T10:45:00", g.End!.DateTime);
        Assert.Equal("Asia/Tehran", g.Start.TimeZone);
        Assert.Null(g.Start.Date);
    }

    [Fact]
    public void An_end_that_is_not_after_the_start_is_corrected()
    {
        var timed = GoogleEventMapper.ToGoogle(Timed(new DateTime(2026, 11, 5, 9, 0, 0), new DateTime(2026, 11, 5, 9, 0, 0)));
        Assert.Equal("2026-11-05T10:00:00", timed.End!.DateTime);

        var day = AllDay("الف", new DateTime(2026, 11, 5));
        day.End = day.Start;
        Assert.Equal("2026-11-06", GoogleEventMapper.ToGoogle(day).End!.Date);
    }

    [Fact]
    public void Text_fields_are_never_null_so_a_patch_can_clear_them()
    {
        var g = GoogleEventMapper.ToGoogle(AllDay("الف", new DateTime(2026, 11, 5)));
        Assert.Equal("", g.Description);
        Assert.Equal("", g.Location);
    }

    [Fact]
    public void Every_local_event_carries_its_id_so_it_can_be_recognised_later()
    {
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        var g = GoogleEventMapper.ToGoogle(e);
        Assert.Equal(e.Id, g.RooznamaId);
    }

    [Fact]
    public void A_Gregorian_yearly_event_keeps_an_RRULE_and_other_events_clear_theirs()
    {
        var yearly = AllDay("الف", new DateTime(2026, 3, 8));
        yearly.Recurrence = EventRecurrence.YearlyGregorian;
        Assert.Equal(new[] { "RRULE:FREQ=YEARLY" }, GoogleEventMapper.ToGoogle(yearly).Recurrence);

        Assert.Empty(GoogleEventMapper.ToGoogle(AllDay("ب", new DateTime(2026, 3, 8))).Recurrence!);   // [] removes an earlier rule
    }

    [Fact]
    public void A_Jalali_occurrence_is_a_single_event_marked_with_its_day()
    {
        var e = AllDay("نوروز من", new DateTime(2026, 3, 21));
        e.Recurrence = EventRecurrence.YearlyJalali;
        var instance = new PersonalEventInstance(e, new DateTime(2027, 3, 21), new DateTime(2027, 3, 22));

        var g = GoogleEventMapper.ToGoogle(e, instance);
        Assert.Equal("2027-03-21", g.Start!.Date);
        Assert.Null(g.Recurrence);
        Assert.Equal("20270321", g.ExtendedProperties!.Private![GoogleEventMapper.InstanceKey]);
        Assert.Equal(e.Id, g.RooznamaId);
    }

    [Fact]
    public void An_imported_event_gets_neither_a_marker_nor_a_repetition()
    {
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        e.Origin = "google";
        e.ExternalId = "abc";
        var g = GoogleEventMapper.ToGoogle(e);
        Assert.Null(g.ExtendedProperties);
        Assert.Null(g.Recurrence);
    }

    [Fact]
    public void Zone_names_are_converted_and_unknown_ones_fall_back_to_exact_instants()
    {
        Assert.Equal("Asia/Tehran", GoogleEventMapper.ToIana("Asia/Tehran"));
        Assert.Equal("UTC", GoogleEventMapper.ToIana("UTC"));
        Assert.Null(GoogleEventMapper.ToIana(""));
        Assert.Equal("Asia/Tehran", GoogleEventMapper.ToIana("Iran Standard Time"));

        var odd = Timed(new DateTime(2026, 11, 5, 9, 0, 0), new DateTime(2026, 11, 5, 10, 0, 0));
        odd.TimeZone = "No Such Zone";
        var g = GoogleEventMapper.ToGoogle(odd);
        Assert.Null(g.Start!.TimeZone);
        Assert.Matches(@"^2026-11-05T09:00:00[+-]\d\d:\d\d$", g.Start.DateTime!);
    }

    // ---------- from Google ----------
    [Fact]
    public void An_all_day_event_from_Google_becomes_an_all_day_event()
    {
        var g = new GoogleEvent
        {
            Id = "g1", ETag = "\"e1\"", Summary = "تعطیل", Updated = "2026-11-01T10:00:00.000Z",
            Start = new GoogleEventTime { Date = "2026-11-10" }, End = new GoogleEventTime { Date = "2026-11-12" }
        };
        var e = GoogleEventMapper.FromGoogle(g, TimeZoneInfo.Utc)!;

        Assert.True(e.IsAllDay);
        Assert.Equal(new DateTime(2026, 11, 10), e.Start);
        Assert.Equal(new DateTime(2026, 11, 12), e.End);
        Assert.Equal("google", e.Origin);
        Assert.Equal("g1", e.ExternalId);
        Assert.Equal("\"e1\"", e.ETag);
        Assert.Equal(EventSyncState.Synced, e.SyncState);
        Assert.Equal(new DateTime(2026, 11, 1, 10, 0, 0), e.UpdatedUtc);
        Assert.Equal(EventRecurrence.None, e.Recurrence);
    }

    [Fact]
    public void A_timed_event_with_an_offset_is_converted_to_the_target_zone()
    {
        var g = new GoogleEvent
        {
            Id = "g2", Summary = "جلسه",
            Start = new GoogleEventTime { DateTime = "2026-11-05T09:00:00+03:30" },
            End = new GoogleEventTime { DateTime = "2026-11-05T10:00:00+03:30" }
        };
        var e = GoogleEventMapper.FromGoogle(g, TimeZoneInfo.Utc)!;
        Assert.False(e.IsAllDay);
        Assert.Equal(new DateTime(2026, 11, 5, 5, 30, 0), e.Start);   // 09:00 at +03:30 is 05:30 UTC
        Assert.Equal(new DateTime(2026, 11, 5, 6, 30, 0), e.End);
        Assert.Equal(DateTimeKind.Unspecified, e.Start.Kind);
    }

    [Fact]
    public void A_bare_wall_clock_time_is_read_in_the_events_own_zone()
    {
        var g = new GoogleEvent
        {
            Id = "g3", Summary = "x",
            Start = new GoogleEventTime { DateTime = "2026-11-05T09:00:00", TimeZone = "Asia/Tehran" },
            End = new GoogleEventTime { DateTime = "2026-11-05T10:00:00", TimeZone = "Asia/Tehran" }
        };
        var e = GoogleEventMapper.FromGoogle(g, TimeZoneInfo.Utc)!;
        Assert.Equal(new DateTime(2026, 11, 5, 5, 30, 0), e.Start);   // Tehran is UTC+03:30 in November (no daylight saving since 2022)
    }

    [Fact]
    public void Missing_pieces_get_sensible_defaults_and_unusable_events_are_refused()
    {
        var noTitle = GoogleEventMapper.FromGoogle(new GoogleEvent { Id = "a", Start = new GoogleEventTime { Date = "2026-11-10" } }, TimeZoneInfo.Utc, "(بدون عنوان)")!;
        Assert.Equal("(بدون عنوان)", noTitle.Title);
        Assert.Equal(new DateTime(2026, 11, 11), noTitle.End);

        Assert.Null(GoogleEventMapper.FromGoogle(new GoogleEvent { Id = "b" }));                                              // no start
        Assert.Null(GoogleEventMapper.FromGoogle(new GoogleEvent { Start = new GoogleEventTime { Date = "2026-11-10" } }));   // no id
        Assert.Null(GoogleEventMapper.FromGoogle(new GoogleEvent { Id = "c", Start = new GoogleEventTime { Date = "garbage" } }));
    }

    [Fact]
    public void Copying_a_remote_edit_changes_the_content_and_nothing_else()
    {
        var local = AllDay("قدیمی", new DateTime(2026, 11, 5));
        local.ExternalId = "g1";
        var id = local.Id;
        var remote = AllDay("جدید", new DateTime(2026, 11, 7));
        remote.ETag = "\"e2\"";

        GoogleEventMapper.CopyContent(remote, local);
        Assert.Equal("جدید", local.Title);
        Assert.Equal(new DateTime(2026, 11, 7), local.Start);
        Assert.Equal("\"e2\"", local.ETag);
        Assert.Equal(id, local.Id);
        Assert.Equal("g1", local.ExternalId);
    }
}
