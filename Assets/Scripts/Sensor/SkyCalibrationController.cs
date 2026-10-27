
using System.Collections;
using UnityEngine;

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

    private bool calibrationInProgress;

    private void Awake()
    {
        if (celestialSphere == null)
            celestialSphere = transform;

        if (arCamera == null && Camera.main != null)
            arCamera = Camera.main.transform;

        Latitude = defaultLatitude;
        Longitude = defaultLongitude;
    }

    private void Start()
    {
        StartCoroutine(WaitForSensorsAndCalibrate());
    }

    private IEnumerator WaitForSensorsAndCalibrate()
    {
        // センサーの初期化完了を待つ。
        float timeout = 30f;

        while (SensorManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        SensorManager sensors = SensorManager.Instance;

        if (sensors == null)
        {
            Debug.LogError(
                "SensorManagerが見つかりません。Hierarchyを確認してください。");
            yield break;
        }

        timeout = 30f;

        while (timeout > 0f &&
               (!sensors.IsHeadingReady ||
                (useGps && !sensors.IsLocationReady)))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (useGps && sensors.IsLocationReady)
        {
            Latitude = sensors.Latitude;
            Longitude = sensors.Longitude;
        }
        else if (useGps)
        {
            Debug.LogWarning(
                "GPSを取得できないため，初期設定の緯度・経度を使用します。");
        }

        if (!sensors.IsHeadingReady)
        {
            Debug.LogError(
                "コンパスの方位が取得できないため，校正を中止しました。");
            yield break;
        }

        yield return new WaitForSecondsRealtime(calibrateDelay);

        Calibrate();
    }

    public void Calibrate()
    {
        if (calibrationInProgress)
            return;

        SensorManager sensors = SensorManager.Instance;

        if (sensors == null || !sensors.IsHeadingReady)
        {
            Debug.LogWarning("有効なコンパス方位を取得できません。");
            return;
        }

        if (arCamera == null)
        {
            Debug.LogError("AR Cameraが設定されていません。");
            return;
        }

        calibrationInProgress = true;

        try
        {
            CelestialSkyOrientation orientation =
                celestialSphere != null
                    ? celestialSphere.GetComponent<CelestialSkyOrientation>()
                    : null;

            if (orientation == null && celestialSphere != null)
            {
                orientation =
                    celestialSphere.gameObject
                        .AddComponent<CelestialSkyOrientation>();
            }

            if (orientation == null)
            {
                Debug.LogError("天球のTransformが設定されていません。");
                return;
            }

            orientation.SetLocation(Latitude, Longitude);
            orientation.CalibrateNorth(sensors.Heading, arCamera);

            IsCalibrated = true;

            Debug.Log(
                $"[SkyCalibrationController] 校正完了：" +
                $"heading={sensors.Heading:F2}°，" +
                $"lat={Latitude:F6}，lon={Longitude:F6}");
        }
        finally
        {
            calibrationInProgress = false;
        }
    }

    public void Recalibrate()
    {
        IsCalibrated = false;

        SensorManager sensors = SensorManager.Instance;

        if (sensors == null)
        {
            Debug.LogWarning("SensorManagerが見つかりません。");
            return;
        }

        sensors.RefreshSensors();

        // センサー更新直後の古い値を使わないようにする。
        StartCoroutine(RecalibrateWhenReady());
    }

    private IEnumerator RecalibrateWhenReady()
    {
        float timeout = 10f;

        while (timeout > 0f &&
               (SensorManager.Instance == null ||
                !SensorManager.Instance.IsHeadingReady))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (SensorManager.Instance != null &&
            SensorManager.Instance.IsHeadingReady)
        {
            Calibrate();
        }
        else
        {
            Debug.LogWarning("再校正用の方位を取得できませんでした。");
        }
    }
}