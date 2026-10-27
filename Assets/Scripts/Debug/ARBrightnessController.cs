using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Applies a black filter to the AR camera background.
/// The camera image is darkened before AR stars are rendered, so stars remain bright.

/// </summary>
public class ARBrightnessController : MonoBehaviour
{
    [Header("Black Filter")]
    [SerializeField]
    private Volume targetVolume;

    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    [Range(0f, 0.9f)]
    [FormerlySerializedAs("initialExposure")]
    private float initialFilterOpacity = 0.8f;

    [SerializeField]
    [Range(0f, 0.9f)]
    [FormerlySerializedAs("minimumExposure")]
    private float minimumFilterOpacity = 0f;

    [SerializeField]
    [Range(0f, 1f)]
    [FormerlySerializedAs("maximumExposure")]
    private float maximumFilterOpacity = 1f;

    [Header("Android Test Mode")]
    [Tooltip("Use the same solid black camera background as Windows testing. AR sensors remain enabled.")]
    [SerializeField]
    private bool useSolidBlackBackgroundForAndroidTest = false;

    [Header("Runtime UI")]
    [SerializeField]
    private bool createRuntimeSlider = true;

    [SerializeField]
    private bool createResetButton = true;

    private Text filterLabel = null;
    private GameObject runtimeCanvasObject = null;
    private ARCameraBackground cameraBackground = null;
    private Material runtimeBackgroundMaterial = null;
    [SerializeField]
    private Material sphereFilterMaterial;

    private void Awake()
    {
        // Kept serialized for compatibility with the existing scene component.
        _ = targetVolume;
        _ = cameraBackground;
        _ = createRuntimeSlider;
        _ = createResetButton;
        _ = runtimeCanvasObject;
        _ = useSolidBlackBackgroundForAndroidTest;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
{
#if UNITY_ANDROID && !UNITY_EDITOR
    StartAndroidBrightnessFilter();
#elif UNITY_IOS && !UNITY_EDITOR
    EnableARCameraForIOS();
#else
    ApplySolidBlackBackground();
#endif
}

#if UNITY_ANDROID && !UNITY_EDITOR
    private void StartAndroidBrightnessFilter()
    {
        if (useSolidBlackBackgroundForAndroidTest)
        {
            ApplySolidBlackBackground();
            DisableARTrackingForAndroidGyroTest();
            Debug.Log(
                "ARBrightnessController: Android solid black background test mode enabled.");
            return;
        }

        EnableARCameraForNormalMode();

        if (createRuntimeSlider)
        {
            EnsureEventSystem();
            CreateRuntimeSlider();
        }

        if (!ConfigureCameraBackgroundFilter())
        {
            Debug.LogWarning(
                "ARBrightnessController: Slider was created, but the camera background filter could not be configured.");
            return;
        }

        SetFilterOpacity(initialFilterOpacity);
    }

    private void EnableARCameraForNormalMode()
    {
        if (targetCamera != null)
        {
            ARCameraManager cameraManager =
                targetCamera.GetComponent<ARCameraManager>();

            if (cameraManager != null)
            {
                cameraManager.enabled = true;
            }

            ARCameraBackground cameraBackground =
                targetCamera.GetComponent<ARCameraBackground>();

            if (cameraBackground != null)
            {
                cameraBackground.enabled = true;
            }
        }

        ARSession session = FindFirstObjectByType<ARSession>();
        if (session != null)
        {
            session.enabled = true;
        }

        Debug.Log(
            "ARBrightnessController: AR camera background and AR session enabled. " +
            "Gyroscope remains the camera rotation source.");
    }

    private void DisableARTrackingForAndroidGyroTest()
    {
        if (targetCamera != null)
        {
            ARCameraManager cameraManager =
                targetCamera.GetComponent<ARCameraManager>();

            if (cameraManager != null)
            {
                cameraManager.enabled = false;
            }

            ARCameraBackground cameraBackground =
                targetCamera.GetComponent<ARCameraBackground>();

            if (cameraBackground != null)
            {
                cameraBackground.enabled = false;
            }

            Behaviour trackedPoseDriver =
                targetCamera.GetComponent("TrackedPoseDriver") as Behaviour;

            if (trackedPoseDriver != null)
            {
                trackedPoseDriver.enabled = false;
            }
        }

        ARSession session = FindFirstObjectByType<ARSession>();
        if (session != null)
        {
            session.enabled = false;
        }

        Debug.Log(
            "ARBrightnessController: AR tracking disabled for Android gyro test mode.");
    }

    private bool ConfigureCameraBackgroundFilter()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning(
                "ARBrightnessController: Target camera was not found.");
            return false;
        }

