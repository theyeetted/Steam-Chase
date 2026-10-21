
using System;
using System.IO;
using UnityEngine;

public class GameEventRecorder : MonoBehaviour
{
    public static GameEventRecorder Instance { get; private set; }

    [Header("Session")]
    [SerializeField] private bool logPlayerActions = true;
    [SerializeField] private float fpsSampleInterval = 5f;

    public string SessionId { get; private set; }
    public float ElapsedSeconds => Time.realtimeSinceStartup - sessionStartTime;

    public event Action<string> OnCombatMessage;

    private float sessionStartTime;
    private float fpsTimer;
    private int sampledFrames;
    private StreamWriter logWriter;
    private int eventIndex;
    private string logFilePath;
    private bool isHandlingUnityLog;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const bool DeveloperRecordingEnabled = true;
#else
    private const bool DeveloperRecordingEnabled = false;
#endif

    [Serializable]
    private class RecordedEvent
    {
        public string sessionId;
        public int eventIndex;
        public string utcTime;
        public float elapsedSeconds;
        public string category;
        public string message;
        public string details;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Recorder] Duplicate recorder destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SessionId =  DateTime.Now.ToString("HHmm") + UnityEngine.Random.Range(0, 100).ToString("D2");
        sessionStartTime = Time.realtimeSinceStartup;

        if (DeveloperRecordingEnabled)
            OpenLogFile();

        Record("Session", "Session started",
            $"Unity={Application.unityVersion}; Platform={Application.platform}");

        // Generates a session ID when the game starts.
        Debug.Log($"[Recorder] Session started: {SessionId}");
    }

    private void Update()
    {
        if (!DeveloperRecordingEnabled)
            return;

        fpsTimer += Time.unscaledDeltaTime;
        sampledFrames++;

        if (fpsTimer < fpsSampleInterval)
            return;

        float averageFps = sampledFrames / Mathf.Max(fpsTimer, 0.001f);

        Record("Performance", "FPS sample",
            $"AverageFPS={averageFps:F1}; SampleSeconds={fpsTimer:F1}");

        fpsTimer = 0f;
        sampledFrames = 0;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Application.logMessageReceived += HandleUnityLog;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Application.logMessageReceived -= HandleUnityLog;
#endif
    }

    public void RecordCombat(string playerMessage, string details = "")
    {
        if (string.IsNullOrWhiteSpace(playerMessage))
            return;

        // Player-facing message. Keep this short and understandable.
        OnCombatMessage?.Invoke(playerMessage);

        // Detailed data is saved only in developer sessions.
        Record("Combat", playerMessage, details);
    }

    public void RecordPlayerAction(string action, string details = "")
    {
        if (!logPlayerActions || string.IsNullOrWhiteSpace(action))
            return;

        Record("PlayerAction", action, details);
    }

    public void RecordDebug(string message, string details = "")
    {
        Record("Debug", message, details);
    }

    public void RecordWarning(string message, string details = "")
    {
        Record("Warning", message, details);
    }

    private void Record(string category, string message, string details)
    {
        if (!DeveloperRecordingEnabled)
            return;

        if (logWriter == null)
            return;

        RecordedEvent entry = new RecordedEvent
        {
            sessionId = SessionId,
            eventIndex = ++eventIndex,
            utcTime = DateTime.UtcNow.ToString("o"),
            elapsedSeconds = ElapsedSeconds,
            category = category,
            message = message,
            details = details
        };

        try
        {
            logWriter.WriteLine(JsonUtility.ToJson(entry));
            logWriter.Flush();
        }
        catch (Exception exception)
        {
            // Avoid Debug.LogError here to prevent recursive logging
            // if the recorder cannot write to its own file.
            Debug.LogError($"[Recorder] Failed to write event: {exception.Message}");
        }
    }

    private void OpenLogFile()
    {
        try
        {
            string folder = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "PlaytestLogs");

            /*
            string folder = Path.Combine(
                Application.persistentDataPath,
                "PlaytestLogs"); */ // OLD path

            Directory.CreateDirectory(folder);

            logFilePath = Path.Combine(
                folder, $"session_{SessionId}.jsonl");

            logWriter = new StreamWriter(logFilePath, false);
            logWriter.AutoFlush = false;

            Debug.Log($"[Recorder] Writing playtest log to: {logFilePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Recorder] Could not open log file: {exception.Message}");
        }
    }

// Collect Unity Debug Log
#if UNITY_EDITOR || DEVELOPMENT_BUILD
private void HandleUnityLog(
    string condition,
    string stackTrace,
    LogType type)
{
    // Prevent recorder-generated errors from causing a logging loop.
    if (isHandlingUnityLog)
        return;

    isHandlingUnityLog = true;

    try
    {
        // Stack traces are most useful for warnings and serious errors.
        string details = "";

        if (type != LogType.Log)
            details = stackTrace;

        Record(
            "UnityConsole/" + type,
            condition,
            details);
    }
    finally
    {
        isHandlingUnityLog = false;
    }
}
#endif

    private void OnApplicationQuit()
    {
        Record("Session", "Session ended",
            $"DurationSeconds={ElapsedSeconds:F1}");
    // Ensure the raw JSONL file is fully written before exporting.
    try
    {
        logWriter?.Flush();
        logWriter?.Dispose();
        logWriter = null;

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!string.IsNullOrEmpty(logFilePath))
                PlaytestLogExporter.Export(logFilePath, SessionId);
    #endif
    }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[Recorder] Shutdown logging issue: {exception.Message}");
        }

        if (Instance == this)
            Instance = null;
    }
}
