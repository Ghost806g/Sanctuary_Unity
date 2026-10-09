// =========================================================================
//  SANCTUARY — HeroManager
//  Port de: main_v3.js (createFreshHero, levelUp, equip, etc.)
//  Gerencia o herói ativo: atributos, equipamento, skills, progressão
// =========================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sanctuary.Core;
using Sanctuary.Data;

namespace Sanctuary.Hero
{
    /// <summary>
    /// Gerencia o herói ativo: criação, level up, equipamento, uso de skills.
    /// Port de: createFreshHero(), levelUp(), e toda a lógica do herói em main_v3.js.
    /// </summary>
    public class HeroManager : MonoBehaviour
    {
        public static HeroManager Instance { get; private set; }

        [Header("Hero Data (Runtime)")]
        [SerializeField] private HeroSaveData heroData;
        public HeroSaveData HeroData => heroData;

        [Header("Database References")]
        [SerializeField] private ClassData[] allClasses;
        [SerializeField] private RaceData[] allRaces;
        [SerializeField] private ProfessionData[] allProfessions;

        // Cache de stats efetivos
        private int effectiveATK;
        private int effectiveDEF;
        private int effectiveCrit;
        private int effectiveEvasion;

        public int EffectiveATK => effectiveATK;
        public int EffectiveDEF => effectiveDEF;
        public int EffectiveCrit => effectiveCrit;
        public int EffectiveEvasion => effectiveEvasion;

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
            EventBus.OnXPGained += GainXP;
            EventBus.OnGoldGained += GainGold;
            EventBus.OnMaterialGained += GainMaterial;
        }

        private void OnDisable()
        {
            EventBus.OnXPGained -= GainXP;
            EventBus.OnGoldGained -= GainGold;
            EventBus.OnMaterialGained -= GainMaterial;
        }

        // ====================== CRIAÇÃO DE HERÓI ======================
        // Port de: createFreshHero(name, cls, race, profession)

        /// <summary>
        /// Cria um novo herói com os parâmetros do ritual de criação.
        /// Port direto de createFreshHero() no main_v3.js.
        /// </summary>
        public HeroSaveData CreateNewHero(string name, HeroClass cls, HeroRace race, string profession)
        {
            heroData = new HeroSaveData();
            heroData.heroName = name;
            heroData.heroClass = cls;
            heroData.heroRace = race;
            heroData.profession = profession;

            // Atributos base (todos começam em 10)
            heroData.forca = 10;
            heroData.constituicao = 10;
            heroData.agilidade = 10;
            heroData.inteligencia = 10;
            heroData.sabedoria = 10;

            // Aplica bônus de Classe
            ClassData classData = GetClassData(cls);
            if (classData != null)
            {
                heroData.forca += classData.bonusForca;
                heroData.constituicao += classData.bonusConstituicao;
                heroData.agilidade += classData.bonusAgilidade;
                heroData.inteligencia += classData.bonusInteligencia;
                heroData.sabedoria += classData.bonusSabedoria;
            }

            // Aplica bônus de Raça
            RaceData raceData = GetRaceData(race);
            if (raceData != null)
            {
                heroData.forca += raceData.bonusForca;
                heroData.constituicao += raceData.bonusConstituicao;
                heroData.agilidade += raceData.bonusAgilidade;
                heroData.inteligencia += raceData.bonusInteligencia;
                heroData.sabedoria += raceData.bonusSabedoria;
            }

            // Aplica bônus de Profissão
            ProfessionData profData = GetProfessionData(profession);
            if (profData != null)
            {
                heroData.forca += profData.bonusForca;
                heroData.constituicao += profData.bonusConstituicao;
                heroData.agilidade += profData.bonusAgilidade;
                heroData.inteligencia += profData.bonusInteligencia;
                heroData.sabedoria += profData.bonusSabedoria;
            }

            // Calcula HP e Mana iniciais
            var calc = new Combat.CombatCalculator();
            float hpPerCon = classData?.hpPerCon ?? 10f;
            float manaPerInt = classData?.manaPerInt ?? 5f;

            heroData.maxHp = calc.CalculateMaxHP(heroData.constituicao, 1, hpPerCon);
            heroData.hp = heroData.maxHp;
            heroData.maxMana = calc.CalculateMaxMana(heroData.inteligencia, 1, manaPerInt);
            heroData.mana = heroData.maxMana;
            heroData.maxVigor = 100;
            heroData.vigor = heroData.maxVigor;

            // Inicializa inventário
            heroData.equippedItemIds = new string[5];
            heroData.materialQuantities = new int[25]; // Uma entrada por MaterialID
            heroData.level = 1;
            heroData.xp = 0;
            heroData.xpToNext = calc.CalculateXPToNextLevel(1);
            heroData.dungeonLevel = 1;

            RecalculateStats();

            Debug.Log($"[HeroManager] 🏰 Herói criado: {name} ({race} {cls} - {profession})");
            Debug.Log($"[HeroManager] Stats: FOR={heroData.forca} CON={heroData.constituicao} AGI={heroData.agilidade} INT={heroData.inteligencia} SAB={heroData.sabedoria}");
            Debug.Log($"[HeroManager] HP={heroData.hp}/{heroData.maxHp} Mana={heroData.mana}/{heroData.maxMana}");

            return heroData;
        }

