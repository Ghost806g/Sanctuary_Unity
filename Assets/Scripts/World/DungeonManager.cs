// =========================================================================
//  SANCTUARY — DungeonManager
//  Port de: main_v3.js (sistema de andares, biomas, encounters)
//  Controla a exploração: andares, encontros, transição de bioma, boss gates
// =========================================================================

using UnityEngine;
using Sanctuary.Core;
using Sanctuary.Data;

namespace Sanctuary.World
{
    /// <summary>
    /// Controla a exploração da dungeon: andares, encontros aleatórios,
    /// boss gates, transição de bioma.
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        [Header("Biomas (Ordem de progressão)")]
        [SerializeField] private BiomeData[] biomes; // Catacumbas, Pântano, Magma, Cristal
        [SerializeField] private int currentBiomeIndex = 0;
        [SerializeField] private int currentFloor = 1;
        [SerializeField] private int encounterCount = 0; // Encontros no andar atual

        public BiomeData CurrentBiome => biomes != null && currentBiomeIndex < biomes.Length
            ? biomes[currentBiomeIndex] : null;
        public int CurrentFloor => currentFloor;

        [Header("Configuração")]
        [SerializeField] private int encountersPerFloor = 3; // Combates por andar
        [SerializeField] private float eliteChance = 0.15f;
        [SerializeField] private float eventChance = 0.10f; // Chance de evento aleatório

