
using TMPro;
using UnityEngine;

public class SessionIdDisplay : MonoBehaviour // Attach to game menu txt
{
    [SerializeField] private TMP_Text sessionIdText;

    private void Start()
    {
        if (sessionIdText == null)
        {
            Debug.LogWarning(
                "[SessionIdDisplay] Session ID text is not assigned.");
            return;
        }

        if (GameEventRecorder.Instance == null)
        {
            sessionIdText.text = "Session ID: unavailable";
            Debug.LogWarning(
                "[SessionIdDisplay] GameEventRecorder is missing.");
            return;
        }

        sessionIdText.text =
            $"Session ID: {GameEventRecorder.Instance.SessionId}";

        Debug.Log(
            $"[SessionIdDisplay] Displaying session " +
            $"{GameEventRecorder.Instance.SessionId}");
    }
}
