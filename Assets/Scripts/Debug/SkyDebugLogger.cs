using System;
using UnityEngine;

public class SkyDebugLogger : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform arCamera;
    [SerializeField] private Transform celestialSphere;
    [SerializeField] private CelestialSkyOrientation orientation;

    [Header("ログ設定")]
    [SerializeField] private float logInterval = 10f;
    [SerializeField] private bool logOnStart = true;

    private float elapsed;

    private void Start()
    {
        if (arCamera == null && Camera.main != null)
            arCamera = Camera.main.transform;

        if (orientation == null)
            orientation = FindFirstObjectByType<CelestialSkyOrientation>();

        if (celestialSphere == null && orientation != null)
            celestialSphere = orientation.transform;

        Debug.Log("[SKY-DIAG] 診断ログ開始");

        if (logOnStart)
            WriteLog();
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= Mathf.Max(0.1f, logInterval))
        {
            elapsed = 0f;
            WriteLog();
        }
    }

    public void WriteLog()
    {
        DateTime utc = DateTime.UtcNow;

        // GPS
        string gpsStatus = Input.location.status.ToString();
        string gpsLog;

        if (Input.location.status == LocationServiceStatus.Running)
        {
            LocationInfo loc = Input.location.lastData;

            gpsLog =
                $"GPS status={gpsStatus}, " +
                $"enabledByUser={Input.location.isEnabledByUser}, " +
                $"lat={loc.latitude:F6}, lon={loc.longitude:F6}, " +
                $"alt={loc.altitude:F1}m, " +
                $"horizontalAccuracy={loc.horizontalAccuracy:F1}m, " +
                $"verticalAccuracy={loc.verticalAccuracy:F1}m, " +
                $"locationTimestamp={loc.timestamp:F1}";
        }
        else
        {
            gpsLog =
                $"GPS status={gpsStatus}, " +
                $"enabledByUser={Input.location.isEnabledByUser}";
        }

        // コンパス
        string compassLog =
            $"Compass enabled={Input.compass.enabled}, " +
            $"trueHeading={Input.compass.trueHeading:F2}deg, " +
            $"magneticHeading={Input.compass.magneticHeading:F2}deg, " +
            $"headingAccuracy={Input.compass.headingAccuracy:F2}deg, " +
            $"timestamp={Input.compass.timestamp:F2}";

        // カメラ姿勢
        string cameraLog = "Camera=null";

        if (arCamera != null)
        {
            Vector3 forward = arCamera.forward;

            Vector3 horizontalForward =
                Vector3.ProjectOnPlane(forward, Vector3.up);

            float worldYaw = -1f;

            if (horizontalForward.sqrMagnitude > 0.0001f)
            {
                horizontalForward.Normalize();

                worldYaw = Mathf.Atan2(
                    horizontalForward.x,
                    horizontalForward.z
                ) * Mathf.Rad2Deg;

                if (worldYaw < 0f)
                    worldYaw += 360f;
            }

            Vector3 angles = arCamera.eulerAngles;

            cameraLog =
                $"Camera worldPosition={arCamera.position}, " +
                $"worldEuler={angles}, " +
                $"worldYaw={worldYaw:F2}deg, " +
                $"forward={forward}";
        }

        // 天球
        string sphereLog = "CelestialSphere=null";

        if (celestialSphere != null)
        {
            Vector3 angles = celestialSphere.eulerAngles;

            Vector3 northPoleWorld =
                celestialSphere.TransformDirection(Vector3.up).normalized;

            sphereLog =
                $"Sphere worldPosition={celestialSphere.position}, " +
                $"worldEuler={angles}, " +
                $"worldRotation={celestialSphere.rotation.eulerAngles}, " +
                $"localEuler={celestialSphere.localEulerAngles}, " +
                $"localYaxisWorld={northPoleWorld}";
        }

        // 天文計算
        string astroLog = "Astro location unavailable";

        if (orientation != null &&
            Input.location.status == LocationServiceStatus.Running)
        {
            LocationInfo loc = Input.location.lastData;

            double latitude = loc.latitude;
            double longitude = loc.longitude;

            double lst =
                CelestialSkyOrientation.CalculateLST(
                    utc, longitude);

            astroLog =
                $"LST={lst:F4}deg, " +
                $"latitude={latitude:F6}, " +
                $"longitude={longitude:F6}";
        }

        Debug.Log(
            $"[SKY-DIAG] UTC={utc:yyyy-MM-dd HH:mm:ss.fff}\n" +
            $"{gpsLog}\n" +
            $"{compassLog}\n" +
            $"{cameraLog}\n" +
            $"{sphereLog}\n" +
            $"{astroLog}"
        );
    }
}