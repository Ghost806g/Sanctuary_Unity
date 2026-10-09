// =========================================================================
//  SANCTUARY — ScriptableObject Data Classes
//  Port de: database.js, constants.js, HeroTraitsSystem.js
//  ScriptableObjects = "JSONs editáveis" dentro do Unity Editor
// =========================================================================

using System;
using UnityEngine;

namespace Sanctuary.Data
{
    // ====================== CLASSE DO HERÓI ======================

    /// <summary>
    /// Define uma classe jogável (Guerreiro, Arcanista, etc.)
    /// Port de: createFreshHero() em main_v3.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewClass", menuName = "Sanctuary/Hero/Class Data")]
    public class ClassData : ScriptableObject
    {
        [Header("Identidade")]
        public HeroClass classType;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public string emoji; // Para manter as raízes do original 😎

        [Header("Bônus de Atributos (Port de createFreshHero)")]
        public int bonusForca = 0;
        public int bonusConstituicao = 0;
        public int bonusAgilidade = 0;
        public int bonusInteligencia = 0;
        public int bonusSabedoria = 0;

        [Header("HP/Mana Base")]
        public float hpPerCon = 10f;   // HP por ponto de constituição
        public float manaPerInt = 5f;  // Mana por ponto de inteligência

        [Header("Profissões Disponíveis")]
        public ProfessionData[] professions;

        [Header("Skills Iniciais")]
        public SkillData[] startingSkills;
    }

    // ====================== RAÇA ======================

    /// <summary>
    /// Define uma raça jogável (Humano, Elfo Sombrio, etc.)
    /// Port de: createFreshHero() + HeroTraitsSystem.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewRace", menuName = "Sanctuary/Hero/Race Data")]
    public class RaceData : ScriptableObject
    {
        [Header("Identidade")]
        public HeroRace raceType;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public string emoji;

        [Header("Bônus de Atributos")]
        public int bonusForca = 0;
        public int bonusConstituicao = 0;
        public int bonusAgilidade = 0;
        public int bonusInteligencia = 0;
        public int bonusSabedoria = 0;

        [Header("Modificadores Raciais (Port de HeroTraits.Races)")]
        [Tooltip("Modificador de evasão (ex: +15 para Elfos)")]
        public int evasionMod = 0;

        [Tooltip("Tipos de dano com vulnerabilidade (ex: Físico para Elfos)")]
        public ElementType vulnerableElement = ElementType.Fisico;
        public float vulnerabilityMultiplier = 1.0f;

        [Tooltip("Tipos de dano com resistência (ex: Fogo para Anões)")]
        public ElementType resistantElement = ElementType.Fisico;
        public float resistanceMultiplier = 1.0f;

        [Tooltip("Elementos que esta raça NÃO pode usar")]
        public ElementType[] restrictedElements;
        public string restrictionMessage = "";
    }

    // ====================== PROFISSÃO ======================

    /// <summary>
    /// Define uma profissão (30 no total, 5 por classe).
    /// Port de: profBonuses em main_v3.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewProfession", menuName = "Sanctuary/Hero/Profession Data")]
    public class ProfessionData : ScriptableObject
    {
        [Header("Identidade")]
        public string professionName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public string emoji;
        public HeroClass parentClass; // A qual classe pertence

        [Header("Bônus de Atributos")]
        public int bonusForca = 0;
        public int bonusConstituicao = 0;
        public int bonusAgilidade = 0;
        public int bonusInteligencia = 0;
        public int bonusSabedoria = 0;

        [Header("Modificadores Especiais (Port de HeroTraits.Professions)")]
        public float critChanceMod = 0f;      // Ex: +15% para Mercenário
        public float healReceivedMod = 1.0f;   // Ex: 0.5 para Mercenário
        public float healOutputMod = 1.0f;     // Ex: 2.0 para Clérigo
        public float damageOutputMod = 1.0f;   // Ex: 0.5 para Clérigo
        public float evasionMod = 0f;          // Ex: 0 (nula) para Vanguarda

