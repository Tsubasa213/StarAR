using System;
using UnityEngine;

public class CelestialSkyRotation : MonoBehaviour
{
    [SerializeField] private CelestialSkyOrientation orientation;

    [Header("Time")]
    [SerializeField] private bool useRealTime = true;

    [Tooltip("useRealTime=false の場合に使用")]
    [SerializeField] private float timeScale = 1f;

    private DateTime simulatedUtc;

    private void Awake()
    {
        if (orientation == null)
            orientation =
                GetComponent<CelestialSkyOrientation>();
    }

    private void Start()
    {
        simulatedUtc = DateTime.UtcNow;
    }

    private void Update()
    {
        if (orientation == null)
            return;

        if (useRealTime)
        {
            simulatedUtc = DateTime.UtcNow;
        }
        else
        {
            simulatedUtc =
                simulatedUtc.AddSeconds(
                    Time.unscaledDeltaTime *
                    timeScale);
        }

        // ここだけが時間による天球の更新。
        // GyroやCamera姿勢はここでは使用しない。
        orientation.ApplyAstronomicalOrientation(
            simulatedUtc);
    }

    public void ResetToRealTime()
    {
        simulatedUtc = DateTime.UtcNow;
        useRealTime = true;
    }

    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Max(0f, scale);
        useRealTime = false;
    }
}
