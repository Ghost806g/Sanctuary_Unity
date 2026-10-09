// =========================================================================
//  SANCTUARY — World Systems (Bestiário, Conquistas, Panteão, Quests)
//  Port de: main_v3.js (seções de bestiário, conquistas, panteão, quests)
// =========================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sanctuary.Core;
using Sanctuary.Data;

namespace Sanctuary.World
{
    // ====================== BESTIÁRIO ======================

    /// <summary>
    /// Rastreia kills e desbloqueia vantagens contra espécies.
    /// Port de: bestiário em main_v3.js
    /// </summary>
    public class BestiaryManager : MonoBehaviour
    {
        public static BestiaryManager Instance { get; private set; }

        [Header("Database de Monstros")]
        [SerializeField] private MonsterData[] allMonsters;

        // Kill tracking (runtime)
        private Dictionary<string, int> killCounts = new Dictionary<string, int>();
        private HashSet<string> discoveredMonsters = new HashSet<string>();

        // Thresholds para bônus
        private static readonly int[] KILL_THRESHOLDS = { 10, 25, 50, 100 };
        private static readonly float[] KILL_BONUSES = { 0.05f, 0.10f, 0.15f, 0.25f };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        private void OnCombatEnded(bool victory)
        {
            if (!victory) return;

            var enemy = Combat.CombatManager.Instance?.CurrentEnemy;
            if (enemy == null) return;

            string baseName = enemy.SourceData?.monsterName ?? enemy.Name;
            RegisterKill(baseName);
        }

        public void RegisterKill(string monsterName)
        {
            if (!killCounts.ContainsKey(monsterName))
                killCounts[monsterName] = 0;

            killCounts[monsterName]++;
            discoveredMonsters.Add(monsterName);

            int kills = killCounts[monsterName];

            // Checar threshold
            for (int i = 0; i < KILL_THRESHOLDS.Length; i++)
            {
                if (kills == KILL_THRESHOLDS[i])
                {
                    float bonus = KILL_BONUSES[i] * 100;
                    EventBus.ToastRequested(
                        $"📖 Bestiário: +{bonus}% de dano contra {monsterName} ({kills} abates)!",
                        "info"
                    );
                    break;
                }
            }
        }

        /// <summary>
        /// Retorna o bônus de dano contra um monstro específico.
        /// </summary>
        public float GetDamageBonus(string monsterName)
        {
            if (!killCounts.TryGetValue(monsterName, out int kills))
                return 0f;

            float bonus = 0f;
            for (int i = KILL_THRESHOLDS.Length - 1; i >= 0; i--)
            {
                if (kills >= KILL_THRESHOLDS[i])
                {
                    bonus = KILL_BONUSES[i];
                    break;
                }
            }
            return bonus;
        }

        public int GetKillCount(string monsterName)
        {
            return killCounts.TryGetValue(monsterName, out int count) ? count : 0;
        }

        public bool IsDiscovered(string monsterName) => discoveredMonsters.Contains(monsterName);

        public void LoadData(string[] monsterIds, int[] counts)
        {
            killCounts.Clear();
            discoveredMonsters.Clear();
            if (monsterIds != null)
            {
                for (int i = 0; i < monsterIds.Length && i < counts.Length; i++)
                {
                    killCounts[monsterIds[i]] = counts[i];
                    discoveredMonsters.Add(monsterIds[i]);
                }
            }
        }
    }

    // ====================== CONQUISTAS ======================