        [Tooltip("Tipos de skill que esta profissão NÃO pode usar")]
        public ElementType[] restrictedSkillTypes;
        public string restrictionMessage = "";

        [Tooltip("Tipos de skill com bônus de dano")]
        public ElementType bonusDamageElement = ElementType.Fisico;
        public float bonusDamageMultiplier = 1.0f;
    }

    // ====================== MATERIAL ======================

    /// <summary>
    /// Define um material de crafting/mineração.
    /// Port de: ALL_MATERIALS em database.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "Sanctuary/Crafting/Material Data")]
    public class MaterialData : ScriptableObject
    {
        public MaterialID materialId;
        public string displayName;
        public Color color = Color.white;
        public Sprite icon;
        public int tier = 1; // 1=básico, 2=raro, 3=épico
        public bool isAlchemyIngredient = false;
    }

    // ====================== SKILL / HABILIDADE ======================

    /// <summary>
    /// Define uma habilidade do herói.
    /// Port de: SkillsDB em skills.js + skills em database.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Sanctuary/Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identidade")]
        public string skillId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public string emoji;

        [Header("Tipo")]
        public ElementType elementType;
        public bool isFusion = false; // Tier 2+ (fusão de 2 skills)

        [Header("Stats de Combate")]
        public int baseDamage = 10;
        public int manaCost = 5;
        public int vigorCost = 0;
        public int cooldown = 0;
        public float critBonus = 0f;

        [Header("Scaling (qual atributo escala o dano)")]
        public StatType scalingStat = StatType.Forca;
        public float scalingMultiplier = 1.0f;

        [Header("Efeitos")]
        public StatusType appliedStatus = StatusType.Poison;
        public bool hasStatusEffect = false;
        public float statusChance = 0f;
        public int statusDuration = 0;

        [Header("Multi-hit")]
        public int hitCount = 1;
        public float hitDamageMultiplier = 1.0f; // Dano por hit (se multi)

        [Header("Especial")]
        public bool isAOE = false;
        public bool isSelfTarget = false;
        public float healAmount = 0f; // Se for skill de cura

        [Header("Fusão (Port de SkillRecipes)")]
        public SkillData fusionIngredient1;
        public SkillData fusionIngredient2;

