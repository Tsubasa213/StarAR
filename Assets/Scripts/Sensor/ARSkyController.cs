using UnityEngine;

public class SkyController : MonoBehaviour
{
    [Header("Sky Root")]
    [SerializeField]
    private Transform skyRoot;

    [Header("North Marker (Debug)")]
    [SerializeField]
    private Transform northMarker;

    [Header("Sky Calibration")]
    [SerializeField]
    private SkyCalibrationController calibrationController;

    public void ResetSensorAndAstronomy()
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
                "SkyController: sky calibration was reset.");
        }
        else
        {
            Debug.LogWarning(
                "SkyController: SkyCalibrationController was not found.");
        }
    }

    private void UpdateNorthMarker()
    {
        // 現在はデバッグ用。
        // 必要になったら天球座標から北方向を計算する。
        _ = northMarker;
    }
}