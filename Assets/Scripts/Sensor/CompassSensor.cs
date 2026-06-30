using UnityEngine;

public class CompassSensor : MonoBehaviour
{
    /// <summary>
    /// 真北基準の方位角（0～360°）
    /// </summary>
    public float Heading { get; private set; }

    /// <summary>
    /// コンパスが利用可能か
    /// </summary>
    public bool IsAvailable { get; private set; }

    void Start()
    {
        Input.compass.enabled = true;
        Input.gyro.enabled = true;

        IsAvailable = SystemInfo.supportsGyroscope;
    }

    void Update()
    {
        if (!IsAvailable)
            return;

        Heading = Input.compass.trueHeading;
    }
}