using UnityEngine;
using UnityEngine.InputSystem;

public class FpsDisplay : MonoBehaviour
{
    [SerializeField] Vector2 margin = new Vector2(16f, 16f);
    [SerializeField] int fontSize = 24;
    [SerializeField] Color textColor = Color.white;
    [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] float smoothing = 0.1f;

    float smoothedDeltaTime;
    bool isVisible = true;
    GUIStyle textStyle;
    GUIStyle shadowStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateDisplay()
    {
        if (FindAnyObjectByType<FpsDisplay>() != null)
        {
            return;
        }

        GameObject display = new GameObject("FPS Display");
        DontDestroyOnLoad(display);
        display.AddComponent<FpsDisplay>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        smoothedDeltaTime = Time.unscaledDeltaTime;
    }

    void Update()
    {
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            isVisible = !isVisible;
        }

        float blend = Mathf.Clamp01(smoothing);
        smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * blend;
    }

    void OnGUI()
    {
        if (!isVisible)
        {
            return;
        }

        EnsureStyles();

        float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
        float ms = smoothedDeltaTime * 1000f;
        string label = $"FPS: {fps:0} ({ms:0.0} ms)";

        Rect area = new Rect(margin.x, margin.y, 220f, 40f);
        GUI.Label(new Rect(area.x + 2f, area.y + 2f, area.width, area.height), label, shadowStyle);
        GUI.Label(area, label, textStyle);
    }

    void EnsureStyles()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = textColor;
        }

        if (shadowStyle == null)
        {
            shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor = shadowColor;
        }
    }
}
