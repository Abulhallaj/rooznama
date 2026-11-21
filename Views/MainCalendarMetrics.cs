using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Rooznama.Views;

/// <summary>Where a number sits in a day cell. "Start" is the side the text starts on: the right in Persian, the left in English.</summary>
public enum CellSlot { TopStart, TopCenter, TopEnd, CenterStart, Center, CenterEnd, BottomStart, BottomCenter, BottomEnd }

/// <summary>
/// Sizes, positions and colours of the main calendar's day cells, in one place. They are plain constants on purpose: edit them
/// here by hand and rebuild. The tray flyout (CalendarPanel) does not use any of this.
/// </summary>
internal static class MainCalendarMetrics
{
    // ---- grid ----
    public const int CellHeight = 64;        // height of a day row
    public const int HeaderHeight = 32;      // height of the weekday header row
    public const double HeaderFontSize = 13; // weekday letters
    /// <summary>Distance of a number in a corner or on an edge from the cell border.</summary>
    public const double Edge = 6;

    // ---- Shamsi day (the big one) ----
    public const double ShamsiSize = 18;
    public static readonly Windows.UI.Text.FontWeight ShamsiWeight = FontWeights.SemiBold;
    public const CellSlot ShamsiSlot = CellSlot.Center;
    /// <summary>Pixels the Shamsi number is moved up from the middle (only for the Center slots).</summary>
    public const double ShamsiLift = 6;

    // ---- Gregorian day ----
    public const double GregorianSize = 11;
    public const double GregorianOpacity = 0.65;
    public const CellSlot GregorianSlot = CellSlot.BottomStart;

    // ---- Hijri day ----
    public const double HijriSize = 11;
    public const double HijriOpacity = 0.65;
    public const CellSlot HijriSlot = CellSlot.BottomEnd;

    // ---- holidays ----
    /// <summary>Background of a holiday tile: a light red that is readable on the light and the dark theme.</summary>
    public static readonly Color HolidayBackground = Color.FromArgb(0x2E, 0xE5, 0x39, 0x35);
    /// <summary>Give Fridays the holiday background too (they already have red text).</summary>
    public const bool FridayBackground = false;

    /// <summary>Puts a number in its slot: alignment plus the edge margin.</summary>
    public static void Place(FrameworkElement e, CellSlot slot, double lift = 0)
    {
        int row = (int)slot / 3, col = (int)slot % 3;   // rows: top, middle, bottom · columns: start, middle, end
        e.VerticalAlignment = row == 0 ? VerticalAlignment.Top : row == 1 ? VerticalAlignment.Center : VerticalAlignment.Bottom;
        e.HorizontalAlignment = col == 0 ? HorizontalAlignment.Left : col == 1 ? HorizontalAlignment.Center : HorizontalAlignment.Right;
        e.Margin = new Thickness(col == 0 ? Edge : 0, row == 0 ? Edge : 0, col == 2 ? Edge : 0,
                                 (row == 2 ? Edge : 0) + (row == 1 ? lift * 2 : 0));   // a bottom margin of 2x moves a centred item up by x
    }
}
