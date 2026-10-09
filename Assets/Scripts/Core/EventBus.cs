// =========================================================================
//  SANCTUARY — EventBus (Sistema de Eventos Desacoplado)
//  Permite que sistemas se comuniquem sem dependência direta.
// =========================================================================

using System;

namespace Sanctuary.Core
{
    /// <summary>
    /// Barramento de eventos global. Todos os sistemas publicam e assinam
    /// eventos aqui ao invés de chamar uns aos outros diretamente.
    /// Inspirado no padrão Observer — facilita futuro 3D migration.
    /// </summary>
    public static class EventBus
    {
        // ====================== GAME STATE ======================
        public static event Action<GameState> OnGameStateChanged;
        public static void GameStateChanged(GameState newState)
            => OnGameStateChanged?.Invoke(newState);

        // ====================== COMBATE ======================
        public static event Action OnCombatStarted;
        public static void CombatStarted() => OnCombatStarted?.Invoke();

        public static event Action<bool> OnCombatEnded; // true = vitória
        public static void CombatEnded(bool victory) => OnCombatEnded?.Invoke(victory);

        public static event Action<string, int, bool> OnDamageDealt; // alvo, dano, isCrit
        public static void DamageDealt(string target, int damage, bool isCrit)
            => OnDamageDealt?.Invoke(target, damage, isCrit);

        public static event Action<string, int> OnHealApplied; // alvo, quantidade
        public static void HealApplied(string target, int amount)
            => OnHealApplied?.Invoke(target, amount);

        public static event Action<string, StatusType, int> OnStatusApplied; // alvo, tipo, duração
        public static void StatusApplied(string target, StatusType type, int duration)
            => OnStatusApplied?.Invoke(target, type, duration);

        public static event Action<int> OnComboChanged; // novo combo count
        public static void ComboChanged(int combo) => OnComboChanged?.Invoke(combo);

        public static event Action<float> OnPostureDamaged; // % de postura
        public static void PostureDamaged(float posturePercent)
            => OnPostureDamaged?.Invoke(posturePercent);

        public static event Action OnEnemyStaggered;
        public static void EnemyStaggered() => OnEnemyStaggered?.Invoke();

        // ====================== HERÓI ======================
        public static event Action<int> OnHeroLevelUp; // novo nível
        public static void HeroLevelUp(int newLevel) => OnHeroLevelUp?.Invoke(newLevel);

        public static event Action<float> OnHeroHPChanged; // HP ratio (0-1)
        public static void HeroHPChanged(float ratio) => OnHeroHPChanged?.Invoke(ratio);

        public static event Action<float> OnHeroManaChanged; // Mana ratio (0-1)
        public static void HeroManaChanged(float ratio) => OnHeroManaChanged?.Invoke(ratio);

        public static event Action OnHeroDied;
        public static void HeroDied() => OnHeroDied?.Invoke();

        // ====================== LOOT ======================
        public static event Action<string, ItemRarity> OnItemDropped; // nome, raridade
        public static void ItemDropped(string itemName, ItemRarity rarity)
            => OnItemDropped?.Invoke(itemName, rarity);

        public static event Action<int> OnGoldGained;
        public static void GoldGained(int amount) => OnGoldGained?.Invoke(amount);

        public static event Action<int> OnXPGained;
        public static void XPGained(int amount) => OnXPGained?.Invoke(amount);

        public static event Action<MaterialID, int> OnMaterialGained;
        public static void MaterialGained(MaterialID mat, int amount)
            => OnMaterialGained?.Invoke(mat, amount);

        // ====================== DUNGEON ======================
        public static event Action<int> OnFloorChanged; // novo andar
        public static void FloorChanged(int floor) => OnFloorChanged?.Invoke(floor);

        public static event Action<BiomeType> OnBiomeChanged;
        public static void BiomeChanged(BiomeType biome)
            => OnBiomeChanged?.Invoke(biome);

        public static event Action OnBossGateReached;
        public static void BossGateReached() => OnBossGateReached?.Invoke();

        // ====================== CRAFTING ======================
        public static event Action<string> OnItemForged; // nome do item
        public static void ItemForged(string itemName) => OnItemForged?.Invoke(itemName);

        public static event Action<string> OnPotionBrewed;
        public static void PotionBrewed(string potionName)
            => OnPotionBrewed?.Invoke(potionName);

        public static event Action<string> OnSkillFused;
        public static void SkillFused(string skillName) => OnSkillFused?.Invoke(skillName);

        // ====================== QUESTS ======================
        public static event Action<string> OnQuestCompleted;
        public static void QuestCompleted(string questId) => OnQuestCompleted?.Invoke(questId);

        // ====================== ACHIEVEMENTS ======================
        public static event Action<string> OnAchievementUnlocked;
        public static void AchievementUnlocked(string achievementId)
            => OnAchievementUnlocked?.Invoke(achievementId);

        // ====================== UI ======================
        public static event Action<string, string> OnToastRequested; // mensagem, tipo
        public static void ToastRequested(string message, string type = "info")
            => OnToastRequested?.Invoke(message, type);

        public static event Action<string> OnCombatLogEntry;
        public static void CombatLogEntry(string message)
            => OnCombatLogEntry?.Invoke(message);

        // ====================== ÁUDIO ======================
        public static event Action<string> OnSFXRequested;
        public static void SFXRequested(string sfxId) => OnSFXRequested?.Invoke(sfxId);

        public static event Action<string> OnBGMRequested;
        public static void BGMRequested(string bgmId) => OnBGMRequested?.Invoke(bgmId);
    }
}
