
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class PlaytestLogExporter
{
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

    public static void Export(string jsonlPath, string sessionId)
    {
        if (string.IsNullOrEmpty(jsonlPath) || !File.Exists(jsonlPath))
        {
            Debug.LogWarning(
                "[PlaytestLogExporter] Raw log file was not found.");
            return;
        }

        try
        {
            List<RecordedEvent> events = new List<RecordedEvent>();
            int invalidLines = 0;

            foreach (string line in File.ReadLines(jsonlPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    RecordedEvent entry =
                        JsonUtility.FromJson<RecordedEvent>(line);

                    if (entry != null)
                        events.Add(entry);
                    else
                        invalidLines++;
                }
                catch (Exception)
                {
                    invalidLines++;
                }
            }

            string folder = Path.GetDirectoryName(jsonlPath);
            string reportPath = Path.Combine(
                folder, $"session_{sessionId}_REPORT.txt");
            string csvPath = Path.Combine(
                folder, $"session_{sessionId}_EVENTS.csv");

            WriteReport(reportPath, sessionId, events, invalidLines);
            WriteCsv(csvPath, events);

            Debug.Log(
                $"[PlaytestLogExporter] Export complete. " +
                $"Events={events.Count}; Report={reportPath}; CSV={csvPath}");
        }
        catch (Exception exception)
        {
            // Avoid Debug.LogError here: Unity log capture may send this
            // message back into the recorder during application shutdown.
            Debug.LogWarning(
                $"[PlaytestLogExporter] Export failed: {exception.Message}");
        }
    }

    private static void WriteReport(
        string path,
        string sessionId,
        List<RecordedEvent> events,
        int invalidLines)
    {
        StringBuilder sb = new StringBuilder();

        float duration = 0f;
        int warningCount = 0;
        int errorCount = 0;
        int fpsCount = 0;
        float fpsTotal = 0f;

        foreach (RecordedEvent entry in events)
        {
            duration = Mathf.Max(duration, entry.elapsedSeconds);

            if (entry.category != null &&
                entry.category.IndexOf(
                    "Warning", StringComparison.OrdinalIgnoreCase) >= 0)
                warningCount++;

            if (entry.category != null &&
                (entry.category.IndexOf(
                    "Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 entry.category.IndexOf(
                    "Exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 entry.category.IndexOf(
                    "Assert", StringComparison.OrdinalIgnoreCase) >= 0))
                errorCount++;

            if (entry.category == "Performance" &&
                entry.message == "FPS sample")
            {
                string fpsText = FindDetailValue(
                    entry.details, "AverageFPS");

                if (float.TryParse(
                    fpsText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float fps))
                {
                    fpsTotal += fps;
                    fpsCount++;
                }
            }
        }

        sb.AppendLine("STEAMCHASING PLAYTEST REPORT");
        sb.AppendLine("============================");
        sb.AppendLine($"Session ID: {sessionId}");
        sb.AppendLine($"Exported UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Duration: {FormatDuration(duration)}");
        sb.AppendLine($"Total events: {events.Count}");
        sb.AppendLine($"Warnings: {warningCount}");
        sb.AppendLine($"Errors / exceptions / asserts: {errorCount}");
        sb.AppendLine(
            $"Average of FPS samples: " +
            (fpsCount > 0
                ? $"{(fpsTotal / fpsCount):F1} FPS ({fpsCount} samples)"
                : "No FPS samples recorded"));
        sb.AppendLine($"Unreadable log lines: {invalidLines}");
        sb.AppendLine();
        sb.AppendLine("EVENT TIMELINE");
        sb.AppendLine("--------------");

        foreach (RecordedEvent entry in events)
        {
            sb.AppendLine(
                $"[{FormatDuration(entry.elapsedSeconds)}] " +
                $"#{entry.eventIndex} [{entry.category}] {entry.message}");

            if (!string.IsNullOrWhiteSpace(entry.details))
                sb.AppendLine($"    Details: {entry.details}");
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteCsv(
        string path, List<RecordedEvent> events)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(
            "SessionId,EventIndex,UtcTime,ElapsedSeconds," +
            "Category,Message,Details");

        foreach (RecordedEvent entry in events)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(entry.sessionId),
                Csv(entry.eventIndex.ToString(
                    CultureInfo.InvariantCulture)),
                Csv(entry.utcTime),
                Csv(entry.elapsedSeconds.ToString(
                    "F3", CultureInfo.InvariantCulture)),
                Csv(entry.category),
                Csv(entry.message),
                Csv(entry.details)
            }));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string Csv(string value)
    {
        value = value ?? "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string FindDetailValue(string details, string key)
    {
        if (string.IsNullOrEmpty(details))
            return "";

        foreach (string part in details.Split(';'))
        {
            string[] pair = part.Trim().Split(new[] { '=' }, 2);

            if (pair.Length == 2 &&
                pair[0].Trim() == key)
                return pair[1].Trim();
        }

        return "";
    }

    private static string FormatDuration(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0f, seconds));
        return time.TotalHours >= 1
            ? time.ToString(@"hh\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }
}
