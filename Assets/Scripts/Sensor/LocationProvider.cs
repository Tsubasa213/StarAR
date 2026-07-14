using System.Collections;
using UnityEngine;

public class LocationProvider : MonoBehaviour
{
    public static LocationProvider Instance { get; private set; }

    public bool IsReady { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("GPSがOFF");
            yield break;
        }

        Input.location.Start();

        int wait = 20;

        while (Input.location.status ==
               LocationServiceStatus.Initializing &&
               wait > 0)
        {
            yield return new WaitForSeconds(1);
            wait--;
        }

        if (Input.location.status !=
            LocationServiceStatus.Running)
        {
            Debug.Log("GPS取得失敗");
            yield break;
        }

        IsReady = true;

        Debug.Log("GPS開始");
    }

    void Update()
    {
        if (!IsReady)
            return;

        Latitude =
            Input.location.lastData.latitude;

        Longitude =
            Input.location.lastData.longitude;
    }
}