        // ====================== LEVEL UP ======================

        public void GainXP(int amount)
        {
            if (heroData == null) return;

            heroData.xp += amount;
            EventBus.CombatLogEntry($"✨ +{amount} XP");

            while (heroData.xp >= heroData.xpToNext)
            {
                heroData.xp -= heroData.xpToNext;
                heroData.level++;

                var calc = new Combat.CombatCalculator();
                heroData.xpToNext = calc.CalculateXPToNextLevel(heroData.level);

                // Ganho de atributos por nível
                heroData.forca += 1;
                heroData.constituicao += 1;
                heroData.agilidade += 1;
                heroData.inteligencia += 1;
                heroData.sabedoria += 1;

                // Recalcula HP/Mana
                ClassData classData = GetClassData(heroData.heroClass);
                heroData.maxHp = calc.CalculateMaxHP(heroData.constituicao, heroData.level, classData?.hpPerCon ?? 10f);
                heroData.hp = heroData.maxHp; // Full heal no level up
                heroData.maxMana = calc.CalculateMaxMana(heroData.inteligencia, heroData.level, classData?.manaPerInt ?? 5f);
                heroData.mana = heroData.maxMana;

                RecalculateStats();

                EventBus.HeroLevelUp(heroData.level);
                EventBus.ToastRequested($"🎉 LEVEL UP! Nível {heroData.level}!", "legendary");
                EventBus.SFXRequested("level_up");

                Debug.Log($"[HeroManager] ⬆️ LEVEL UP! Nível {heroData.level}");
            }
        }

        // ====================== RECURSOS ======================

        public void GainGold(int amount)
        {
            if (heroData == null) return;
            heroData.gold += amount;
            EventBus.CombatLogEntry($"💰 +{amount} Ouro");
        }

        public void GainMaterial(MaterialID mat, int amount)
        {
            if (heroData == null) return;
            int index = (int)mat;
            if (index < heroData.materialQuantities.Length)
            {
                heroData.materialQuantities[index] += amount;
                EventBus.CombatLogEntry($"📦 +{amount} {mat}");
            }
        }

        public int GetMaterialCount(MaterialID mat)
        {
            int index = (int)mat;
            if (heroData != null && index < heroData.materialQuantities.Length)
                return heroData.materialQuantities[index];
            return 0;
        }

        public bool SpendGold(int amount)
        {
            if (heroData == null || heroData.gold < amount) return false;
            heroData.gold -= amount;
            return true;
        }

        public bool SpendMaterial(MaterialID mat, int amount)
        {
            int index = (int)mat;
            if (heroData == null || heroData.materialQuantities[index] < amount) return false;
            heroData.materialQuantities[index] -= amount;
            return true;
        }

        // ====================== DANO E CURA ======================

        public void TakeDamage(int damage)
        {
            if (heroData == null) return;
            if (GameManager.Instance.GodMode) return;

            heroData.hp = Mathf.Max(0, heroData.hp - damage);
            EventBus.HeroHPChanged((float)heroData.hp / heroData.maxHp);

            if (heroData.hp <= 0)
            {
                EventBus.HeroDied();
            }
        }

        public void HealHP(int amount)
        {
            if (heroData == null) return;
            heroData.hp = Mathf.Min(heroData.maxHp, heroData.hp + amount);
            EventBus.HeroHPChanged((float)heroData.hp / heroData.maxHp);
            EventBus.HealApplied(heroData.heroName, amount);
        }

        public bool SpendMana(int amount)
        {
            if (heroData == null || heroData.mana < amount) return false;
            heroData.mana -= amount;
            EventBus.HeroManaChanged((float)heroData.mana / heroData.maxMana);
            return true;
        }

        // ====================== STATS RECALCULATION ======================

        public void RecalculateStats()
        {
            if (heroData == null) return;

            var calc = new Combat.CombatCalculator();

            // Base stats
            int weaponATK = 0; // TODO: Somar do item equipado
            int armorDEF = 0;

            effectiveATK = calc.CalculateEffectiveATK(heroData.forca, weaponATK);
            effectiveDEF = calc.CalculateEffectiveDEF(heroData.constituicao, armorDEF);
            effectiveCrit = 5 + (heroData.agilidade / 3); // Base 5% + agilidade
            effectiveEvasion = heroData.agilidade / 2;
        }

        // ====================== DATA LOOKUPS ======================

        public ClassData GetClassData(HeroClass cls)
        {
            if (allClasses == null) return null;
            return allClasses.FirstOrDefault(c => c.classType == cls);
        }

        public RaceData GetRaceData(HeroRace race)
        {
            if (allRaces == null) return null;
            return allRaces.FirstOrDefault(r => r.raceType == race);
        }

        public ProfessionData GetProfessionData(string profName)
        {
            if (allProfessions == null) return null;
            return allProfessions.FirstOrDefault(p => p.professionName == profName);
        }

        // ====================== SAVE / LOAD ======================

        public void LoadHeroData(HeroSaveData data)
        {
            heroData = data;
            RecalculateStats();
            Debug.Log($"[HeroManager] Herói carregado: {heroData.heroName} Lv{heroData.level}");
        }
    }
}
