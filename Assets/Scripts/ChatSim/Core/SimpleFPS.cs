using UnityEngine;

public class SimpleFPS : MonoBehaviour
{
    public static SimpleFPS Instance;

    float deltaTime;
    bool show = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Toggle (optional)
        if (Input.GetKeyDown(KeyCode.F3))
            show = !show;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!show) return;

        int h = Screen.height;

        GUIStyle style = new GUIStyle();
        style.fontSize = h * 2 / 50;
        style.normal.textColor = Color.white;

        float ms = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;

        GUI.Label(new Rect(10, 10, 300, 40),
            $"FPS: {Mathf.Ceil(fps)} | {ms:0.0} ms",
            style);
    }
}