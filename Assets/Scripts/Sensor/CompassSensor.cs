
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

/// <summary>
/// 端末のコンパスから真北方向を取得する。
/// SkyRoot自体を回転させず、現在の方位だけを提供する。
/// </summary>
public class CompassSensor : MonoBehaviour
{
    public static CompassSensor Instance { get; private set; }

    [Header("診断ログ設定")]
    [SerializeField] private float logInterval = 10f;

    /// <summary>
    /// 真北からの方位角。
    /// 0 = 北、90 = 東、180 = 南、270 = 西
    /// </summary>
    public float Heading { get; private set; }

    /// <summary>
    /// コンパスから有効な方位を取得できているか。
    /// </summary>
    public bool IsAvailable { get; private set; }

    private float nextLogTime;
    private bool locationStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(EnableSensors());
    }

    private IEnumerator EnableSensors()
    {
        // コンパスを有効化する
        Input.compass.enabled = true;

        // trueHeadingの取得に必要な位置情報を開始する
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning(
                "CompassSensor: 端末の位置情報サービスが無効です。");
            yield break;
        }

        Input.location.Start(10f, 1f);
        locationStarted = true;

        // 位置情報サービスの初期化を待つ
        float timeout = 20f;
        float elapsed = 0f;

        while (Input.location.status == LocationServiceStatus.Initializing
               && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogWarning(
                $"CompassSensor: 位置情報を開始できません。" +
                $" status={Input.location.status}");
            yield break;
        }

        Debug.Log("CompassSensor: compass and location enabled.");
    }

    private void Update()
    {
        UpdateHeading();
        WriteDiagnosticLog();
    }

    private void UpdateHeading()
    {
        if (!Input.compass.enabled)
        {
            IsAvailable = false;
            return;
        }

        // trueHeadingは真北基準。未取得の場合は負の値になる
        float trueHeading = Input.compass.trueHeading;

        if (trueHeading >= 0f && trueHeading <= 360f)
        {
            Heading = trueHeading;
            IsAvailable = true;
        }
        else
        {
            IsAvailable = false;
        }
    }

    private void WriteDiagnosticLog()
    {
        if (Time.unscaledTime < nextLogTime)
            return;

        nextLogTime =
            Time.unscaledTime + Mathf.Max(0.1f, logInterval);

        string locationStatus = locationStarted
            ? Input.location.status.ToString()
            : "NotStarted";

        Debug.Log(
            $"[CompassSensor] " +
            $"trueHeading={Input.compass.trueHeading:F2}°, " +
            $"magneticHeading={Input.compass.magneticHeading:F2}°, " +
            $"headingAccuracy={Input.compass.headingAccuracy:F2}°, " +
            $"Heading={Heading:F2}°, " +
            $"IsAvailable={IsAvailable}, " +
            $"locationStatus={locationStatus}");
    }

    /// <summary>
    /// コンパスを再有効化する。
    /// </summary>
    public void Refresh()
    {
        Input.compass.enabled = false;
        Input.compass.enabled = true;

        IsAvailable = false;
        nextLogTime = 0f;

        Debug.Log("CompassSensor: compass refreshed.");
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;

        // 位置情報サービスをこのクラスが開始していた場合に停止
        if (locationStarted &&
            Input.location.status != LocationServiceStatus.Stopped)
        {
            Input.location.Stop();
        }
    }
}