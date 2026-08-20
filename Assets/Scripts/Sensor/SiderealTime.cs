using System;
using UnityEngine;

public static class SiderealTime
{
    /// <summary>
    /// 現在のグリニッジ恒星時を時間単位で取得
    /// </summary>
    public static double GetGreenwichSiderealTime(DateTime utc)
    {
        double jd = JulianDate(utc);

        double T = (jd - 2451545.0) / 36525.0;

        double gst =
            280.46061837
            + 360.98564736629 * (jd - 2451545.0)
            + 0.000387933 * T * T
            - T * T * T / 38710000.0;

        gst %= 360.0;

        if (gst < 0)
            gst += 360.0;

        return gst / 15.0;
    }

    /// <summary>
    /// 現在地の地方恒星時を時間単位で取得
    /// </summary>
    public static double GetLocalSiderealTime(
        DateTime utc,
        double longitude
    )
    {
        double gst = GetGreenwichSiderealTime(utc);

        double lst = gst + longitude / 15.0;

        lst %= 24.0;

        if (lst < 0)
            lst += 24.0;

        return lst;
    }

    static double JulianDate(DateTime utc)
    {
        utc = utc.ToUniversalTime();

        int year = utc.Year;
        int month = utc.Month;

        double day =
            utc.Day
            + utc.Hour / 24.0
            + utc.Minute / 1440.0
            + utc.Second / 86400.0
            + utc.Millisecond / 86400000.0;

        if (month <= 2)
        {
            year--;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + A / 4;

        return
            Math.Floor(365.25 * (year + 4716))
            + Math.Floor(30.6001 * (month + 1))
            + day
            + B
            - 1524.5;
    }
}