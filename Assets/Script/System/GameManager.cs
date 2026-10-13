using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : SingleTon<GameManager>
{
    [Header("Menus")]
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject winMenu;
    [SerializeField] private GameObject loseMenu;

    [Header("Objects visible during gameplay")]
    [Tooltip("Assign EndTurn, EnergyUI, PerksUI and any gameplay view roots. Keep this manager, the menus, EventSystem and system objects active.")]
    [SerializeField] private GameObject[] inGameObjects = new GameObject[0];

    public bool IsPlaying { get; private set; }

    private static bool startAfterRestart;
    private bool gameEnded;
    private readonly List<(UnityEngine.UI.Button button, UnityAction callback)> buttonBindings = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRestartState()
    {
        startAfterRestart = false;
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        BindButton(startMenu, "Start", StartGame);
        BindButton(startMenu, "Exit", ExitGame);
        BindButton(winMenu, "Restart", RestartGame);
        BindButton(winMenu, "Exit", ExitGame);
        BindButton(loseMenu, "Restart", RestartGame);
        BindButton(loseMenu, "Exit", ExitGame);

        if (startAfterRestart)
        {
            startAfterRestart = false;
            StartGame();
        }
        else
        {
            ShowMenu(startMenu);
        }
    }

    private void Update()
    {
        if (!IsPlaying) return;

        PlayerView player = HeroSystem.Instance != null ? HeroSystem.Instance.playerView : null;
        // MatchSetupSystem initializes health and enemies after Start is pressed.
        if (player == null || player.MaxHealth <= 0 || EnemySystem.Instance == null) return;

        // Apply damage directly so these shortcuts also work during an action.
        if (Input.GetKeyDown(KeyCode.W))
        {
            foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
            {
                if (enemy != null && enemy.CurrentHealth > 0) enemy.Damage(999);
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            player.Damage(999);
        }

        if (player.CurrentHealth <= 0)
        {
            LoseGame();
            return;
        }

        // Check health as well as the list, so lethal shock damage also wins.
        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0) return;
        }

        WinGame();
    }

    public void StartGame()
    {
        if (IsPlaying || gameEnded) return;

        IsPlaying = true;
        SetActive(startMenu, false);
        SetActive(winMenu, false);
        SetActive(loseMenu, false);
        SetInGameObjectsActive(true);
        Time.timeScale = 1f;
    }

    public void WinGame()
    {
        EndGame(winMenu);
    }

    public void LoseGame()
    {
        EndGame(loseMenu);
    }

    public void RestartGame()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("Save the scene before restarting the game.", this);
            return;
        }

        startAfterRestart = true;
        Time.timeScale = 1f;
#if UNITY_EDITOR
        // Also supports testing a scene that is not in the build's scene list yet.
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.path);
#endif
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EndGame(GameObject menu)
    {
        if (!IsPlaying || gameEnded) return;

        gameEnded = true;
        IsPlaying = false;
        // Coroutines yielding null can continue even when timeScale is zero.
        if (ActionSystem.Instance != null) ActionSystem.Instance.StopActions();
        if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
        ShowMenu(menu);
    }

    private void ShowMenu(GameObject menu)
    {
        Time.timeScale = 0f;
        SetInGameObjectsActive(false);
        SetActive(startMenu, menu == startMenu);
        SetActive(winMenu, menu == winMenu);
        SetActive(loseMenu, menu == loseMenu);
    }

    private void SetInGameObjectsActive(bool active)
    {
        foreach (GameObject item in inGameObjects)
        {
            SetActive(item, active);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private void BindButton(GameObject menu, string childName, UnityAction callback)
    {
        if (menu == null) return;

        Transform child = menu.transform.Find(childName);
        if (child == null || !child.TryGetComponent(out UnityEngine.UI.Button button))
        {
            Debug.LogWarning($"{menu.name}/{childName} needs a Button component.", menu);
            return;
        }

        // An existing Inspector hookup to this method should only fire once.
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == callback.Method.Name) return;
        }

        button.onClick.AddListener(callback);
        buttonBindings.Add((button, callback));
    }

    private void OnDestroy()
    {
        foreach (var binding in buttonBindings)
        {
            if (binding.button != null) binding.button.onClick.RemoveListener(binding.callback);
        }

        if (Instance == this) Time.timeScale = 1f;
    }
}