        UniversalAdditionalCameraData cameraData =
            targetCamera.GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            // Keep the camera in its normal mode. Brightness is changed only
            // by the AR background material below, not by post exposure.
            cameraData.renderPostProcessing = false;
        }

        ARCameraManager cameraManager =
            targetCamera.GetComponent<ARCameraManager>();

        if (cameraManager != null)
        {
            // The background must be drawn before the AR stars.
            cameraManager.requestedBackgroundRenderingMode =
                CameraBackgroundRenderingMode.BeforeOpaques;
        }

        cameraBackground =
            targetCamera.GetComponent<ARCameraBackground>();

        if (cameraBackground == null)
        {
            Debug.LogWarning(
                "ARBrightnessController: ARCameraBackground was not found.");
            return false;
        }

        Material sourceMaterial = cameraBackground.customMaterial;

        if (sourceMaterial == null)
        {
            Shader shader = Shader.Find(
                "Unlit/ARCoreBackgroundWithBlackFilter");

            if (shader == null)
            {
                Debug.LogWarning(
                    "ARBrightnessController: Camera background filter shader was not found.");
                return false;
            }

            sourceMaterial = new Material(shader);
        }

        runtimeBackgroundMaterial = new Material(sourceMaterial);
        runtimeBackgroundMaterial.name = "AR Camera Background Filter (Runtime)";
        cameraBackground.customMaterial = runtimeBackgroundMaterial;
        cameraBackground.useCustomMaterial = true;
        return true;
    }
