using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Applies an Android-only black filter to the AR camera background.
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
    private bool useSolidBlackBackgroundForAndroidTest = true;

    [Header("Runtime UI")]
    [SerializeField]
    private bool createRuntimeSlider = true;

    [SerializeField]
    private bool createResetButton = true;

    private Text filterLabel = null;
    private GameObject runtimeCanvasObject = null;
    private ARCameraBackground cameraBackground = null;
    private Material runtimeBackgroundMaterial = null;

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

        if (filterLabel != null)
        {
            filterLabel.text =
                "Dark filter " +
                Mathf.RoundToInt(clampedOpacity * 100f) +
                "%";
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
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
        panelRect.anchoredPosition = new Vector2(24f, 24f);
        panelRect.sizeDelta = new Vector2(520f, 220f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);
        panelImage.raycastTarget = true;

        filterLabel = CreateText(
            panelObject.transform,
            "Dark filter 55%",
            24,
            new Vector2(28f, 112f),
            new Vector2(460f, 34f));

        CreateSlider(panelObject.transform);

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
            "Brightness Slider",
            typeof(RectTransform),
            typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.zero;
        sliderRect.pivot = Vector2.zero;
        sliderRect.anchoredPosition = new Vector2(30f, 52f);
        sliderRect.sizeDelta = new Vector2(460f, 42f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = minimumFilterOpacity;
        slider.maxValue = maximumFilterOpacity;
        slider.value = initialFilterOpacity;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject trackObject = new GameObject(
            "Track",
            typeof(RectTransform),
            typeof(Image));
        trackObject.transform.SetParent(sliderObject.transform, false);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.offsetMin = new Vector2(0f, -8f);
        trackRect.offsetMax = new Vector2(0f, 8f);
        Image trackImage = trackObject.GetComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.35f);
        trackImage.raycastTarget = true;

        GameObject fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(Image));
        fillObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(1f, 0.5f);
        fillRect.offsetMin = new Vector2(0f, -8f);
        fillRect.offsetMax = new Vector2(0f, 8f);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.35f, 0.75f, 1f, 0.9f);
        fillImage.raycastTarget = false;

        GameObject handleObject = new GameObject(
            "Handle",
            typeof(RectTransform),
            typeof(Image));
        handleObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(34f, 42f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;
        handleImage.raycastTarget = true;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(SetFilterOpacity);
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
        buttonRect.anchoredPosition = new Vector2(30f, 8f);
        buttonRect.sizeDelta = new Vector2(460f, 34f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.45f, 0.75f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ResetSensorAndAstronomy);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.45f, 0.75f, 0.95f);
        colors.highlightedColor = new Color(0.25f, 0.6f, 0.9f, 1f);
        colors.pressedColor = new Color(0.08f, 0.3f, 0.55f, 1f);
        button.colors = colors;

        Text label = CreateText(
            buttonObject.transform,
            "Re-sync GPS / time",
            20,
            Vector2.zero,
            buttonRect.sizeDelta);
        label.alignment = TextAnchor.MiddleCenter;
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
}
