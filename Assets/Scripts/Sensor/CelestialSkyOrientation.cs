using System;
using UnityEngine;

public class CelestialSkyOrientation : MonoBehaviour
{
    [SerializeField] private Transform celestialSphere;

    private double latitude;
    private double longitude;
    private Vector3 worldNorth;
    private Vector3 worldUp = Vector3.up;
    private bool locationReady;
    private bool calibrationReady;

    public void SetLocation(double lat, double lon)
    {
        latitude = lat;
        longitude = lon;
        locationReady = true;
    }

    public void CalibrateNorth(
        float trueHeadingDegrees,
        Transform cameraTransform)
    {
        if (celestialSphere == null)
            celestialSphere = transform;

        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                cameraTransform.forward,
                Vector3.up);

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning(
                "スマートフォンを水平に近い状態にしてください。");
            return;
        }

        cameraForward.Normalize();

        float h = trueHeadingDegrees * Mathf.Deg2Rad;

        Vector3 cameraRight =
            Vector3.Cross(Vector3.up, cameraForward).normalized;

        // Camera.forward が現在の heading を向いているので、
        // そこから真北方向を復元する。
        worldNorth =
            cameraForward * Mathf.Cos(h) -
            cameraRight * Mathf.Sin(h);

        worldNorth =
            Vector3.ProjectOnPlane(
                worldNorth,
                Vector3.up).normalized;

        calibrationReady = true;

        ApplyAstronomicalOrientation(DateTime.UtcNow);
    }

    public void ApplyAstronomicalOrientation(DateTime utc)
    {
        if (!locationReady ||
            !calibrationReady ||
            celestialSphere == null)
            return;

        double lst =
            CalculateLST(utc, longitude);

        double lstRad =
            lst * Mathf.Deg2Rad;

        double latRad =
            latitude * Mathf.Deg2Rad;

        Vector3 north = worldNorth;
        Vector3 up = Vector3.up;
        Vector3 east =
            Vector3.Cross(up, north).normalized;

        // RA=0h, Dec=0 の方向
        Vector3 ra0 =
            east * (float)Math.Sin(lstRad) +
            north * (float)Math.Cos(lstRad);

        // 天の北極
        Vector3 celestialPole =
            north * (float)Math.Sin(latRad) +
            up * (float)Math.Cos(latRad);

        Vector3 ra6 =
            Vector3.Cross(
                celestialPole,
                ra0).normalized;

        // StarGeneratorの座標系:
        // X = cos(dec) cos(ra)
        // Y = sin(dec)
        // Z = cos(dec) sin(ra)
        celestialSphere.rotation =
            Quaternion.LookRotation(
                ra6,
                celestialPole);
    }

    public static double CalculateLST(
        DateTime utc,
        double longitude)
    {
        DateTime epoch =
            new DateTime(
                1970, 1, 1, 0, 0, 0,
                DateTimeKind.Utc);

        double jd =
            2440587.5 +
            (utc - epoch).TotalSeconds /
            86400.0;

        double d =
            jd - 2451545.0;

        double gmst =
            280.46061837 +
            360.98564736629 * d +
            0.000387933 *
            Math.Pow(d / 36525.0, 2) -
            Math.Pow(d / 36525.0, 3) /
            38710000.0;

        double lst =
            gmst + longitude;

        lst %= 360.0;

        if (lst < 0.0)
            lst += 360.0;

        return lst;
    }
}