#endif

    private void ApplySolidBlackBackground()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;

        UniversalAdditionalCameraData cameraData =
            targetCamera.GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            cameraData.renderPostProcessing = false;
        }

        ARCameraBackground cameraBackground =
            targetCamera.GetComponent<ARCameraBackground>();

        if (cameraBackground != null)
        {
            cameraBackground.enabled = false;
        }

        _ = cameraBackground;
        _ = initialFilterOpacity;
    }

    public void SetFilterOpacity(float opacity)
    {
        float clampedOpacity = Mathf.Clamp(
            opacity,
            minimumFilterOpacity,
            maximumFilterOpacity);

        if (runtimeBackgroundMaterial != null &&
            runtimeBackgroundMaterial.HasProperty("_FilterOpacity"))
        {
            runtimeBackgroundMaterial.SetFloat(
                "_FilterOpacity",
                clampedOpacity);
        }
        if (sphereFilterMaterial != null)
        {
            Color color = sphereFilterMaterial.color;
            color.a = clampedOpacity;
            sphereFilterMaterial.color = color;
        }

        if (filterLabel != null)
        {
            filterLabel.text =
                "Dark filter " +
                Mathf.RoundToInt(clampedOpacity * 100f) +
                "%";
        }
    }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    private void CreateRuntimeSlider()
    {
        runtimeCanvasObject = new GameObject("AR Brightness UI");

        Canvas canvas = runtimeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvas.overrideSorting = true;

        CanvasScaler scaler = runtimeCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        runtimeCanvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject(
            "Brightness Panel",
            typeof(RectTransform),
            typeof(Image));
        panelObject.transform.SetParent(
            runtimeCanvasObject.transform,
            false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(18f, 0f);
        panelRect.sizeDelta = new Vector2(150f, 560f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);
        panelImage.raycastTarget = true;

        // Numeric label is intentionally omitted. The vertical slider is placed
        // along the left edge so it does not cover the stars in the center.
        CreateSlider(panelObject.transform);
        Debug.Log("[ARFilter] UI canvas created: ScreenSpaceOverlay, sortingOrder=" + canvas.sortingOrder +
            ", panelSize=" + panelRect.sizeDelta + ", panelPosition=" + panelRect.anchoredPosition);

        if (createResetButton)
        {
            CreateResetButton(panelObject.transform);
        }
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("AR UI EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();

        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        foreach (BaseInputModule module in
                 eventSystem.GetComponents<BaseInputModule>())
        {
            module.enabled = module == inputModule;
        }

        inputModule.AssignDefaultActions();
        inputModule.enabled = true;
    }

    private void CreateSlider(Transform parent)
    {
        GameObject sliderObject = new GameObject(
            "Brightness Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, 18f);
        sliderRect.sizeDelta = new Vector2(76f, 430f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = minimumFilterOpacity;
        slider.maxValue = maximumFilterOpacity;
        slider.direction = Slider.Direction.BottomToTop;
        slider.wholeNumbers = false;

        GameObject trackObject = new GameObject(
            "Track", typeof(RectTransform), typeof(Image));
        trackObject.transform.SetParent(sliderObject.transform, false);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0f);
        trackRect.anchorMax = new Vector2(0.5f, 1f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.anchoredPosition = Vector2.zero;
        trackRect.sizeDelta = new Vector2(12f, -42f);
        Image trackImage = trackObject.GetComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.38f);
        trackImage.raycastTarget = false;

        GameObject fillAreaObject = new GameObject(
            "Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0.5f, 0f);
        fillAreaRect.anchorMax = new Vector2(0.5f, 1f);
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchoredPosition = Vector2.zero;
        fillAreaRect.sizeDelta = new Vector2(12f, -42f);

        GameObject fillObject = new GameObject(
            "Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.35f, 0.75f, 1f, 0.95f);
        fillImage.raycastTarget = false;

        GameObject handleAreaObject = new GameObject(
            "Handle Slide Area", typeof(RectTransform));
        handleAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0.5f, 0f);
        handleAreaRect.anchorMax = new Vector2(0.5f, 1f);
        handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
        handleAreaRect.anchoredPosition = Vector2.zero;
        handleAreaRect.sizeDelta = new Vector2(76f, -42f);

        GameObject handleObject = new GameObject(
            "Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(handleAreaObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0f);
        handleRect.anchorMax = new Vector2(0.5f, 0f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(66f, 26f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;
        handleImage.raycastTarget = true;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.value = Mathf.Clamp(initialFilterOpacity, minimumFilterOpacity, maximumFilterOpacity);
        slider.onValueChanged.AddListener(SetFilterOpacity);

        Debug.Log("[ARFilter] Vertical slider created: Direction=" +
            slider.direction + ", rect=" + sliderRect.rect.size +
            ", fillRect=" + (slider.fillRect != null) +
            ", handleRect=" + (slider.handleRect != null));
    }

    private void CreateResetButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "Sensor Re-sync Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = Vector2.zero;
        buttonRect.anchoredPosition = new Vector2(12f, 12f);
        buttonRect.sizeDelta = new Vector2(180f, 70f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0f, 0f, 0f, 0f);
        buttonImage.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ResetSensorAndAstronomy);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.45f, 0.75f, 0.95f);
        colors.highlightedColor = new Color(0.25f, 0.6f, 0.9f, 1f);
        colors.pressedColor = new Color(0.08f, 0.3f, 0.55f, 1f);
        button.colors = colors;


    }

    private static void ResetSensorAndAstronomy()
    {
        SkyController skyController =
            FindFirstObjectByType<SkyController>();

        if (skyController == null)
        {
            Debug.LogWarning(
                "ARBrightnessController: SkyController was not found for re-sync.");
            return;
        }

        skyController.ResetSensorAndAstronomy();
    }

    private static Text CreateText(
        Transform parent,
        string textValue,
        int fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            "Filter Label",
            typeof(RectTransform),
            typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.text = textValue;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }
#endif

    private void OnDestroy()
    {
        if (runtimeBackgroundMaterial != null)
        {
            Destroy(runtimeBackgroundMaterial);
            runtimeBackgroundMaterial = null;
        }
    }


#if UNITY_IOS && !UNITY_EDITOR
    private void EnableARCameraForIOS()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            ARCameraManager cameraManager =
                targetCamera.GetComponent<ARCameraManager>();

            if (cameraManager != null)
            {
                cameraManager.enabled = true;
            }

            cameraBackground =
                targetCamera.GetComponent<ARCameraBackground>();

            if (cameraBackground != null)
            {
                cameraBackground.enabled = true;
            }
        }

        ARSession session = FindFirstObjectByType<ARSession>();
        if (session != null)
        {
            session.enabled = true;
        }

        if (createRuntimeSlider)
        {
            EnsureEventSystem();
            CreateRuntimeSlider();
        }

        if (!ConfigureIOSCameraBackgroundFilter())
        {
            Debug.LogWarning(
                "ARBrightnessController: iOS camera filter setup failed. " +
                "Check the ARKit background shader and build settings.");
            return;
        }

        SetFilterOpacity(initialFilterOpacity);
        Debug.Log(
            "ARBrightnessController: iOS AR camera and black filter enabled.");
    }

    private bool ConfigureIOSCameraBackgroundFilter()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning(
                "ARBrightnessController: Target camera was not found.");
            return false;
        }

        cameraBackground =
            targetCamera.GetComponent<ARCameraBackground>();

        if (cameraBackground == null)
        {
            Debug.LogWarning(
                "ARBrightnessController: ARCameraBackground was not found.");
            return false;
        }

        // This shader is based on the ARKit shader supplied by the user.
        // Do not use the ARCore shader on iOS.
        Shader shader =
            Shader.Find("Unlit/ARKitBackgroundWithBlackFilter");

        if (shader == null)
        {
            Debug.LogWarning(
                "ARBrightnessController: Unlit/ARKitBackgroundWithBlackFilter " +
                "was not found. Add the shader to Always Included Shaders.");
            return false;
        }

        if (runtimeBackgroundMaterial != null)
        {
            Destroy(runtimeBackgroundMaterial);
        }

        runtimeBackgroundMaterial = new Material(shader);
        runtimeBackgroundMaterial.name =
            "ARKit Camera Background Filter (Runtime)";

        // This project uses URP. Explicitly enable the ARKit URP shader variant;
        // otherwise the legacy branch can be selected and render only a small quad.
        runtimeBackgroundMaterial.EnableKeyword("ARKIT_BACKGROUND_URP");

        cameraBackground.customMaterial = runtimeBackgroundMaterial;
        cameraBackground.useCustomMaterial = true;

        Debug.Log(
            "[ARFilter] iOS setup: camera=" + targetCamera.name +
            ", backgroundEnabled=" + cameraBackground.enabled +
            ", useCustomMaterial=" + cameraBackground.useCustomMaterial +
            ", shader=" + runtimeBackgroundMaterial.shader.name +
            ", ARKIT_BACKGROUND_URP=" +
            runtimeBackgroundMaterial.IsKeywordEnabled("ARKIT_BACKGROUND_URP") +
            ", renderMode=" + GetCameraBackgroundRenderMode());
        return true;
    }

    private string GetCameraBackgroundRenderMode()
    {
        ARCameraManager manager = targetCamera != null
            ? targetCamera.GetComponent<ARCameraManager>()
            : null;
        return manager != null
            ? manager.requestedBackgroundRenderingMode.ToString()
            : "ARCameraManager missing";
    }
#endif
}
