using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SkyCalibrationController : MonoBehaviour
{
    [SerializeField] private Transform celestialSphere;
    [SerializeField] private Transform arCamera;
    [SerializeField] private double defaultLatitude = 34.6937;
    [SerializeField] private double defaultLongitude = 135.5023;
    [SerializeField] private bool useGps = true;
    [SerializeField] private float calibrateDelay = 1.5f;

    public bool IsCalibrated { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    private void Awake()
    {
        if (celestialSphere == null)
            celestialSphere = transform;

        if (arCamera == null && Camera.main != null)
            arCamera = Camera.main.transform;

        Latitude = defaultLatitude;
        Longitude = defaultLongitude;

        Input.compass.enabled = true;
    }

    private void Start()
    {
        if (useGps)
            StartCoroutine(StartGps());

        Invoke(nameof(Calibrate), calibrateDelay);
    }

    private IEnumerator StartGps()
    {
        if (!Input.location.isEnabledByUser)
            yield break;

        Input.location.Start(1f, 1f);

        float timeout = 10f;
        while (Input.location.status == LocationServiceStatus.Initializing &&
               timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            Latitude = Input.location.lastData.latitude;
            Longitude = Input.location.lastData.longitude;

            Debug.Log(
                $"GPS: lat={Latitude:F6}, lon={Longitude:F6}");
        }
    }

    public void Calibrate()
    {
        if (arCamera == null)
        {
            Debug.LogError("SkyCalibrationController: AR Cameraがありません。");
            return;
        }

        float heading = Input.compass.trueHeading;

        if (heading < 0f || heading > 360f)
        {
            Debug.LogWarning("コンパス値がまだ取得できません。");
            return;
        }

        CelestialSkyOrientation orientation =
            GetComponent<CelestialSkyOrientation>();

        if (orientation == null)
            orientation = gameObject.AddComponent<CelestialSkyOrientation>();

        orientation.SetLocation(Latitude, Longitude);
        orientation.CalibrateNorth(heading, arCamera);

        IsCalibrated = true;

        Debug.Log(
            $"Sky calibrated: heading={heading:F2}, " +
            $"lat={Latitude:F6}, lon={Longitude:F6}");
    }

    public void Recalibrate()
    {
        IsCalibrated = false;
        Calibrate();
    }
}