        [Header("VFX")]
        public string animationTrigger;
        public Color vfxColor = Color.white;
    }

    // ====================== MONSTRO ======================

    /// <summary>
    /// Define um monstro ou boss.
    /// Port de: biome.monsters e biome.boss em database.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewMonster", menuName = "Sanctuary/World/Monster Data")]
    public class MonsterData : ScriptableObject
    {
        [Header("Identidade")]
        public string monsterName;
        [TextArea(2, 4)]
        public string description;
        public Sprite sprite;
        public string emoji;

        [Header("Stats Base")]
        public int baseHP = 50;
        public int baseATK = 10;
        public int baseDEF = 5;
        public int xpDrop = 10;
        public int goldDrop = 5;

        [Header("Tipo")]
        public bool isBoss = false;
        public AIType aiType = AIType.Balanced;
        public ElementType primaryElement = ElementType.Fisico;
        public string[] tags; // "morto-vivo", "demonio", etc.

        [Header("Boss: Fase 2 (Port de boss phase 2)")]
        public bool hasPhase2 = false;
        public float phase2HPThreshold = 0.5f;
        public float phase2AtkMultiplier = 1.5f;
        public string phase2Name;
        public Sprite phase2Sprite;

        [Header("Loot Table")]
        public LootEntry[] lootTable;

        [Header("Moveset Especial")]
        public MonsterMoveData[] specialMoves;
    }

    [Serializable]
    public class LootEntry
    {
        public string itemId;
        public float dropChance = 0.1f; // 0-1
        public ItemRarity minRarity = ItemRarity.Comum;
        public ItemRarity maxRarity = ItemRarity.Raro;
    }

    [Serializable]
    public class MonsterMoveData
    {
        public string moveName;
        public string logMessage;
        public float dmgMultiplier = 1.0f;
        public string moveType; // "heavy_attack", "defend", etc
        public StatusType appliedEffect;
        public bool hasEffect = false;
        public int cooldown = 2;
        public int weight = 40; // Peso para IA
    }

    // ====================== BIOMA ======================

    /// <summary>
    /// Define um bioma com seus monstros, boss e efeitos.
    /// Port de: BIOMES em database.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiome", menuName = "Sanctuary/World/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identidade")]
        public BiomeType biomeType;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Visual")]
        public Sprite background;
        public Sprite[] parallaxLayers; // Para parallax 2D
        public Color ambientColor = Color.white;
        public Color fogColor = Color.black;

        [Header("Áudio")]
        public string bgmId;

        [Header("Monstros")]
        public MonsterData[] monsters;
        public MonsterData boss;

        [Header("Field Effect (Efeito de Área)")]
        public string fieldEffectName;
        [TextArea(2, 4)]
        public string fieldEffectDesc;
        public FieldEffectType fieldEffectType;

        [Header("Lore")]
        public LoreFragmentData[] fragments;

        [Header("Andares")]
        public int floorsBeforeBoss = 10;
    }

    public enum FieldEffectType
    {
        None,
        PoisonTick,     // Dano de veneno por turno
        BurnTick,       // Dano de fogo por turno
        SlowMovement,   // Reduz agilidade
        DarkVision,     // Reduz precisão
        CrystalReflect  // Reflete parte do dano
    }

    // ====================== ITEM / EQUIPAMENTO ======================

    /// <summary>
    /// Define um item equipável.
    /// Port de: sistema de itens em main_v3.js + forgeFX.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Sanctuary/Items/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identidade")]
        public string itemId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public string emoji;

        [Header("Equipamento")]
        public EquipSlot slot;
        public ItemRarity rarity = ItemRarity.Comum;

        [Header("Stats")]
        public int bonusATK = 0;
        public int bonusDEF = 0;
        public int bonusHP = 0;
        public int bonusMana = 0;
        public int bonusCrit = 0;
        public int bonusEvasion = 0;

        [Header("Stats por Atributo")]
        public int bonusForca = 0;
        public int bonusConstituicao = 0;
        public int bonusAgilidade = 0;
        public int bonusInteligencia = 0;
        public int bonusSabedoria = 0;

        [Header("Set de Boss (Port de BOSS_SETS)")]
        public string setName;
        public int setId = -1; // -1 = não pertence a set
        public SetBonusData[] setBonuses;

        [Header("Efeito Especial")]
        public bool hasSpecialEffect = false;
        [TextArea(2, 3)]
        public string specialEffectDesc;

        [Header("Crafting")]
        public CraftingRecipe recipe;
        public int sellPrice = 10;

        /// <summary>
        /// Retorna a cor da borda baseada na raridade (Port das cores CSS do original).
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                ItemRarity.Comum    => new Color(0.6f, 0.6f, 0.6f),      // #9ca3af
                ItemRarity.Incomum  => new Color(0.13f, 0.77f, 0.38f),   // #22c55e
                ItemRarity.Raro    => new Color(0.22f, 0.52f, 0.96f),    // #3b82f6
                ItemRarity.Epico   => new Color(0.66f, 0.33f, 0.97f),    // #a855f7
                ItemRarity.Lendario => new Color(0.98f, 0.75f, 0.14f),   // #fbbf24
                ItemRarity.Mitico  => new Color(0.94f, 0.27f, 0.27f),    // #ef4444
                _ => Color.white
            };
        }
    }

    [Serializable]
    public class SetBonusData
    {
        public int requiredPieces = 2;
        public string bonusName;
        [TextArea(2, 3)]
        public string bonusDescription;
        public string bonusType; // "hpMult", "atkMult", "evasion", etc.
        public float bonusValue = 0.2f;
    }

    [Serializable]
    public class CraftingRecipe
    {
        public CraftingIngredient[] ingredients;
        public int goldCost = 100;
    }

    [Serializable]
    public class CraftingIngredient
    {
        public MaterialID material;
        public int quantity = 1;
    }

    // ====================== POÇÃO ======================

    /// <summary>
    /// Define uma poção crafável.
    /// Port de: POCOES_DATABASE em AlquimiaEngine.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewPotion", menuName = "Sanctuary/Items/Potion Data")]
    public class PotionData : ScriptableObject
    {
        public string potionId;
        public string displayName;
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;
        public string emoji;

        [Header("Efeito")]
        public PotionEffectType effectType;
        public float effectValue = 0f;
        public int effectDuration = 0; // 0 = instantâneo

        [Header("Receita de Alquimia")]
        public MaterialID[] ingredients;
    }

    public enum PotionEffectType
    {
        CuraLeve,
        CuraMedia,
        CuraTotal,
        VigorMedia,
        BuffFortitude,
        Veneno,
        Explosao,
        CuraConfusao
    }

    // ====================== LORE FRAGMENT ======================

    /// <summary>
    /// Fragmento de lore colecionável por bioma.
    /// Port de: MEMORY_FRAGMENT_POOL em constants.js
    /// </summary>
    [CreateAssetMenu(fileName = "NewFragment", menuName = "Sanctuary/Lore/Fragment Data")]
    public class LoreFragmentData : ScriptableObject
    {
        public string fragmentId;
        public string displayName;
        [TextArea(3, 6)]
        public string description;
        public BiomeType biome;
        public string chapter;
        public Sprite icon;
    }

    // ====================== CONQUISTA ======================

    [CreateAssetMenu(fileName = "NewAchievement", menuName = "Sanctuary/World/Achievement Data")]
    public class AchievementData : ScriptableObject
    {
        public string achievementId;
        public string displayName;
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;
        public string emoji;
        public Color color = Color.yellow;

        [Header("Condição")]
        public AchievementCondition condition;
        public int targetValue = 1;

        [Header("Recompensa")]
        public int goldReward = 0;
        public int xpReward = 0;
    }

    public enum AchievementCondition
    {
        KillCount,         // Matar X monstros
        BossKill,          // Matar X bosses
        ReachLevel,        // Alcançar nível X
        ReachFloor,        // Alcançar andar X
        CraftItems,        // Craftar X itens
        CollectMaterials,  // Coletar X materiais
        BrewPotions,       // Criar X poções
        CompleteQuests,    // Completar X quests
        FindFragments,     // Encontrar X fragmentos
        MaxCombo,          // Atingir combo X
        NoDamageBoss,      // Derrotar boss sem tomar dano
        HardmodeVictory    // Vencer no modo hardmode
    }

    // ====================== QUEST ======================

    [CreateAssetMenu(fileName = "NewQuest", menuName = "Sanctuary/World/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string questId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public string emoji;

        [Header("Objetivo")]
        public QuestObjectiveType objectiveType;
        public int targetAmount = 1;
        public string targetId; // ID do monstro, material, etc.
        public BiomeType targetBiome;

        [Header("Recompensas")]
        public int goldReward = 100;
        public int xpReward = 50;
        public MaterialID materialReward;
        public int materialRewardAmount = 0;
    }

    public enum QuestObjectiveType
    {
        KillMonsters,
        KillSpecificMonster,
        CollectMaterials,
        ReachFloor,
        DefeatBoss,
        BrewPotion,
        CraftItem
    }

    // ====================== PANTEÃO (UPGRADES PERMANENTES) ======================

    [CreateAssetMenu(fileName = "NewPantheonUpgrade", menuName = "Sanctuary/World/Pantheon Upgrade")]
    public class PantheonUpgradeData : ScriptableObject
    {
        public string upgradeId;
        public string displayName;
        [TextArea(2, 3)]
        public string description;
        public string emoji;
        public Color color = Color.magenta;

        [Header("Custo")]
        public int goldCost = 500;
        public MaterialID materialCost;
        public int materialAmount = 0;

        [Header("Bônus Global Permanente")]
        public string bonusType;
        public float bonusValue = 0.1f;
    }
}
