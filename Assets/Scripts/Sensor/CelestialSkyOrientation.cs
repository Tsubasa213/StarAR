
using System;
using UnityEngine;

public class CelestialSkyOrientation : MonoBehaviour
{
    [Header("天球設定")]
    [SerializeField] private Transform celestialSphere;

    [Header("デバッグログ設定")]
    [SerializeField] private float debugLogInterval = 10f;

    private double latitude;
    private double longitude;

    private Vector3 worldNorth = Vector3.forward;
    private bool locationReady;
    private bool calibrationReady;

    private float nextDebugLogTime;
    private float nextWarningLogTime;

    public void SetLocation(double lat, double lon)
    {
        latitude = lat;
        longitude = lon;
        locationReady = true;

        Debug.Log(
            $"天球位置設定：緯度={latitude:F6}，経度={longitude:F6}");
    }

    public void CalibrateNorth(
        float trueHeadingDegrees,
        Transform cameraTransform)
    {
        if (celestialSphere == null)
            celestialSphere = transform;

        if (cameraTransform == null)
        {
            Debug.LogError("ARカメラが設定されていません。");
            return;
        }

        if (trueHeadingDegrees < 0f ||
            trueHeadingDegrees > 360f)
        {
            Debug.LogWarning("有効な真北方位を取得できません。");
            return;
        }

        // カメラの向きを水平面に投影する
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                cameraTransform.forward,
                Vector3.up);

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning(
                "スマートフォンを水平に近い状態にしてください。");
            return;
        }

        cameraForward.Normalize();

        // Unityの水平面で，カメラの右方向を求める
        Vector3 cameraRight =
            Vector3.Cross(
                Vector3.up,
                cameraForward).normalized;

        float headingRad =
            trueHeadingDegrees * Mathf.Deg2Rad;

        // カメラの向きと真北方位から，ワールド上の北を求める
        worldNorth =
            cameraForward * Mathf.Cos(headingRad)
            - cameraRight * Mathf.Sin(headingRad);

        worldNorth =
            Vector3.ProjectOnPlane(
                worldNorth,
                Vector3.up).normalized;

        calibrationReady = true;

        Debug.Log(
            $"北方向校正：heading={trueHeadingDegrees:F2}°，" +
            $"worldNorth={worldNorth}");

        // 校正直後に天球の姿勢を更新する
        ApplyAstronomicalOrientation(DateTime.UtcNow);
    }

    public void ApplyAstronomicalOrientation(DateTime utc)
    {
        if (!locationReady ||
            !calibrationReady ||
            celestialSphere == null)
        {
            // 準備未完了時の警告を10秒間隔に制限する
            if (Time.unscaledTime >= nextWarningLogTime)
            {
                nextWarningLogTime =
                    Time.unscaledTime +
                    Mathf.Max(0.1f, debugLogInterval);

                Debug.LogWarning(
                    "天球の姿勢を更新できません。" +
                    "位置情報または方位校正が未完了です。");
            }

            return;
        }

        // 地方恒星時を計算する
        double lstDegrees =
            CalculateLST(utc, longitude);

        double lstRad =
            lstDegrees * Mathf.Deg2Rad;

        double latRad =
            latitude * Mathf.Deg2Rad;

        float sinLst = (float)Math.Sin(lstRad);
        float cosLst = (float)Math.Cos(lstRad);

        float sinLat = (float)Math.Sin(latRad);
        float cosLat = (float)Math.Cos(latRad);

        Vector3 north = worldNorth.normalized;
        Vector3 up = Vector3.up;

        // 東方向
        Vector3 east =
            Vector3.Cross(up, north).normalized;

        // 天の北極の方向
        // 北極の高度は観測地点の緯度に等しい
        Vector3 celestialPole =
            north * cosLat +
            up * sinLat;

        // 赤経90度，赤緯0度に対応する方向
        Vector3 ra90 =
            east * cosLst
            - north * (sinLst * sinLat)
            + up * (sinLst * cosLat);

        // 天球の回転を更新する
        celestialSphere.rotation =
            Quaternion.LookRotation(
                ra90.normalized,
                celestialPole.normalized);

        // 姿勢更新ログを10秒間隔に制限する
        if (Time.unscaledTime >= nextDebugLogTime)
        {
            nextDebugLogTime =
                Time.unscaledTime +
                Mathf.Max(0.1f, debugLogInterval);

            Debug.Log(
                $"天球姿勢更新：UTC={utc:yyyy-MM-dd HH:mm:ss}，" +
                $"LST={lstDegrees:F3}°，緯度={latitude:F4}°");
        }
    }

    public static double CalculateLST(
        DateTime utc,
        double longitude)
    {
        // 入力時刻をUTCに統一する
        utc = utc.Kind == DateTimeKind.Utc
            ? utc
            : utc.ToUniversalTime();

        DateTime epoch =
            new DateTime(
                1970, 1, 1, 0, 0, 0,
                DateTimeKind.Utc);

        double jd =
            2440587.5 +
            (utc - epoch).TotalSeconds / 86400.0;

        double d = jd - 2451545.0;
        double t = d / 36525.0;

        double gmst =
            280.46061837
            + 360.98564736629 * d
            + 0.000387933 * t * t
            - t * t * t / 38710000.0;

        double lst = (gmst + longitude) % 360.0;

        if (lst < 0.0)
            lst += 360.0;

        return lst;
    }
}