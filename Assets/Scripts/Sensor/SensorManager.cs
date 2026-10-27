
using System.Collections;
using UnityEngine;

/// <summary>
/// GPSとコンパスの取得を一元管理する。
/// 他のスクリプトからInput.locationやInput.compassを
/// 直接初期化しないこと。
/// </summary>
public class SensorManager : MonoBehaviour
{
    public static SensorManager Instance { get; private set; }

    [Header("GPS Settings")]
    [SerializeField] private float desiredAccuracyMeters = 10f;
    [SerializeField] private float updateDistanceMeters = 1f;

    public bool IsLocationReady { get; private set; }
    public bool IsHeadingReady { get; private set; }

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public float Heading { get; private set; }
    public float HeadingAccuracy { get; private set; }

    private bool locationStarted;
    private Coroutine startupCoroutine;
    private float nextLogTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "SensorManagerが複数存在します。重複した方を削除してください。");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        startupCoroutine = StartCoroutine(InitializeSensors());
    }

    private IEnumerator InitializeSensors()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError(
                "位置情報が無効です。端末の位置情報設定を確認してください。");
            yield break;
        }

        Input.location.Start(
            desiredAccuracyMeters,
            updateDistanceMeters);

        locationStarted = true;

        Input.compass.enabled = true;

        float timeout = 20f;

        while (Input.location.status == LocationServiceStatus.Initializing
               && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogError(
                $"GPSを開始できません。status={Input.location.status}");
            yield break;
        }

        IsLocationReady = true;
        UpdateLocation();

        Debug.Log(
            $"[SensorManager] GPS開始：緯度={Latitude:F6}，経度={Longitude:F6}");

        // コンパス値が有効になるまで最大10秒待つ。
        timeout = 10f;

        while (timeout > 0f && !IsHeadingReady)
        {
            UpdateSensors();
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsHeadingReady)
        {
            Debug.LogWarning(
                "[SensorManager] コンパス値をまだ取得できません。");
        }
        else
        {
            Debug.Log(
                $"[SensorManager] コンパス開始：heading={Heading:F2}°");
        }
    }

    private void Update()
    {
        UpdateSensors();

        if (Time.unscaledTime >= nextLogTime)
        {
            nextLogTime = Time.unscaledTime + 10f;

            if (IsHeadingReady)
            {
                Debug.Log(
                    $"[SensorManager] heading={Heading:F2}°，" +
                    $"accuracy={HeadingAccuracy:F1}°，" +
                    $"GPS={IsLocationReady}");
            }
        }
    }

    private void UpdateSensors()
    {
        if (IsLocationReady &&
            Input.location.status == LocationServiceStatus.Running)
        {
            UpdateLocation();
        }

        if (!Input.compass.enabled)
            return;

        float heading = Input.compass.trueHeading;

        if (heading >= 0f && heading <= 360f)
        {
            Heading = heading;
            HeadingAccuracy = Input.compass.headingAccuracy;
            IsHeadingReady = true;
        }
        else
        {
            IsHeadingReady = false;
        }
    }

    private void UpdateLocation()
    {
        LocationInfo data = Input.location.lastData;
        Latitude = data.latitude;
        Longitude = data.longitude;
    }

    public void RefreshSensors()
    {
        IsHeadingReady = false;

        if (Input.compass.enabled)
        {
            Input.compass.enabled = false;
            Input.compass.enabled = true;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            if (startupCoroutine != null)
                StopCoroutine(startupCoroutine);

            IsLocationReady = false;
            startupCoroutine = StartCoroutine(InitializeSensors());
        }

        Debug.Log("[SensorManager] センサーを再確認しました。");
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (Input.compass.enabled)
            Input.compass.enabled = false;

        if (locationStarted)
            Input.location.Stop();

        Instance = null;
    }
}