using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class LocationProvider : MonoBehaviour
{
    public static LocationProvider Instance { get; private set; }

    [Header("Location Service")]
    [SerializeField]
    private float desiredAccuracyInMeters = 10f;

    [SerializeField]
    private float updateDistanceInMeters = 1f;

    [SerializeField]
    private int startupTimeoutSeconds = 20;

    [Header("Editor Fallback")]
    [Tooltip("Unity Editor has no device GPS, so use this fixed location for testing.")]
    [SerializeField]
    private bool useEditorFallback = true;

    [SerializeField]
    private double fallbackLatitude = 34.693738;

    [SerializeField]
    private double fallbackLongitude = 135.502165;

    public bool IsReady { get; private set; }

    public bool IsUsingFallback { get; private set; }

    public string StatusMessage { get; private set; } = "Not started";

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public bool RefreshNow()
    {
        if (!IsReady)
        {
            Debug.LogWarning("LocationProvider: refresh requested before location was ready.");
            return false;
        }

        if (!IsUsingFallback)
        {
            if (Input.location.status != LocationServiceStatus.Running)
            {
                SetError($"Location service stopped: {Input.location.status}");
                return false;
            }

            UpdateLocation();
        }

        StatusMessage = IsUsingFallback
            ? "Using Editor fallback location"
            : "Using device location";

        Debug.Log(
            $"LocationProvider: refreshed ({Latitude:F6}, {Longitude:F6})");
        return true;
    }

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

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        if (useEditorFallback)
        {
            SetLocation(fallbackLatitude, fallbackLongitude, true);
            StatusMessage = "Using Editor fallback location";
            Debug.Log($"LocationProvider: {StatusMessage} ({Latitude:F6}, {Longitude:F6})");
            yield break;
        }
#endif

        yield return StartLocationService();
    }

    private IEnumerator StartLocationService()
    {
        StatusMessage = "Starting device location service";

        if (!Input.location.isEnabledByUser)
        {
            SetError("Location service is disabled by the user");
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);

            float permissionTimeout = 5f;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) &&
                   permissionTimeout > 0f)
            {
                permissionTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            SetError("Fine location permission was not granted");
            yield break;
        }
#endif

        Input.location.Start(
            Mathf.Max(1f, desiredAccuracyInMeters),
            Mathf.Max(0f, updateDistanceInMeters));

        int remainingSeconds = Mathf.Max(1, startupTimeoutSeconds);

        while (Input.location.status == LocationServiceStatus.Initializing &&
               remainingSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingSeconds--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            SetError($"Location service failed: {Input.location.status}");
            yield break;
        }

        IsUsingFallback = false;
        IsReady = true;
        StatusMessage = "Using device location";
        UpdateLocation();

        Debug.Log($"LocationProvider: {StatusMessage} ({Latitude:F6}, {Longitude:F6})");
    }

    private void Update()
    {
        if (!IsReady || IsUsingFallback)
        {
            return;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            SetError($"Location service stopped: {Input.location.status}");
            return;
        }

        UpdateLocation();
    }

    private void UpdateLocation()
    {
        LocationInfo location = Input.location.lastData;
        Latitude = location.latitude;
        Longitude = location.longitude;
    }

    private void SetLocation(double latitude, double longitude, bool fallback)
    {
        Latitude = latitude;
        Longitude = longitude;
        IsUsingFallback = fallback;
        IsReady = true;
    }

    private void SetError(string message)
    {
        IsReady = false;
        IsUsingFallback = false;
        StatusMessage = message;
        Debug.LogWarning($"LocationProvider: {message}");
    }
}
