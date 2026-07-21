using UnityEngine;
using UnityEngine.InputSystem;
using InputSystemAttitudeSensor = UnityEngine.InputSystem.AttitudeSensor;
using InputSystemGyroscope = UnityEngine.InputSystem.Gyroscope;

public class SkyController : MonoBehaviour
{
    [Header("Sky Root")]
    [SerializeField]
    private Transform skyRoot;

    [Header("North Marker (Debug)")]
    [SerializeField]
    private Transform northMarker;

    [Header("Gyroscope Camera")]
    [SerializeField]
    private bool useGyroscope = true;

    [Tooltip("Camera transform driven by the gyroscope. Main Camera is used when empty.")]
    [SerializeField]
    private Transform gyroCamera;

    [Tooltip("Invert the device rotation if the view moves opposite to the phone.")]
    [SerializeField]
    private bool invertGyroscopeRotation = true;

    [Tooltip("Use the Android fused attitude sensor first. This avoids accumulating gyro drift.")]
    [SerializeField]
    private bool preferAttitudeSensor = true;

    [Tooltip("Sampling frequency used by the raw gyroscope fallback.")]
    [Min(30f)]
    [SerializeField]
    private float gyroSamplingFrequency = 120f;

    [Tooltip("Reject very small raw gyro values to reduce idle jitter.")]
    [Min(0f)]
    [SerializeField]
    private float gyroNoiseDeadzone = 0.005f;

    private Quaternion initialCameraRotation;
    private Quaternion accumulatedGyroRotation = Quaternion.identity;
    private Quaternion referenceAttitude;
    private InputSystemGyroscope gyroSensor;
    private InputSystemAttitudeSensor attitudeSensor;
    private bool hasReferenceAttitude;
    private bool gyroReady;
    private Vector3 filteredAngularVelocity;
    private bool hasFilteredAngularVelocity;

    private void Start()
    {
        if (!useGyroscope)
        {
            Debug.LogWarning("SkyController: gyroscope input is disabled.");
            return;
        }

        gyroSensor = InputSystemGyroscope.current;
        attitudeSensor = InputSystemAttitudeSensor.current;

        if (gyroSensor != null)
        {
            InputSystem.EnableDevice(gyroSensor);
            gyroSensor.samplingFrequency = Mathf.Max(30f, gyroSamplingFrequency);
        }

        if (attitudeSensor != null)
        {
            InputSystem.EnableDevice(attitudeSensor);
        }

        if (gyroSensor == null && attitudeSensor == null)
        {
            Debug.LogWarning(
                "SkyController: no Input System gyroscope or attitude sensor was found.");
            return;
        }

        if (!SetupGyroscopeCamera())
        {
            return;
        }

        gyroReady = true;
        Debug.Log(
            $"SkyController: Input System sensors enabled. " +
            $"gyro={gyroSensor != null}, attitude={attitudeSensor != null}, " +
            $"attitudeFirst={preferAttitudeSensor}");
    }

    private void LateUpdate()
    {
        UpdateGyroscopeRotation();
    }

    private bool SetupGyroscopeCamera()
    {
        if (gyroCamera == null && Camera.main != null)
        {
            gyroCamera = Camera.main.transform;
        }

        if (gyroCamera == null)
        {
            Debug.LogWarning("SkyController: Main Camera is not available.");
            return false;
        }

        DisableComponent(gyroCamera.gameObject, "TrackedPoseDriver");
        DisableComponent(gameObject, "PlayerCameraController");
        initialCameraRotation = gyroCamera.localRotation;
        return true;
    }

    private void UpdateGyroscopeRotation()
    {
        if (!gyroReady || gyroCamera == null)
        {
            return;
        }

        Quaternion relativeRotation = accumulatedGyroRotation;
        bool hasRotation = false;

        if (preferAttitudeSensor &&
            attitudeSensor != null &&
            attitudeSensor.enabled)
        {
            Quaternion currentAttitude = attitudeSensor.attitude.ReadValue();
            if (IsValidAttitude(currentAttitude))
            {
                if (!hasReferenceAttitude)
                {
                    referenceAttitude = currentAttitude;
                    hasReferenceAttitude = true;
                }

                relativeRotation = currentAttitude *
                    Quaternion.Inverse(referenceAttitude);
                hasRotation = true;
            }
        }

        if (!hasRotation && gyroSensor != null && gyroSensor.enabled)
        {
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Vector3 sensorRotationRate = gyroSensor.angularVelocity.ReadValue();
            float smoothing = 1f - Mathf.Exp(-18f * deltaTime);

            if (!hasFilteredAngularVelocity)
            {
                filteredAngularVelocity = sensorRotationRate;
                hasFilteredAngularVelocity = true;
            }
            else
            {
                filteredAngularVelocity = Vector3.Lerp(
                    filteredAngularVelocity,
                    sensorRotationRate,
                    smoothing);
            }

            Vector3 unityRotationRate = filteredAngularVelocity;
            if (unityRotationRate.magnitude < gyroNoiseDeadzone)
            {
                unityRotationRate = Vector3.zero;
            }

            float angularSpeed = unityRotationRate.magnitude;
            if (angularSpeed > 0.0001f && deltaTime > 0f)
            {
                float angle = angularSpeed * Mathf.Rad2Deg * deltaTime;
                Quaternion deltaRotation = Quaternion.AngleAxis(
                    angle,
                    unityRotationRate / angularSpeed);

                accumulatedGyroRotation =
                    accumulatedGyroRotation * deltaRotation;
            }

            relativeRotation = accumulatedGyroRotation;
            hasRotation = true;
        }

        if (!hasRotation)
        {
            return;
        }

        if (invertGyroscopeRotation)
        {
            relativeRotation = Quaternion.Inverse(relativeRotation);
        }

        gyroCamera.localRotation = relativeRotation * initialCameraRotation;
    }

    public void ResetSensorAndAstronomy()
    {
        if (gyroCamera != null)
        {
            initialCameraRotation = gyroCamera.localRotation;
        }

        accumulatedGyroRotation = Quaternion.identity;
        filteredAngularVelocity = Vector3.zero;
        hasFilteredAngularVelocity = false;

        if (attitudeSensor != null && attitudeSensor.enabled)
        {
            Quaternion currentAttitude = attitudeSensor.attitude.ReadValue();
            if (IsValidAttitude(currentAttitude))
            {
                referenceAttitude = currentAttitude;
                hasReferenceAttitude = true;
            }
        }

        LocationProvider location = LocationProvider.Instance;
        if (location != null)
        {
            location.RefreshNow();
        }

        CelestialSphereController celestial =
            FindAnyObjectByType<CelestialSphereController>();
        if (celestial != null)
        {
            celestial.ResetToCurrentAstronomy();
        }

        Debug.Log(
            "SkyController: sensor reference, location, and astronomy time " +
            "were reset.");
    }

    private static bool IsValidAttitude(Quaternion attitude)
    {
        float magnitude = Mathf.Abs(attitude.x) +
            Mathf.Abs(attitude.y) +
            Mathf.Abs(attitude.z) +
            Mathf.Abs(attitude.w);

        return magnitude > 0.001f;
    }

    private static void DisableComponent(GameObject target, string componentTypeName)
    {
        if (target == null)
        {
            return;
        }

        Behaviour component = target.GetComponent(componentTypeName) as Behaviour;
        if (component != null)
        {
            component.enabled = false;
            Debug.Log($"SkyController: disabled {componentTypeName} for gyroscope mode.");
        }
    }

    private void UpdateNorthMarker() => _ = northMarker;
}
