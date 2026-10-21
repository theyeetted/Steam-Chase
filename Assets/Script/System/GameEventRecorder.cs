using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

public class GameEventRecorder : MonoBehaviour
{
    public static GameEventRecorder Instance { get; private set; }

    [Header("Session")]
    [SerializeField] private bool logPlayerActions = true;

    [Tooltip("How often performance samples are written.")]
    [SerializeField, Min(0.5f)] private float fpsSampleInterval = 5f;

    [Tooltip("Frame times above this value count as spikes.")]
    [SerializeField, Min(1f)] private float frameSpikeThresholdMs = 33.3f;

    [Tooltip("Print performance samples in the Unity Console.")]
    [SerializeField] private bool printPerformanceToConsole = false;

    public string SessionId { get; private set; }
    public float ElapsedSeconds => Time.realtimeSinceStartup - sessionStartTime;

    public event Action<string> OnCombatMessage;

    private float sessionStartTime;
    private float sampleTimer;
    private float worstFrameTime;
    private float lastAllocatedMemoryMB;
    private float lastReservedMemoryMB;
    private float lastManagedMemoryMB;
    private float lastGcAllocatedKB;

    private int spikeCount;
    private int sampledFrames;
    private int totalFrameSpikes;
    private int eventIndex;

    private readonly List<float> frameTimesMs = new List<float>(600);

    private StreamWriter logWriter;
    private string logFilePath;
    private bool isHandlingUnityLog;

    // Optional Unity Profiler metrics.
    private ProfilerRecorder mainThreadRecorder;
    private ProfilerRecorder renderThreadRecorder;
    private ProfilerRecorder gcAllocatedRecorder;

    private bool mainThreadRecorderStarted;
    private bool renderThreadRecorderStarted;
    private bool gcRecorderStarted;

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

        SessionId =
            DateTime.Now.ToString("HHmm") + UnityEngine.Random.Range(0, 100).ToString("D2");

        sessionStartTime = Time.realtimeSinceStartup;

        if (DeveloperRecordingEnabled)
        {
            OpenLogFile();
            StartProfilerRecorders();
        }

        Record(
            "Session",
            "Session started",
            $"Unity={Application.unityVersion}; " +
            $"Platform={Application.platform}; " +
            $"Device={SystemInfo.deviceModel}; " +
            $"OS={SystemInfo.operatingSystem}; " +
            $"Processor={SystemInfo.processorType}; " +
            $"ProcessorCount={SystemInfo.processorCount}; " +
            $"SystemMemoryMB={SystemInfo.systemMemorySize}; " +
            $"GraphicsDevice={SystemInfo.graphicsDeviceName}; " +
            $"GraphicsMemoryMB={SystemInfo.graphicsMemorySize}; " +
            $"Screen={Screen.width}x{Screen.height}; " +
            $"TargetFrameRate={Application.targetFrameRate}");

