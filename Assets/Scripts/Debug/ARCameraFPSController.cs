using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;

public class ARCameraFPSController : MonoBehaviour
{
    private ARCameraManager cameraManager;

    void Start()
    {
        cameraManager = GetComponent<ARCameraManager>();

#if UNITY_IOS && !UNITY_EDITOR
        SetCameraFPS();
#endif
    }

    private void SetCameraFPS()
    {
        if (cameraManager == null)
        {
            Debug.LogError("ARCameraFPSController: ARCameraManagerが見つかりません");
            return;
        }

        var subsystem = cameraManager.subsystem;

        if (subsystem == null)
        {
            Debug.LogError("ARCameraFPSController: ARCameraSubsystemが見つかりません");
            return;
        }

        var configurations = subsystem.GetConfigurations(Allocator.Temp);

        foreach (var configuration in configurations)
        {
            Debug.Log(
                $"AR Camera Configuration: " +
                $"FPS={configuration.framerate}, " +
                $"Resolution={configuration.width}x{configuration.height}"
            );

            if (configuration.framerate == 60)
            {
                subsystem.currentConfiguration = configuration;

                Debug.Log("AR Camera FPS: 60 FPSに設定しました");
                break;
            }
        }

        configurations.Dispose();
    }
}