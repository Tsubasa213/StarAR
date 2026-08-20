// using System;
// using UnityEngine;

// /// <summary>
// /// GPS・コンパス・時刻から天球の向きを計算する。
// ///
// /// SkyRootの回転を担当する唯一のスクリプト。
// /// Cameraの回転には一切触れない。
// /// </summary>
// public class CelestialSphereController : MonoBehaviour
// {
//     [Header("Rotation Target")]
//     [SerializeField]
//     private Transform celestialSphere;

//     [Header("Alignment")]
//     [Tooltip("星・天の川のモデルと実際の天球座標を合わせるための補正値")]
//     [SerializeField]
//     private Vector3 alignmentEulerAngles = Vector3.zero;

//     [Header("Sidereal Time")]
//     [Tooltip("1 = 実時間。600 = 恒星時を約600倍速")]
//     [Min(0f)]
//     [SerializeField]
//     private float siderealTimeScale = 1f;

//     [SerializeField]
//     private bool reverseSiderealDirection = false;

//     [Header("Compass")]
//     [Tooltip("コンパス方位をSkyRootのY回転に反映する")]
//     [SerializeField]
//     private bool useCompass = true;

//     [Tooltip("コンパスの向きを反転する必要がある場合")]
//     [SerializeField]
//     private bool reverseCompass = false;

//     [Header("Debug")]
//     [SerializeField]
//     private bool logAstronomyState = true;

//     [SerializeField]
//     private float logIntervalSeconds = 5f;

//     private Quaternion alignmentRotation;

//     private double simulatedSiderealTime;

//     private bool initialized;

//     private float nextLogTime;

//     public DateTime CurrentUtc { get; private set; }

//     public double CurrentLocalSiderealTimeDegrees
//     {
//         get;
//         private set;
//     }

//     public double Latitude
//     {
//         get;
//         private set;
//     }

//     public double Longitude
//     {
//         get;
//         private set;
//     }

//     public float Heading
//     {
//         get;
//         private set;
//     }

//     private void Awake()
//     {
//         if (celestialSphere == null)
//         {
//             celestialSphere = transform;
//         }

//         alignmentRotation =
//             Quaternion.Euler(alignmentEulerAngles);
//     }

//     private void Start()
//     {
//         RecalculateSky();
//     }

//     private void Update()
//     {
//         RecalculateSky();
//     }

//     /// <summary>
//     /// GPS・コンパス・時刻から天球を再計算する。
//     /// </summary>
//     public void RecalculateSky()
//     {
//         if (celestialSphere == null)
//         {
//             return;
//         }

//         LocationProvider location =
//             LocationProvider.Instance;

//         CompassSensor compass =
//             CompassSensor.Instance;

//         if (location == null ||
//             !location.IsReady)
//         {
//             return;
//         }

//         Latitude = location.Latitude;
//         Longitude = location.Longitude;

//         CurrentUtc = DateTime.UtcNow;

//         CurrentLocalSiderealTimeDegrees =
//             SiderealTime.GetLocalSiderealTime(
//                 CurrentUtc,
//                 Longitude
//             );

//         if (!initialized)
//         {
//             simulatedSiderealTime =
//                 CurrentLocalSiderealTimeDegrees;

//             initialized = true;
//         }
//         else
//         {
//             if (Mathf.Approximately(
//                     siderealTimeScale,
//                     1f))
//             {
//                 simulatedSiderealTime =
//                     CurrentLocalSiderealTimeDegrees;
//             }
//             else
//             {
//                 simulatedSiderealTime =
//                     NormalizeDegrees(
//                         simulatedSiderealTime +
//                         GetSiderealDelta() *
//                         siderealTimeScale
//                     );
//             }
//         }

//         if (compass != null &&
//             compass.IsAvailable)
//         {
//             Heading = compass.Heading;
//         }

//         ApplyRotation();

//         WriteDebugLog(location);
//     }

//     private double previousSiderealTime;

//     private double GetSiderealDelta()
//     {
//         double current =
//             CurrentLocalSiderealTimeDegrees;

//         if (!initialized)
//         {
//             previousSiderealTime = current;
//             return 0d;
//         }

//         double delta =
//             current - previousSiderealTime;

//         if (delta < -180d)
//         {
//             delta += 360d;
//         }
//         else if (delta > 180d)
//         {
//             delta -= 360d;
//         }

//         previousSiderealTime = current;

//         return delta;
//     }

//     private void ApplyRotation()
//     {
//         /*
//          * 緯度による天球の傾き
//          */
//         Quaternion latitudeRotation =
//             Quaternion.Euler(
//                 (float)Latitude - 90f,
//                 0f,
//                 0f
//             );

//         /*
//          * 恒星時による地球自転
//          */
//         float siderealDirection =
//             reverseSiderealDirection
//                 ? 1f
//                 : -1f;

//         Quaternion siderealRotation =
//             Quaternion.Euler(
//                 0f,
//                 (float)simulatedSiderealTime *
//                 siderealDirection,
//                 0f
//             );

//         /*
//          * 真北方向
//          */
//         Quaternion compassRotation =
//             Quaternion.identity;

//         if (useCompass)
//         {
//             float heading =
//                 reverseCompass
//                     ? -Heading
//                     : Heading;

//             compassRotation =
//                 Quaternion.Euler(
//                     0f,
//                     -heading,
//                     0f
//                 );
//         }

//         celestialSphere.localRotation =
//             alignmentRotation *
//             compassRotation *
//             siderealRotation *
//             latitudeRotation;
//     }

//     /// <summary>
//     /// 画面復帰時などに呼び出す。
//     /// </summary>
//     public void ResetToCurrentAstronomy()
//     {
//         initialized = false;
//         previousSiderealTime = 0d;

//         CompassSensor compass =
//             CompassSensor.Instance;

//         if (compass != null)
//         {
//             compass.Refresh();
//         }

//         RecalculateSky();

//         Debug.Log(
//             "CelestialSphereController: astronomy recalculated."
//         );
//     }

//     private void WriteDebugLog(
//         LocationProvider location)
//     {
//         if (!logAstronomyState)
//         {
//             return;
//         }

//         if (Time.unscaledTime < nextLogTime)
//         {
//             return;
//         }

//         Debug.Log(
//             $"Sky: " +
//             $"lat={Latitude:F6}, " +
//             $"lon={Longitude:F6}, " +
//             $"heading={Heading:F2}, " +
//             $"UTC={CurrentUtc:O}, " +
//             $"LST={simulatedSiderealTime:F3}"
//         );

//         nextLogTime =
//             Time.unscaledTime +
//             Mathf.Max(
//                 1f,
//                 logIntervalSeconds
//             );
//     }

//     private static double NormalizeDegrees(
//         double degrees)
//     {
//         degrees %= 360d;

//         if (degrees < 0d)
//         {
//             degrees += 360d;
//         }

//         return degrees;
//     }
// }