        Debug.Log($"[Recorder] Session started: {SessionId}");
    }

    private void StartProfilerRecorders()
    {
        // Profiler markers may not exist on every Unity version/platform.
        try
        {
            mainThreadRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal, "Main Thread", 15);
            mainThreadRecorderStarted = mainThreadRecorder.Valid;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Recorder] Main Thread profiler marker unavailable: " + e.Message);
        }

        try
        {
            renderThreadRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal, "Render Thread", 15);
            renderThreadRecorderStarted = renderThreadRecorder.Valid;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Recorder] Render Thread profiler marker unavailable: " + e.Message);
        }

        try
        {
            gcAllocatedRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory, "GC Allocated In Frame", 15);
            gcRecorderStarted = gcAllocatedRecorder.Valid;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Recorder] GC allocation marker unavailable: " + e.Message);
        }
    }

    private void Update()
    {
        if (!DeveloperRecordingEnabled || logWriter == null)
            return;

        float frameTimeMs = Time.unscaledDeltaTime * 1000f;

        if (frameTimeMs <= 0f)
            return;

        frameTimesMs.Add(frameTimeMs);
        sampledFrames++;

        if (frameTimeMs > worstFrameTime)
            worstFrameTime = frameTimeMs;

        if (frameTimeMs >= frameSpikeThresholdMs)
        {
            spikeCount++;
            totalFrameSpikes++;
        }

        sampleTimer += Time.unscaledDeltaTime;

        if (sampleTimer < Mathf.Max(fpsSampleInterval, 0.5f))
            return;

        WritePerformanceSample();
    }

    private void WritePerformanceSample()
    {
        if (sampledFrames == 0 || frameTimesMs.Count == 0)
            return;

        float elapsedSampleSeconds = sampleTimer;
        float averageFps = sampledFrames / Mathf.Max(elapsedSampleSeconds, 0.001f);

        float averageFrameTimeMs = 0f;
        for (int i = 0; i < frameTimesMs.Count; i++)
            averageFrameTimeMs += frameTimesMs[i];

        averageFrameTimeMs /= frameTimesMs.Count;

        // Approximate 99th percentile frame time.
        // 1% low FPS = 1000 / 99th-percentile frame time in milliseconds.
        frameTimesMs.Sort();

        int p99Index = Mathf.Clamp(
            Mathf.CeilToInt(frameTimesMs.Count * 0.99f) - 1,
            0,
            frameTimesMs.Count - 1);

        float p99FrameTimeMs = frameTimesMs[p99Index];
        float onePercentLowFps = 1000f / Mathf.Max(p99FrameTimeMs, 0.001f);

        float allocatedMemoryMB =
            Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        float reservedMemoryMB =
            Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);

        float managedMemoryMB =
            Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);

        float allocatedMemoryDeltaMB = allocatedMemoryMB - lastAllocatedMemoryMB;
        float reservedMemoryDeltaMB = reservedMemoryMB - lastReservedMemoryMB;
        float managedMemoryDeltaMB = managedMemoryMB - lastManagedMemoryMB;

        float mainThreadMs = mainThreadRecorderStarted
            ? mainThreadRecorder.LastValue / 1000000f : -1f;

        float renderThreadMs = renderThreadRecorderStarted
            ? renderThreadRecorder.LastValue / 1000000f : -1f;

        float gcAllocatedKB = gcRecorderStarted
            ? gcAllocatedRecorder.LastValue / 1024f : -1f;

        float gcAllocatedDeltaKB = gcAllocatedKB >= 0f && lastGcAllocatedKB >= 0f
            ? gcAllocatedKB - lastGcAllocatedKB : -1f;

        string details =
            $"AverageFPS={averageFps:F1}; " +
            $"Approx1PercentLowFPS={onePercentLowFps:F1}; " +
            $"AverageFrameTimeMs={averageFrameTimeMs:F2}; " +
            $"P99FrameTimeMs={p99FrameTimeMs:F2}; " +
            $"WorstFrameTimeMs={worstFrameTime:F2}; " +
            $"FrameSpikesOver{frameSpikeThresholdMs:F1}ms={spikeCount}; " +
            $"TotalFrameSpikes={totalFrameSpikes}; " +
            $"SampledFrames={sampledFrames}; " +
            $"SampleSeconds={elapsedSampleSeconds:F2}; " +
            $"UnityAllocatedMemoryMB={allocatedMemoryMB:F1}; " +
            $"UnityAllocatedMemoryDeltaMB={allocatedMemoryDeltaMB:F1}; " +
            $"UnityReservedMemoryMB={reservedMemoryMB:F1}; " +
            $"UnityReservedMemoryDeltaMB={reservedMemoryDeltaMB:F1}; " +
            $"ManagedMemoryMB={managedMemoryMB:F1}; " +
            $"ManagedMemoryDeltaMB={managedMemoryDeltaMB:F1}; " +
            $"MainThreadMarkerMs={mainThreadMs:F2}; " +
            $"RenderThreadMarkerMs={renderThreadMs:F2}; " +
            $"GCAllocatedInLastFrameKB={gcAllocatedKB:F2}; " +
            $"GCAllocatedDeltaKB={gcAllocatedDeltaKB:F2}";

        Record("Performance", "Performance sample", details);

        if (printPerformanceToConsole)
            Debug.Log("[Recorder] Performance: " + details);

        lastAllocatedMemoryMB = allocatedMemoryMB;
        lastReservedMemoryMB = reservedMemoryMB;
        lastManagedMemoryMB = managedMemoryMB;

        if (gcAllocatedKB >= 0f)
            lastGcAllocatedKB = gcAllocatedKB;

        frameTimesMs.Clear();
        sampledFrames = 0;
        sampleTimer = 0f;
        worstFrameTime = 0f;
        spikeCount = 0;
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

        // This message is shown in the player-facing combat feed.
        OnCombatMessage?.Invoke(playerMessage);
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

    // Call this around important game events to correlate them with performance.
    public void RecordGameplayMarker(string marker, string details = "")
    {
        Record("GameplayMarker", marker, details);
    }

    private void Record(string category, string message, string details)
    {
        if (!DeveloperRecordingEnabled || logWriter == null)
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
            // Buffered writes: do not flush every event.
            logWriter.WriteLine(JsonUtility.ToJson(entry));
        }
        catch (Exception exception)
        {
            Debug.LogError("[Recorder] Failed to write event: " + exception.Message);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        if (isHandlingUnityLog)
            return;

        isHandlingUnityLog = true;

        try
        {
            string details = type == LogType.Log ? "" : stackTrace;
            Record("UnityConsole/" + type, condition, details);
        }
        finally
        {
            isHandlingUnityLog = false;
        }
    }
