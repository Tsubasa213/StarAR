using UnityEngine;

public class SkyRotation : MonoBehaviour
{
    [SerializeField]
    private Transform celestialSphere;

    [Header("デバッグ倍率")]
    [SerializeField]
    private float timeScale = 600f;

    // 24時間で360°
    private const float EarthRotationPerSecond =
        360f / (24f * 60f * 60f);

    void Update()
    {
        float angle =
            EarthRotationPerSecond *
            timeScale *
            Time.deltaTime;

        celestialSphere.Rotate(
            Vector3.up,
            angle,
            Space.Self
        );
    }
}