        // ====================== LIFECYCLE ======================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnCombatEnded += OnCombatEnded;
        }

        private void OnDisable()
        {
            EventBus.OnCombatEnded -= OnCombatEnded;
        }

        // ====================== EXPLORAÇÃO ======================

        /// <summary>
        /// Avança na dungeon — resolve o próximo encontro.
        /// </summary>
        public void Explore()
        {
            if (CurrentBiome == null)
            {
                Debug.LogError("[DungeonManager] Nenhum bioma configurado!");
                return;
            }

            encounterCount++;

            // Checar se é hora do boss
            if (encounterCount >= CurrentBiome.floorsBeforeBoss)
            {
                EventBus.BossGateReached();
                EventBus.CombatLogEntry($"🚪 Você sente uma presença aterrorizante... O boss aguarda!");
                EventBus.ToastRequested("⚠️ O CHEFE DO BIOMA ESTÁ PRÓXIMO!", "warning");
                return;
            }

            // Evento aleatório?
            if (Random.value < eventChance)
            {
                TriggerRandomEvent();
                return;
            }

            // Combate normal
            StartRandomEncounter();
        }

        /// <summary>
        /// Inicia um combate aleatório com um monstro do bioma atual.
        /// </summary>
        public void StartRandomEncounter()
        {
            if (CurrentBiome.monsters == null || CurrentBiome.monsters.Length == 0)
            {
                Debug.LogError("[DungeonManager] Bioma sem monstros!");
                return;
            }

            // Sorteia monstro
            int index = Random.Range(0, CurrentBiome.monsters.Length);
            MonsterData monster = CurrentBiome.monsters[index];

            // Verificar elite
            bool forceElite = currentFloor >= 5 && Random.value < eliteChance;

            // Pega nível do herói
            int heroLevel = Hero.HeroManager.Instance?.HeroData?.level ?? 1;
            bool hardmode = Hero.HeroManager.Instance?.HeroData?.hardmode ?? false;

            // Inicia combate
            Combat.CombatManager.Instance.InitCombat(monster, heroLevel, false, forceElite, hardmode);
            GameManager.Instance.ChangeState(GameState.Combat);
        }

        /// <summary>
        /// Inicia a luta contra o boss do bioma.
        /// </summary>
        public void StartBossFight()
        {
            if (CurrentBiome.boss == null)
            {
                Debug.LogError("[DungeonManager] Bioma sem boss!");
                return;
            }

            int heroLevel = Hero.HeroManager.Instance?.HeroData?.level ?? 1;
            bool hardmode = Hero.HeroManager.Instance?.HeroData?.hardmode ?? false;

            Combat.CombatManager.Instance.InitCombat(CurrentBiome.boss, heroLevel, true, false, hardmode);
            GameManager.Instance.ChangeState(GameState.BossFight);

            EventBus.CombatLogEntry($"👑 O BOSS {CurrentBiome.boss.monsterName} bloqueia a saída das {CurrentBiome.displayName}!");
        }

        // ====================== PROGRESSÃO ======================

        private void OnCombatEnded(bool victory)
        {
            if (!victory) return;

            currentFloor++;
            EventBus.FloorChanged(currentFloor);

            // Atualiza dados do herói
            if (Hero.HeroManager.Instance?.HeroData != null)
            {
                Hero.HeroManager.Instance.HeroData.dungeonLevel = currentFloor;
                Hero.HeroManager.Instance.HeroData.killCount++;
            }

            // Checar transição de bioma (após derrotar o boss)
            if (Combat.CombatManager.Instance.IsBossFight)
            {
                if (Hero.HeroManager.Instance?.HeroData != null)
                    Hero.HeroManager.Instance.HeroData.bossesDefeated++;

                AdvanceBiome();
            }

            // Drop de materiais (chance de mineração)
            if (Random.value < 0.4f) // 40% chance de drop de material
            {
                DropRandomMaterial();
            }

            // Drop de fragmento de lore (chance rara)
            if (Random.value < 0.08f)
            {
                DropLoreFragment();
            }
        }

        /// <summary>
        /// Avança para o próximo bioma após derrotar o boss.
        /// </summary>
        private void AdvanceBiome()
        {
            currentBiomeIndex++;
            encounterCount = 0;

            if (currentBiomeIndex >= biomes.Length)
            {
                // Todos os biomas completos — modo New Game+?
                currentBiomeIndex = 0;
                EventBus.ToastRequested("🏆 Todos os biomas conquistados! A escuridão recomeça...", "legendary");
                Debug.Log("[DungeonManager] Ciclo completo — reiniciando biomas");
            }

            if (Hero.HeroManager.Instance?.HeroData != null)
                Hero.HeroManager.Instance.HeroData.currentBiomeIndex = currentBiomeIndex;

            EventBus.BiomeChanged(CurrentBiome.biomeType);
            EventBus.BGMRequested(CurrentBiome.bgmId);
            EventBus.ToastRequested($"🌍 Novo bioma desbloqueado: {CurrentBiome.displayName}!", "info");

            Debug.Log($"[DungeonManager] Bioma avançado: {CurrentBiome.displayName}");
        }

        // ====================== DROPS ======================

        private void DropRandomMaterial()
        {
            // Drop baseado no bioma e andar
            MaterialID[] commonMats = { MaterialID.Ferro, MaterialID.Cobre, MaterialID.Couro, MaterialID.Carvao };
            MaterialID[] rareMats = { MaterialID.Prata, MaterialID.Mithril, MaterialID.Quartzo };
            MaterialID[] epicMats = { MaterialID.Esmeralda, MaterialID.Adamantium, MaterialID.Diamante };

            MaterialID mat;
            float roll = Random.value;

            if (roll > 0.95f && currentFloor >= 15)
                mat = epicMats[Random.Range(0, epicMats.Length)];
            else if (roll > 0.75f && currentFloor >= 5)
                mat = rareMats[Random.Range(0, rareMats.Length)];
            else
                mat = commonMats[Random.Range(0, commonMats.Length)];

            int amount = Random.Range(1, 4);
            EventBus.MaterialGained(mat, amount);
        }

        private void DropLoreFragment()
        {
            if (CurrentBiome.fragments == null || CurrentBiome.fragments.Length == 0) return;

            var fragment = CurrentBiome.fragments[Random.Range(0, CurrentBiome.fragments.Length)];

            // Checar se já foi encontrado
            var hero = Hero.HeroManager.Instance?.HeroData;
            if (hero != null && hero.foundFragmentIds != null)
            {
                foreach (var id in hero.foundFragmentIds)
                {
                    if (id == fragment.fragmentId) return; // Já encontrado
                }

                // Adicionar
                var list = new System.Collections.Generic.List<string>(hero.foundFragmentIds);
                list.Add(fragment.fragmentId);
                hero.foundFragmentIds = list.ToArray();
            }

            EventBus.ToastRequested($"📜 Fragmento encontrado: {fragment.displayName}", "legendary");
            EventBus.SFXRequested("loot");
        }

        // ====================== EVENTOS ALEATÓRIOS ======================

        private void TriggerRandomEvent()
        {
            int eventType = Random.Range(0, 4);

            switch (eventType)
            {
                case 0: // Fonte de cura
                    int healAmount = Hero.HeroManager.Instance?.HeroData?.maxHp / 4 ?? 25;
                    Hero.HeroManager.Instance?.HealHP(healAmount);
                    EventBus.CombatLogEntry($"🏥 Você encontra uma fonte mística e recupera {healAmount} HP!");
                    EventBus.ToastRequested("💧 Fonte de cura encontrada!", "info");
                    break;

                case 1: // Baú de ouro
                    int goldFound = Random.Range(20, 100) * currentFloor;
                    EventBus.GoldGained(goldFound);
                    EventBus.CombatLogEntry($"💰 Você encontra um baú com {goldFound} de ouro!");
                    EventBus.ToastRequested($"💰 Baú encontrado: {goldFound} ouro!", "info");
                    break;

                case 2: // Armadilha
                    int trapDmg = Mathf.RoundToInt((Hero.HeroManager.Instance?.HeroData?.maxHp ?? 100) * 0.1f);
                    Hero.HeroManager.Instance?.TakeDamage(trapDmg);
                    EventBus.CombatLogEntry($"⚠️ ARMADILHA! Você sofre {trapDmg} de dano!");
                    EventBus.ToastRequested("⚠️ Armadilha ativada!", "warning");
                    break;

                case 3: // Mercador misterioso
                    EventBus.CombatLogEntry("🧙 Um mercador misterioso aparece nas sombras...");
                    EventBus.ToastRequested("🧙 Mercador Misterioso!", "info");
                    // TODO: Abrir loja
                    break;
            }
        }

        // ====================== LOAD STATE ======================

        public void LoadState(int biomeIndex, int floor)
        {
            currentBiomeIndex = Mathf.Clamp(biomeIndex, 0, biomes.Length - 1);
            currentFloor = floor;
            encounterCount = 0;

            EventBus.BiomeChanged(CurrentBiome.biomeType);
            Debug.Log($"[DungeonManager] Estado carregado: {CurrentBiome.displayName}, Andar {currentFloor}");
        }
    }
}
