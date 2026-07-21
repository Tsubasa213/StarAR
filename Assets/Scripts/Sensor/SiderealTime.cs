using System;

public static class SiderealTime
{
    public static double GetLocalSiderealTime(
        DateTime utc,
        double longitude)
    {
        double jd = GetJulianDay(utc);

        double T =
            (jd - 2451545.0) / 36525.0;

        double gmst =
            280.46061837
            + 360.98564736629 *
            (jd - 2451545.0)
            + 0.000387933 * T * T
            - T * T * T / 38710000.0;

        gmst %= 360.0;

        if (gmst < 0)
            gmst += 360.0;

        double lst =
            gmst + longitude;

        lst %= 360.0;

        if (lst < 0)
            lst += 360.0;

        return lst;
    }

    static double GetJulianDay(DateTime utc)
    {
        utc = utc.ToUniversalTime();

        int Y = utc.Year;
        int M = utc.Month;

        double D =
            utc.Day +
            utc.Hour / 24.0 +
            utc.Minute / 1440.0 +
            utc.Second / 86400.0 +
            utc.Millisecond / 86400000.0;

        if (M <= 2)
        {
            Y--;
            M += 12;
        }

        int A = Y / 100;
        int B = 2 - A + A / 4;

        return
            Math.Floor(365.25 * (Y + 4716))
            + Math.Floor(30.6001 * (M + 1))
            + D + B - 1524.5;
    }
}
