using System;
using UnityEngine;

/// <summary>
/// Converts the observer location and sidereal time into the sky rotation.
/// This is the single rotation writer for the celestial sphere.
/// </summary>
public class CelestialSphereController : MonoBehaviour
{
    [Header("Rotation Target")]
    [SerializeField]
    private Transform celestialSphere;

    [Tooltip("Small fixed correction for matching the catalog axes to the Milky Way artwork.")]
    [SerializeField]
    private Vector3 alignmentEulerAngles = Vector3.zero;

    [Tooltip("1 is real time. 600 is a debug setting that advances sidereal time by about 10 minutes per real second.")]
    [Min(0f)]
    [SerializeField]
    private float siderealTimeScale = 1f;

    [Tooltip("Reverse the sidereal rotation direction if the sky moves opposite to the intended direction.")]
    [SerializeField]
    private bool reverseSiderealDirection = false;

    [Header("Astronomy Debug")]
    [Tooltip("Write the active location, UTC, and local sidereal time to the Unity log periodically.")]
    [SerializeField]
    private bool logAstronomyState = true;

    [Min(1f)]
    [SerializeField]
    private float astronomyLogIntervalSeconds = 5f;

    private bool isInitialized;
    private double previousSiderealTime;
    private double simulatedSiderealTime;
    private Quaternion alignmentRotation;
    private float nextAstronomyLogTime;

    public DateTime CurrentUtc { get; private set; }

    public double CurrentLocalSiderealTimeDegrees { get; private set; }

    public void ResetToCurrentAstronomy()
    {
        isInitialized = false;
        nextAstronomyLogTime = 0f;
        Debug.Log(
            "CelestialSphereController: current UTC and location will be " +
            "applied on the next frame.");
    }

    private void Awake()
    {
        if (celestialSphere == null)
        {
            celestialSphere = transform;
        }

        alignmentRotation = Quaternion.Euler(alignmentEulerAngles);
    }

    private void Update()
    {
        LocationProvider location = LocationProvider.Instance;

        if (location == null || !location.IsReady || celestialSphere == null)
        {
            return;
        }

        // Use the actual current UTC time as the source of truth. This keeps
        // the sky correct after pausing, resuming, or changing the device time.
        DateTime currentUtc = DateTime.UtcNow;
        double currentSiderealTime = SiderealTime.GetLocalSiderealTime(
            currentUtc,
            location.Longitude);

        CurrentUtc = currentUtc;
        CurrentLocalSiderealTimeDegrees = currentSiderealTime;

        if (!isInitialized)
        {
            previousSiderealTime = currentSiderealTime;
            simulatedSiderealTime = currentSiderealTime;
            isInitialized = true;
        }
        else if (Mathf.Approximately(siderealTimeScale, 1f))
        {
            // In normal mode, do not accumulate frame deltas. Recalculate
            // from the current clock so the displayed sky cannot drift.
            simulatedSiderealTime = currentSiderealTime;
            previousSiderealTime = currentSiderealTime;
        }
        else
        {
            // Keep the accelerated clock for Editor/device demonstrations.
            double frameDelta = GetSignedAngleDelta(
                currentSiderealTime,
                previousSiderealTime);

            simulatedSiderealTime = NormalizeDegrees(
                simulatedSiderealTime + frameDelta * siderealTimeScale);

            previousSiderealTime = currentSiderealTime;
        }

        if (logAstronomyState &&
            (nextAstronomyLogTime <= 0f ||
             Time.unscaledTime >= nextAstronomyLogTime))
        {
            Debug.Log(
                $"CelestialSphereController: {location.StatusMessage}; " +
                $"lat={location.Latitude:F6}, lon={location.Longitude:F6}, " +
                $"UTC={currentUtc:O}, " +
                $"LST={simulatedSiderealTime:F3} deg");

            nextAstronomyLogTime = Time.unscaledTime +
                Mathf.Max(1f, astronomyLogIntervalSeconds);
        }

        Quaternion latitudeRotation = Quaternion.Euler(
            (float)location.Latitude - 90f,
            0f,
            0f);

        float siderealDirection = reverseSiderealDirection ? 1f : -1f;
        Quaternion siderealRotation = Quaternion.Euler(
            0f,
            (float)simulatedSiderealTime * siderealDirection,
            0f);

        celestialSphere.localRotation =
            alignmentRotation *
            siderealRotation *
            latitudeRotation;
    }

    private static double GetSignedAngleDelta(
        double current,
        double previous)
    {
        double delta = current - previous;

        if (delta < -180d)
        {
            delta += 360d;
        }
        else if (delta > 180d)
        {
            delta -= 360d;
        }

        return delta;
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360d;

        if (degrees < 0d)
        {
            degrees += 360d;
        }

        return degrees;
    }
}
