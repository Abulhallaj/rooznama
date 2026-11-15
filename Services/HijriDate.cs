namespace Rooznama.Services;

/// <summary>A Hijri (qamari) date. Month 1 = Muharram … 12 = Dhu al-Hijjah.</summary>
public readonly record struct HijriDate(int Year, int Month, int Day);
