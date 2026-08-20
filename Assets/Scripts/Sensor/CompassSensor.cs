using UnityEngine;

/// <summary>
/// 端末のコンパスから真北方向を取得する。
/// SkyRoot自体を回転させず、現在の方位だけを提供する。
/// </summary>
public class CompassSensor : MonoBehaviour
{
    public static CompassSensor Instance { get; private set; }

    /// <summary>
    /// 真北からの方位角。
    /// 0 = 北、90 = 東、180 = 南、270 = 西
    /// </summary>
    public float Heading { get; private set; }

    /// <summary>
    /// コンパスが有効な状態か。
    /// </summary>
    public bool IsAvailable { get; private set; }

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

    private void Start()
    {
        EnableCompass();
    }

    private void Update()
    {
        UpdateHeading();
    }

    private void EnableCompass()
    {
        Input.compass.enabled = true;

        // trueHeadingを使用するために位置情報も必要。
        Input.location.Start(10f, 1f);

        IsAvailable = true;

        Debug.Log("CompassSensor: compass enabled.");
    }

    private void UpdateHeading()
    {
        if (!IsAvailable)
        {
            return;
        }

        float trueHeading = Input.compass.trueHeading;

        if (trueHeading >= 0f)
        {
            Heading = trueHeading;
        }
    }

    /// <summary>
    /// コンパスを再有効化する。
    /// 画面復帰時などに使用。
    /// </summary>
    public void Refresh()
    {
        Input.compass.enabled = false;
        Input.compass.enabled = true;

        IsAvailable = true;

        Debug.Log("CompassSensor: compass refreshed.");
    }
}