#endif

    private void OpenLogFile()
    {
        try
        {
            DirectoryInfo parent = Directory.GetParent(Application.dataPath);
            string root = parent != null
                ? parent.FullName
                : Application.persistentDataPath;

            string folder = Path.Combine(root, "PlaytestLogs");
            Directory.CreateDirectory(folder);

            logFilePath = Path.Combine(folder, $"session_{SessionId}.jsonl");

            logWriter = new StreamWriter(logFilePath, false);
            logWriter.AutoFlush = false;

            Debug.Log("[Recorder] Writing playtest log to: " + logFilePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("[Recorder] Could not open log file: " + exception.Message);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            FlushLog();
    }

    private void FlushLog()
    {
        try
        {
            if (logWriter != null)
                logWriter.Flush();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Recorder] Flush issue: " + exception.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if (DeveloperRecordingEnabled && logWriter != null)
        {
            // Save the unfinished interval as well.
            if (sampledFrames > 0)
                WritePerformanceSample();

            Record("Session", "Session ended",
                $"DurationSeconds={ElapsedSeconds:F1}; " +
                $"TotalFrameSpikes={totalFrameSpikes}");

            try
            {
                logWriter.Flush();
                logWriter.Dispose();
                logWriter = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!string.IsNullOrEmpty(logFilePath))
                    PlaytestLogExporter.Export(logFilePath, SessionId);
#endif
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Recorder] Shutdown logging issue: " + exception.Message);
            }
        }

        StopProfilerRecorders();

        if (Instance == this)
            Instance = null;
    }

    private void StopProfilerRecorders()
    {
        if (mainThreadRecorderStarted)
            mainThreadRecorder.Dispose();

        if (renderThreadRecorderStarted)
            renderThreadRecorder.Dispose();

        if (gcRecorderStarted)
            gcAllocatedRecorder.Dispose();

        mainThreadRecorderStarted = false;
        renderThreadRecorderStarted = false;
        gcRecorderStarted = false;
    }

    private void OnDestroy()
    {
        StopProfilerRecorders();

        if (Instance == this)
            Instance = null;
    }
}
