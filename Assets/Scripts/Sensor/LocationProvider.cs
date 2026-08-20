using System.Collections;
using UnityEngine;

/// <summary>
/// Android端末から緯度・経度を取得する軽量な位置情報プロバイダ。
/// </summary>
public class LocationProvider : MonoBehaviour
{
    public static LocationProvider Instance { get; private set; }

    [Header("GPS Settings")]
    [SerializeField]
    private float desiredAccuracyMeters = 10f;

    [SerializeField]
    private float updateDistanceMeters = 1f;

    public bool IsReady { get; private set; }

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public string StatusMessage { get; private set; } = "GPS initializing...";

    private Coroutine locationCoroutine;

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
        StartLocation();
    }

    public void RefreshNow()
    {
        IsReady = false;
        StartLocation();
    }

    private void StartLocation()
    {
        if (locationCoroutine != null)
        {
            StopCoroutine(locationCoroutine);
        }

        locationCoroutine = StartCoroutine(StartLocationService());
    }

    private IEnumerator StartLocationService()
    {
        StatusMessage = "Starting GPS...";

        if (!Input.location.isEnabledByUser)
        {
            StatusMessage = "Location service is disabled.";
            Debug.LogWarning("LocationProvider: GPS is disabled.");
            yield break;
        }

        Input.location.Stop();

        Input.location.Start(
            desiredAccuracyMeters,
            updateDistanceMeters
        );

        int maxWait = 20;

        while (
            Input.location.status == LocationServiceStatus.Initializing &&
            maxWait > 0
        )
        {
            yield return new WaitForSecondsRealtime(1f);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            StatusMessage = "GPS initialization timeout.";
            Debug.LogWarning(
                "LocationProvider: GPS initialization timed out."
            );
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            StatusMessage = "GPS failed.";
            Debug.LogWarning(
                "LocationProvider: unable to determine device location."
            );
            yield break;
        }

        UpdateLocation();

        IsReady = true;

        StatusMessage =
            $"GPS ready: lat={Latitude:F6}, lon={Longitude:F6}";

        Debug.Log(
            $"LocationProvider: {StatusMessage}"
        );
    }

    private void Update()
    {
        if (!IsReady)
        {
            return;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            UpdateLocation();
        }
    }

    private void UpdateLocation()
    {
        LocationInfo data = Input.location.lastData;

        Latitude = data.latitude;
        Longitude = data.longitude;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        Input.location.Stop();
    }
}