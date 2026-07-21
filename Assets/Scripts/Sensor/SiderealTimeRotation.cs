using System;
using UnityEngine;

/// <summary>
/// Rotates the whole sky from the sidereal-time difference since startup.
/// GPS and compass alignment are intentionally added as separate layers later.
/// </summary>
public class SiderealTimeRotation : MonoBehaviour
{
    [Header("Sidereal Time")]
    [Tooltip("Longitude is kept as a future GPS hook. It cancels out when only the sidereal-time difference is used.")]
    [SerializeField]
    private float referenceLongitude = 0f;

    [Tooltip("Reverse the visual rotation direction if the sky moves opposite to the intended direction.")]
    [SerializeField]
    private bool reverseDirection = false;

    [Tooltip("1 is real time. 600 advances the simulated sidereal time by about 10 minutes per real second for debugging.")]
    [Min(0f)]
    [SerializeField]
    private float siderealTimeScale = 1f;

    private Quaternion initialLocalRotation;
    private double previousSiderealTime;
    private double accumulatedSiderealDegrees;

    private void Start()
    {
        initialLocalRotation = transform.localRotation;
        previousSiderealTime = GetSiderealTime(DateTime.UtcNow);
        accumulatedSiderealDegrees = 0d;
    }

    private void Update()
    {
        double currentSiderealTime = GetSiderealTime(DateTime.UtcNow);
        double frameDelta = currentSiderealTime - previousSiderealTime;

        // Sidereal time wraps from 360 to 0 at midnight. Correct only that
        // boundary, then accumulate so the rotation remains continuous even
        // when the app stays open for more than twelve sidereal hours.
        if (frameDelta < -180d)
        {
            frameDelta += 360d;
        }
        else if (frameDelta > 180d)
        {
            frameDelta -= 360d;
        }

        accumulatedSiderealDegrees += frameDelta * siderealTimeScale;
        previousSiderealTime = currentSiderealTime;

        float direction = reverseDirection ? 1f : -1f;

        transform.localRotation =
            initialLocalRotation *
            Quaternion.Euler(0f, (float)accumulatedSiderealDegrees * direction, 0f);
    }

    private double GetSiderealTime(DateTime utc)
    {
        return SiderealTime.GetLocalSiderealTime(
            utc,
            referenceLongitude);
    }
}
