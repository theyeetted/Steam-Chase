using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : SingleTon<GameManager>
{
    public enum FlowState { StartMenu, DifficultiesMenu, DeckChooseMenu, Combat, Paused, Victory, Defeat }
    public enum Difficulty { Normal, Elite }

    [Header("Menus (resolved under -----UI-----/Canvas when unassigned)")]
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject difficultiesMenu;
    [SerializeField] private GameObject deckChooseMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject winMenu;
    [SerializeField] private GameObject loseMenu;
    [SerializeField] private GameObject hud;
    [SerializeField] private MatchSetupSystem matchSetup;

    [Header("Objects visible during gameplay")]
    [Tooltip("Optional gameplay view roots. Keep managers, menus, EventSystem and system objects active. Views remain active while paused so their coroutines can resume.")]
    [SerializeField] private GameObject[] inGameObjects = new GameObject[0];

    public FlowState State { get; private set; } = FlowState.StartMenu;
    public bool IsPlaying => State == FlowState.Combat;
    public bool IsPaused => State == FlowState.Paused;
    public Difficulty SelectedDifficulty { get; private set; } = Difficulty.Normal;
    public int SelectedWhiskeyDeck { get; private set; }
    public int SelectedAdeDeck { get; private set; }

    private bool reloading;
    private UnityEngine.UI.Button confirmButton;
    private readonly UnityEngine.UI.Button[] whiskeyButtons = new UnityEngine.UI.Button[3];
    private readonly UnityEngine.UI.Button[] adeButtons = new UnityEngine.UI.Button[3];
    private readonly List<(UnityEngine.UI.Button button, UnityAction callback)> buttonBindings = new();

    // Only scene reloads within this play session retain selections.
    private sealed class ReloadRequest
    {
        public string ScenePath;
        public FlowState Destination;
        public Difficulty Difficulty;
        public int WhiskeyDeck;
        public int AdeDeck;
    }
    private static ReloadRequest pendingReload;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRestartState() => pendingReload = null;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        ResolveReferences();
        BindMenus();
        SetState(FlowState.StartMenu);
    }

    private void Start()
    {
        // Wait until every system's Awake has run before restoring a fight.
        ReloadRequest request = pendingReload;
        pendingReload = null;
        if (request != null && request.ScenePath == gameObject.scene.path)
        {
            SelectedDifficulty = request.Difficulty;
            SelectedWhiskeyDeck = request.WhiskeyDeck;
            SelectedAdeDeck = request.AdeDeck;
            if (request.Destination != FlowState.StartMenu)
            {
                SetState(FlowState.DeckChooseMenu);
                if (request.Destination == FlowState.Combat) ConfirmDecks();
            }
        }
        RefreshDeckButtons();
    }

    private void Update()
    {
        if (reloading) return;
        if (EscapePressed())
        {
            if (IsPaused) ResumeGame();
            else if (IsPlaying) PauseGame();
        }
        if (!IsPlaying || matchSetup == null || !matchSetup.IsSetupComplete) return;
        HeroSystem heroes = HeroSystem.Instance;
        if (heroes == null || EnemySystem.Instance == null) return;

        // The existing combat rules allow one hero to survive the other's defeat.
        bool firstAlive = heroes.playerView != null && heroes.playerView.CurrentHealth > 0;
        bool secondAlive = heroes.secondPlayerView != null && heroes.secondPlayerView.CurrentHealth > 0;
        if (!firstAlive && !secondAlive)
        {
            LoseGame();
            return;
        }
        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0) return;
        }
        WinGame();
    }

    // Retains the old Start button entry point, now opening difficulty selection.
    public void StartGame()
    {
        if (State == FlowState.StartMenu && !reloading) SetState(FlowState.DifficultiesMenu);
    }

    public void BackToMainMenu()
    {
        if (State == FlowState.DifficultiesMenu && !reloading) SetState(FlowState.StartMenu);
    }

    public void SelectNormalDifficulty() => SelectDifficulty(Difficulty.Normal);
    public void SelectEliteDifficulty() => SelectDifficulty(Difficulty.Elite);

    private void SelectDifficulty(Difficulty difficulty)
    {
        if (State != FlowState.DifficultiesMenu || reloading) return;
        SelectedDifficulty = difficulty;
        SetState(FlowState.DeckChooseMenu);
        RefreshDeckButtons();
    }

    public void BackToDifficultiesMenu()
    {
        if (State == FlowState.DeckChooseMenu && !reloading) SetState(FlowState.DifficultiesMenu);
    }

    // Button/Inspector arguments are one-based: 1, 2, or 3. Zero means unselected.
    public void SelectWhiskeyDeck(int deckNumber)
    {
        if (State != FlowState.DeckChooseMenu || reloading || matchSetup == null ||
            !matchSetup.IsDeckAvailable(0, deckNumber)) return;
        SelectedWhiskeyDeck = deckNumber;
        RefreshDeckButtons();
    }

    public void SelectAdeDeck(int deckNumber)
    {
        if (State != FlowState.DeckChooseMenu || reloading || matchSetup == null ||
            !matchSetup.IsDeckAvailable(1, deckNumber)) return;
        SelectedAdeDeck = deckNumber;
        RefreshDeckButtons();
    }

    public void ConfirmDecks()
    {
        if (State != FlowState.DeckChooseMenu || reloading) return;
        if (matchSetup == null)
        {
            Debug.LogError("GameManager needs an active MatchSetupSystem.", this);
            return;
        }
        if (!matchSetup.ConfigureMatch(SelectedDifficulty, SelectedWhiskeyDeck, SelectedAdeDeck, out string error))
        {
            Debug.LogError(error, matchSetup);
            return;
        }
        SetState(FlowState.Combat);
    }

    public void PauseGame()
    {
        if (!IsPlaying || reloading) return;
        if (pauseMenu == null)
        {
            Debug.LogError("Assign PauseMenu before pausing combat.", this);
            return;
        }
        CancelCardInteraction();
        SetState(FlowState.Paused);
    }

    public void ResumeGame()
    {
        if (IsPaused && !reloading) SetState(FlowState.Combat);
    }

    public void WinGame() => EndGame(FlowState.Victory);
    public void LoseGame() => EndGame(FlowState.Defeat);

    public void RestartGame()
    {
        if (CanLeaveCombat()) ReloadScene(FlowState.Combat);
    }

    public void ChooseDeck()
    {
        if (CanLeaveCombat()) ReloadScene(FlowState.DeckChooseMenu);
    }

    public void MainMenu()
    {
        if (CanLeaveCombat()) ReloadScene(FlowState.StartMenu);
        else if (!reloading)
        {
            SelectedWhiskeyDeck = SelectedAdeDeck = 0;
            SelectedDifficulty = Difficulty.Normal;
            SetState(FlowState.StartMenu);
        }
    }

    public void ExitGame()
    {
        pendingReload = null;
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool CanLeaveCombat() => IsPlaying || IsPaused || State == FlowState.Victory || State == FlowState.Defeat;

    private void ReloadScene(FlowState destination)
    {
        if (reloading) return;
        Scene scene = gameObject.scene;
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("Save the scene before restarting the game.", this);
            return;
        }
#if !UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(scene.path))
        {
            Debug.LogError("Add the combat scene to the build scene list before restarting.", this);
            return;
        }
#endif
        pendingReload = new ReloadRequest
        {
            ScenePath = scene.path,
            Destination = destination,
            Difficulty = destination == FlowState.StartMenu ? Difficulty.Normal : SelectedDifficulty,
            WhiskeyDeck = destination == FlowState.StartMenu ? 0 : SelectedWhiskeyDeck,
            AdeDeck = destination == FlowState.StartMenu ? 0 : SelectedAdeDeck
        };
        reloading = true;
        ActionSystem.Instance?.StopActions();
        CancelCardInteraction();
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.path);
#endif
    }

    private void EndGame(FlowState result)
    {
        if ((!IsPlaying && !IsPaused) || reloading) return;
        ActionSystem.Instance?.StopActions();
        CancelCardInteraction();
        SetState(result);
    }

    private void SetState(FlowState state)
    {
        State = state;
        Time.timeScale = IsPlaying ? 1f : 0f;
        // Deactivating gameplay views during pause would discard their coroutines.
        foreach (GameObject item in inGameObjects) SetActive(item, IsPlaying || IsPaused);
        SetActive(hud, IsPlaying);
        SetActive(startMenu, state == FlowState.StartMenu);
        SetActive(difficultiesMenu, state == FlowState.DifficultiesMenu);
        SetActive(deckChooseMenu, state == FlowState.DeckChooseMenu);
        SetActive(pauseMenu, IsPaused);
        SetActive(winMenu, state == FlowState.Victory);
        SetActive(loseMenu, state == FlowState.Defeat);
    }

    private static void CancelCardInteraction()
    {
        foreach (CardView card in FindObjectsByType<CardView>(FindObjectsSortMode.None)) card.CancelInteraction();
        ManualTargetSystem.Instance?.CancelTargeting();
        CardViewHoverSystem.Instance?.Hide();
    }

    private void RefreshDeckButtons()
    {
        for (int i = 0; i < 3; i++)
        {
            if (whiskeyButtons[i] != null)
                whiskeyButtons[i].interactable = matchSetup != null && matchSetup.IsDeckAvailable(0, i + 1);
            if (adeButtons[i] != null)
                adeButtons[i].interactable = matchSetup != null && matchSetup.IsDeckAvailable(1, i + 1);
        }
        if (confirmButton != null)
            confirmButton.interactable = matchSetup != null &&
                matchSetup.CanConfigureMatch(SelectedDifficulty, SelectedWhiskeyDeck, SelectedAdeDeck, out _);
    }

    private void ResolveReferences()
    {
        if (matchSetup == null) matchSetup = FindFirstObjectByType<MatchSetupSystem>();
        Transform canvas = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name != "-----UI-----") continue;
            canvas = root.transform.Find("Canvas");
            break;
        }
        if (canvas == null) return;
        startMenu = ResolveMenu(startMenu, canvas, "StartMenu");
        difficultiesMenu = ResolveMenu(difficultiesMenu, canvas, "DifficultiesMenu");
        deckChooseMenu = ResolveMenu(deckChooseMenu, canvas, "DeckChooseMenu");
        pauseMenu = ResolveMenu(pauseMenu, canvas, "PauseMenu");
        winMenu = ResolveMenu(winMenu, canvas, "WinMenu");
        loseMenu = ResolveMenu(loseMenu, canvas, "GameOverUI");
        hud = ResolveMenu(hud, canvas, "HUD");
    }

    private static GameObject ResolveMenu(GameObject assigned, Transform canvas, string name)
    {
        if (assigned != null) return assigned;
        Transform child = canvas.Find(name);
        return child != null ? child.gameObject : null;
    }

    private void BindMenus()
    {
        BindButton(startMenu, StartGame, "StartButton", "Start");
        BindButton(startMenu, ExitGame, "ExitButton", "Exit");
        BindButton(difficultiesMenu, BackToMainMenu, "Back", "BackButton");
        BindButton(difficultiesMenu, SelectNormalDifficulty, "Normal");
        BindButton(difficultiesMenu, SelectEliteDifficulty, "Elite");
        BindButton(deckChooseMenu, BackToDifficultiesMenu, "Back", "BackButton");
        confirmButton = BindButton(deckChooseMenu, ConfirmDecks, "Confirm", "ConfirmButton");
        for (int number = 1; number <= 3; number++)
        {
            int deckNumber = number;
            whiskeyButtons[number - 1] = BindButton(deckChooseMenu, () => SelectWhiskeyDeck(deckNumber), $"Whiskey/Deck{number}");
            adeButtons[number - 1] = BindButton(deckChooseMenu, () => SelectAdeDeck(deckNumber), $"Ade/Deck{number}");
        }
        BindButton(hud, PauseGame, "Pause", "PauseButton");
        BindButton(pauseMenu, ResumeGame, "Resume");
        BindCombatMenu(pauseMenu);
        BindCombatMenu(winMenu);
        BindCombatMenu(loseMenu);
        // EndTurn and its Inspector listeners belong to the existing turn system.
    }

    private void BindCombatMenu(GameObject menu)
    {
        BindButton(menu, RestartGame, "Restart");
        BindButton(menu, ChooseDeck, "Choose Deck", "ChooseDeck");
        BindButton(menu, MainMenu, "Mainmenu", "MainMenu");
        BindButton(menu, ExitGame, "Exit");
    }

    private UnityEngine.UI.Button BindButton(GameObject menu, UnityAction callback, params string[] paths)
    {
        if (menu == null) return null;
        foreach (string path in paths)
        {
            Transform child = menu.transform.Find(path);
            if (child == null || !child.TryGetComponent(out UnityEngine.UI.Button button)) continue;
            // Replace only old flow listeners at runtime, including Resume -> RestartFight.
            // Unrelated effects/listeners and all EndTurn listeners remain intact.
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                Object target = button.onClick.GetPersistentTarget(i);
                if (target == this || (target is MatchSetupSystem && button.onClick.GetPersistentMethodName(i) == "RestartFight"))
                    button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
            button.onClick.AddListener(callback);
            buttonBindings.Add((button, callback));
            return button;
        }
        Debug.LogWarning($"{menu.name} needs a Button at '{paths[0]}' for {callback.Method.Name}.", menu);
        return null;
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
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
