using UnityEngine;

public class SkyController : MonoBehaviour
{
    [Header("Sky Root")]
    [SerializeField]
    private Transform skyRoot;

    [Header("North Marker (Debug)")]
    [SerializeField]
    private Transform northMarker;

    void Start()
    {
        // コンパス有効
        Input.compass.enabled = true;

        // ジャイロ有効
        Input.gyro.enabled = true;
    }

    void Update()
    {
        UpdateNorthMarker();
    }

    void UpdateNorthMarker()
    {
        float heading = Input.compass.trueHeading;

        // 北マーカーを回転
        if (northMarker != null)
        {
            northMarker.localRotation =
                Quaternion.Euler(
                    0,
                    -heading,
                    0
                );
        }

        // 星空も同じだけ回転
        if (skyRoot != null)
        {
            skyRoot.localRotation =
                Quaternion.Euler(
                    0,
                    -heading,
                    0
                );
        }
    }
}