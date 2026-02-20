namespace SharedProtocol.Common.Constants;

/// <summary>
/// World-related constants
/// </summary>
public static class WorldConstants
{
    public const int DayTimeTicks = 24000;
    public const int DefaultDayTime = 1000;
    public const int ClearWeatherDurationTicks = 360 * 20; // 7200 ticks
    public const int RainWeatherDurationTicks = 180 * 20; // 3600 ticks
    public const int StormWeatherDurationTicks = 120 * 20; // 2400 ticks
    public const int SnowWeatherDurationTicks = 240 * 20; // 4800 ticks
    public const float WeatherStormProbability = 0.1f;
    public const float WeatherSnowProbability = 0.05f;
}
