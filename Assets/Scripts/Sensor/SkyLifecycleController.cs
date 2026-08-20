using UnityEngine;

///
/// アプリの一時停止・復帰を検知して、
/// GPS・コンパス・天球のキャリブレーションを再同期する。
///
public class SkyLifecycleController : MonoBehaviour
{
    [SerializeField]
    private SkyCalibrationController calibrationController;

    private void Awake()
    {
        if (calibrationController == null)
        {
            calibrationController =
                FindAnyObjectByType<SkyCalibrationController>();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            Debug.Log(
                "SkyLifecycleController: application paused.");
            return;
        }

        Debug.Log(
            "SkyLifecycleController: application resumed.");

        RefreshSky();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            return;
        }

        Debug.Log(
            "SkyLifecycleController: application focus restored.");

        RefreshSky();
    }

    private void RefreshSky()
    {
        if (calibrationController == null)
        {
            calibrationController =
                FindAnyObjectByType<SkyCalibrationController>();
        }

        if (calibrationController != null)
        {
            calibrationController.Recalibrate();

            Debug.Log(
                "SkyLifecycleController: sky was recalibrated.");
        }
        else
        {
            Debug.LogWarning(
                "SkyLifecycleController: " +
                "SkyCalibrationController was not found.");
        }
    }
}