    /// <summary>
    /// Sistema de 20+ conquistas desbloqueáveis.
    /// Port de: seção de conquistas em main_v3.js
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("Conquistas Disponíveis")]
        [SerializeField] private AchievementData[] allAchievements;

        private HashSet<string> unlockedIds = new HashSet<string>();

        public event System.Action<AchievementData> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnHeroLevelUp += (lvl) => CheckCondition(AchievementCondition.ReachLevel, lvl);
            EventBus.OnFloorChanged += (floor) => CheckCondition(AchievementCondition.ReachFloor, floor);
            EventBus.OnCombatEnded += (v) => { if (v) CheckCondition(AchievementCondition.KillCount, GetTotalKills()); };
            EventBus.OnItemForged += (_) => CheckCondition(AchievementCondition.CraftItems, GetCraftedCount());
            EventBus.OnQuestCompleted += (_) => CheckCondition(AchievementCondition.CompleteQuests, GetQuestsCompleted());
            EventBus.OnComboChanged += (combo) => CheckCondition(AchievementCondition.MaxCombo, combo);
        }

        /// <summary>
        /// Verifica se alguma conquista foi alcançada.
        /// </summary>
        public void CheckCondition(AchievementCondition condition, int currentValue)
        {
            foreach (var achievement in allAchievements)
            {
                if (unlockedIds.Contains(achievement.achievementId)) continue;
                if (achievement.condition != condition) continue;

                if (currentValue >= achievement.targetValue)
                {
                    Unlock(achievement);
                }
            }
        }

        private void Unlock(AchievementData achievement)
        {
            unlockedIds.Add(achievement.achievementId);

            // Aplicar recompensas
            if (achievement.goldReward > 0)
                EventBus.GoldGained(achievement.goldReward);
            if (achievement.xpReward > 0)
                EventBus.XPGained(achievement.xpReward);

            EventBus.AchievementUnlocked(achievement.achievementId);
            EventBus.ToastRequested($"🏆 Conquista: {achievement.displayName}!", "legendary");
            EventBus.SFXRequested("level_up");

            OnAchievementUnlocked?.Invoke(achievement);
            Debug.Log($"[AchievementManager] 🏆 Desbloqueada: {achievement.displayName}");
        }

        public bool IsUnlocked(string achievementId) => unlockedIds.Contains(achievementId);
        public int UnlockedCount => unlockedIds.Count;
        public int TotalCount => allAchievements?.Length ?? 0;

        // Helpers
        private int GetTotalKills()
        {
            return Hero.HeroManager.Instance?.HeroData?.killCount ?? 0;
        }

        private int GetCraftedCount() => 0; // TODO: Track
        private int GetQuestsCompleted()
        {
            return Hero.HeroManager.Instance?.HeroData?.completedQuestIds?.Length ?? 0;
        }

        public void LoadData(string[] ids)
        {
            unlockedIds.Clear();
            if (ids != null) foreach (var id in ids) unlockedIds.Add(id);
        }
    }

    // ====================== PANTEÃO ======================

    /// <summary>
    /// Sistema de sacrifício: gastar recursos por upgrades permanentes.
    /// Port de: seção do Panteão em main_v3.js
    /// </summary>
    public class PantheonManager : MonoBehaviour
    {
        public static PantheonManager Instance { get; private set; }

        [Header("Upgrades do Panteão")]
        [SerializeField] private PantheonUpgradeData[] allUpgrades;

        private HashSet<string> purchasedIds = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Tenta comprar um upgrade do panteão.
        /// Port de: lógica de sacrifício "sangue derramado nunca retorna"
        /// </summary>
        public bool TryPurchase(PantheonUpgradeData upgrade)
        {
            if (purchasedIds.Contains(upgrade.upgradeId))
            {
                EventBus.ToastRequested("⚠️ Bênção já obtida!", "warning");
                return false;
            }

            var hero = Hero.HeroManager.Instance;
            if (hero == null || hero.HeroData == null) return false;

            // Verificar ouro
            if (hero.HeroData.gold < upgrade.goldCost)
            {
                EventBus.ToastRequested($"⚠️ Ouro insuficiente ({hero.HeroData.gold}/{upgrade.goldCost})", "warning");
                return false;
            }

            // Verificar material (se necessário)
            if (upgrade.materialAmount > 0)
            {
                int have = hero.GetMaterialCount(upgrade.materialCost);
                if (have < upgrade.materialAmount)
                {
                    EventBus.ToastRequested($"⚠️ Material insuficiente: {upgrade.materialCost}", "warning");
                    return false;
                }
                hero.SpendMaterial(upgrade.materialCost, upgrade.materialAmount);
            }

            // Gastar ouro
            hero.SpendGold(upgrade.goldCost);

            // Aplicar bênção permanente
            purchasedIds.Add(upgrade.upgradeId);

            EventBus.ToastRequested($"🙏 Bênção recebida: {upgrade.displayName}!", "legendary");
            EventBus.SFXRequested("level_up");

            Debug.Log($"[PantheonManager] Upgrade comprado: {upgrade.displayName} ({upgrade.bonusType} +{upgrade.bonusValue})");
            return true;
        }

        /// <summary>
        /// Retorna o bônus total acumulado de um tipo.
        /// </summary>
        public float GetTotalBonus(string bonusType)
        {
            float total = 0f;
            foreach (var upgrade in allUpgrades)
            {
                if (purchasedIds.Contains(upgrade.upgradeId) && upgrade.bonusType == bonusType)
                    total += upgrade.bonusValue;
            }
            return total;
        }

        public bool IsPurchased(string upgradeId) => purchasedIds.Contains(upgradeId);

        public void LoadData(string[] ids)
        {
            purchasedIds.Clear();
            if (ids != null) foreach (var id in ids) purchasedIds.Add(id);
        }
    }

    // ====================== QUEST MANAGER ======================

    /// <summary>
    /// Quests procedurais: geração, tracking, recompensas.
    /// Port de: "Mural de Contratos Reais" em main_v3.js
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Quest Templates")]
        [SerializeField] private QuestData[] questTemplates;

        [Header("Configuração")]
        [SerializeField] private int maxActiveQuests = 5;

        private List<ActiveQuest> activeQuests = new List<ActiveQuest>();
        private List<string> completedQuestIds = new List<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnCombatEnded += OnCombatEnded;
            EventBus.OnMaterialGained += OnMaterialGained;
            EventBus.OnFloorChanged += OnFloorChanged;
        }

        private void OnDisable()
        {
            EventBus.OnCombatEnded -= OnCombatEnded;
            EventBus.OnMaterialGained -= OnMaterialGained;
            EventBus.OnFloorChanged -= OnFloorChanged;
        }

        /// <summary>
        /// Gera uma quest procedural aleatória.
        /// Port de: generateProceduralQuest()
        /// </summary>
        public ActiveQuest GenerateQuest()
        {
            if (activeQuests.Count >= maxActiveQuests)
            {
                EventBus.ToastRequested("⚠️ Limite de contratos atingido!", "warning");
                return null;
            }

            if (questTemplates == null || questTemplates.Length == 0) return null;

            // Escolher template aleatório
            QuestData template = questTemplates[Random.Range(0, questTemplates.Length)];

            // Criar instância com valores escalados
            int heroLevel = Hero.HeroManager.Instance?.HeroData?.level ?? 1;
            float scale = 1f + heroLevel * 0.1f;

            var quest = new ActiveQuest
            {
                questId = $"{template.questId}_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
                displayName = template.displayName,
                description = template.description,
                objectiveType = template.objectiveType,
                targetAmount = Mathf.RoundToInt(template.targetAmount * scale),
                currentProgress = 0,
                goldReward = Mathf.RoundToInt(template.goldReward * scale),
                xpReward = Mathf.RoundToInt(template.xpReward * scale),
                targetId = template.targetId
            };

            activeQuests.Add(quest);
            EventBus.ToastRequested($"📜 Novo Contrato: {quest.displayName}", "info");

            return quest;
        }

        private void OnCombatEnded(bool victory)
        {
            if (!victory) return;

            foreach (var quest in activeQuests.ToList())
            {
                if (quest.isCompleted) continue;

                if (quest.objectiveType == QuestObjectiveType.KillMonsters)
                {
                    quest.currentProgress++;
                    CheckQuestCompletion(quest);
                }
            }
        }

        private void OnMaterialGained(MaterialID mat, int amount)
        {
            foreach (var quest in activeQuests.ToList())
            {
                if (quest.isCompleted) continue;
                if (quest.objectiveType == QuestObjectiveType.CollectMaterials)
                {
                    quest.currentProgress += amount;
                    CheckQuestCompletion(quest);
                }
            }
        }

        private void OnFloorChanged(int floor)
        {
            foreach (var quest in activeQuests.ToList())
            {
                if (quest.isCompleted) continue;
                if (quest.objectiveType == QuestObjectiveType.ReachFloor && floor >= quest.targetAmount)
                {
                    quest.currentProgress = quest.targetAmount;
                    CheckQuestCompletion(quest);
                }
            }
        }

        private void CheckQuestCompletion(ActiveQuest quest)
        {
            if (quest.currentProgress >= quest.targetAmount)
            {
                quest.isCompleted = true;

                // Recompensas
                EventBus.GoldGained(quest.goldReward);
                EventBus.XPGained(quest.xpReward);

                completedQuestIds.Add(quest.questId);
                EventBus.QuestCompleted(quest.questId);
                EventBus.ToastRequested($"🏆 Contrato Completo: {quest.displayName}!", "legendary");
                EventBus.SFXRequested("loot");
            }
        }

        /// <summary>
        /// Limpa quests completadas.
        /// Port de: clearCompletedQuests()
        /// </summary>
        public void ClearCompleted()
        {
            activeQuests.RemoveAll(q => q.isCompleted);
        }

        public List<ActiveQuest> GetActiveQuests() => activeQuests;
        public int CompletedCount => completedQuestIds.Count;
    }

    [System.Serializable]
    public class ActiveQuest
    {
        public string questId;
        public string displayName;
        public string description;
        public QuestObjectiveType objectiveType;
        public int targetAmount;
        public int currentProgress;
        public int goldReward;
        public int xpReward;
        public string targetId;
        public bool isCompleted;

        public float ProgressRatio => targetAmount > 0 ? (float)currentProgress / targetAmount : 0f;
    }
}
