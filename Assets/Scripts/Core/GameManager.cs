// =========================================================================
//  SANCTUARY — GameManager (Controlador Principal)
//  Port de: main_v3.js (init + state machine)
// =========================================================================

using UnityEngine;

namespace Sanctuary.Core
{
    /// <summary>
    /// Singleton principal que controla o fluxo do jogo.
    /// State machine: MainMenu → CharacterCreation → Exploration → Combat → etc.
    /// Referência central para todos os managers.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Estado do Jogo")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        public GameState CurrentState => currentState;

        [Header("Multi-Save (3 Slots — Port de appState)")]
        [SerializeField] private int activeSlotIndex = 0;
        public int ActiveSlotIndex => activeSlotIndex;
        public const int MAX_SAVE_SLOTS = 3;

        [Header("Referências de Managers")]
        public SaveManager saveManager;
        public AudioManager audioManager;

        // Estes serão preenchidos conforme criarmos os scripts
        // public CombatManager combatManager;
        // public HeroManager heroManager;
        // public DungeonManager dungeonManager;
        // public BiomeManager biomeManager;
        // public QuestManager questManager;
        // public BestiaryManager bestiaryManager;
        // public AchievementManager achievementManager;
        // public PantheonManager pantheonManager;
        // public ForgeManager forgeManager;
        // public AlchemyManager alchemyManager;

        // ====================== LIFECYCLE ======================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log("[GameManager] 🏰 Sanctuary inicializando...");

            // Inicializa subsistemas na ordem correta
            if (saveManager == null) saveManager = GetComponentInChildren<SaveManager>();
            if (audioManager == null) audioManager = GetComponentInChildren<AudioManager>();

            // Assina eventos globais
            EventBus.OnHeroDied += HandleHeroDeath;
            EventBus.OnCombatEnded += HandleCombatEnd;

            ChangeState(GameState.MainMenu);
            Debug.Log("[GameManager] ✅ Sanctuary pronto!");
        }

        private void OnDestroy()
        {
            EventBus.OnHeroDied -= HandleHeroDeath;
            EventBus.OnCombatEnded -= HandleCombatEnd;
        }

        // ====================== STATE MACHINE ======================

        public void ChangeState(GameState newState)
        {
            GameState oldState = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] Estado: {oldState} → {newState}");

            OnExitState(oldState);
            OnEnterState(newState);

            EventBus.GameStateChanged(newState);
        }

        private void OnEnterState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    EventBus.BGMRequested("bg_menu");
                    break;

                case GameState.CharacterCreation:
                    EventBus.BGMRequested("duality");
                    break;

                case GameState.Exploration:
                    // BiomeManager cuida da música do bioma atual
                    break;

                case GameState.Combat:
                    EventBus.CombatStarted();
                    break;

                case GameState.BossFight:
                    EventBus.CombatStarted();
                    EventBus.ToastRequested("⚔️ O CHEFE DO BIOMA DESPERTOU!", "combat");
                    break;

                case GameState.Death:
                    EventBus.BGMRequested("death");
                    break;

                case GameState.Victory:
                    EventBus.SFXRequested("victory_fanfare");
                    break;
            }
        }

        private void OnExitState(GameState state)
        {
            switch (state)
            {
                case GameState.Combat:
                case GameState.BossFight:
                    // Limpar estado de combate temporário
                    break;
            }
        }

        // ====================== SLOT MANAGEMENT ======================

        /// <summary>
        /// Muda o slot ativo (0, 1 ou 2) — Port de appState.activeSlotIndex
        /// </summary>
        public void SetActiveSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
            {
                Debug.LogError($"[GameManager] Slot inválido: {slotIndex}");
                return;
            }
            activeSlotIndex = slotIndex;
            Debug.Log($"[GameManager] Slot ativo: {slotIndex}");
        }

        // ====================== NOVO JOGO ======================

        public void StartNewGame(string heroName, HeroClass heroClass, HeroRace race, string profession)
        {
            Debug.Log($"[GameManager] Novo jogo: {heroName} ({race} {heroClass} - {profession})");

            // HeroManager criará o herói e salvará no slot ativo
            // heroManager.CreateNewHero(heroName, heroClass, race, profession);

            ChangeState(GameState.Exploration);
        }

        public void ContinueGame(int slotIndex)
        {
            SetActiveSlot(slotIndex);

            if (saveManager != null && saveManager.HasSave(slotIndex))
            {
                // saveManager.LoadGame(slotIndex);
                ChangeState(GameState.Exploration);
            }
            else
            {
                Debug.LogWarning("[GameManager] Nenhum save encontrado no slot " + slotIndex);
                EventBus.ToastRequested("Nenhum herói encontrado nesse slot.", "warning");
            }
        }

        // ====================== EVENT HANDLERS ======================

        private void HandleHeroDeath()
        {
            Debug.Log("[GameManager] 💀 O Nephalem caiu...");
            ChangeState(GameState.Death);
        }

        private void HandleCombatEnd(bool victory)
        {
            if (victory)
            {
                Debug.Log("[GameManager] ⚔️ Vitória no combate!");
                ChangeState(GameState.Exploration);
            }
            // Derrota é tratada por HandleHeroDeath
        }

        // ====================== GOD MODE (Easter Egg do original) ======================

        [Header("Debug")]
        [SerializeField] private bool godMode = false;
        public bool GodMode => godMode;

        public void ToggleGodMode()
        {
            godMode = !godMode;
            string status = godMode ? "ATIVADO" : "DESATIVADO";
            EventBus.ToastRequested($"🔱 God Mode {status}", godMode ? "legendary" : "info");
            Debug.Log($"[GameManager] God Mode: {status}");
        }
    }
}
