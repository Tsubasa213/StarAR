using UnityEngine;
using System;

public class CelestialSphereController : MonoBehaviour
{
    [SerializeField]
    Transform celestialSphere;

    void Update()
    {
        if (!LocationProvider.Instance.IsReady)
            return;

        float latitude =
            (float)LocationProvider.Instance.Latitude;

        float longitude =
            (float)LocationProvider.Instance.Longitude;

        double lst =
            SiderealTime.GetLocalSiderealTime(
                DateTime.UtcNow,
                longitude);

        Quaternion latitudeRotation =
            Quaternion.Euler(
                latitude - 90f,
                0,
                0);

        Quaternion siderealRotation =
            Quaternion.Euler(
                0,
                (float)-lst,
                0);

        celestialSphere.localRotation =
            siderealRotation *
            latitudeRotation;
    }
}