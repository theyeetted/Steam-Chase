using System.Collections;
using TMPro;
using UnityEngine;

public class CombatFeedUI : MonoBehaviour
{
    [Header("Notification Settings")]
    [SerializeField] private GameObject playerFeedPanel;
    [SerializeField] private TMP_Text feedText;
    [SerializeField, Min(0.1f)] private float displayDuration = 3f;

    private GameEventRecorder subscribedRecorder;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (playerFeedPanel != null)
            playerFeedPanel.SetActive(false);
        else
            Debug.LogWarning("[CombatFeedUI] Notification panel initialized as hidden.");
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedRecorder != null)
            subscribedRecorder.OnCombatMessage -= AddMessage;

        subscribedRecorder = null;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    private void TrySubscribe()
    {
        if (subscribedRecorder != null)
            return;

        if (GameEventRecorder.Instance == null)
        {
            Debug.LogWarning("[CombatFeedUI] GameEventRecorder not found yet.");
            return;
        }

        subscribedRecorder = GameEventRecorder.Instance;
        subscribedRecorder.OnCombatMessage += AddMessage;

        Debug.Log("[CombatFeedUI] Connected to game event recorder.");
    }

    private void AddMessage(string message)
    {
        if (playerFeedPanel == null || feedText == null)
        {
            Debug.LogWarning(
                "[CombatFeedUI] Assign both PlayerFeedPanel and Feed Text in the Inspector.");
            return;
        }

        // Show the panel and display the newest notification.
        feedText.text = message;
        playerFeedPanel.SetActive(true);

        // Restart the timer if another notification arrives.
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay());

        Debug.Log($"[CombatFeedUI] Notification shown: {message}");
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        if (playerFeedPanel != null)
            playerFeedPanel.SetActive(false);

        if (feedText != null)
            feedText.text = "";

        hideCoroutine = null;

        Debug.Log("[CombatFeedUI] Notification panel hidden.");
    }
}