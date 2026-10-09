// =========================================================================
//  SANCTUARY — CombatCalculator
//  Port de: cálculos espalhados em CombatEngine.js e main_v3.js
//  Fórmulas puras sem side effects — fácil de testar
// =========================================================================

using UnityEngine;
using Sanctuary.Data;

namespace Sanctuary.Combat
{
    /// <summary>
    /// Calculadora pura de dano, crítico, evasão, armadura, elemental.
    /// Sem side effects — ideal para unit tests.
    /// </summary>
    public class CombatCalculator
    {
        // ====================== CONSTANTES DE BALANCEAMENTO ======================
        // Port das fórmulas hardcoded no CombatEngine.js

        private const float CRIT_MULTIPLIER = 2.0f;
        private const float COMBO_BONUS_PER_HIT = 0.05f; // +5% por combo
        private const float MAX_COMBO_BONUS = 0.50f; // Cap em 50%
        private const float POSTURE_BASE_RATE = 0.15f;
        private const float POSTURE_CRIT_BONUS = 0.3f;
        private const float ARMOR_REDUCTION_FACTOR = 0.5f;
        private const float MIN_DAMAGE_RATIO = 0.1f; // Dano mínimo = 10% do base

        // ====================== DANO BÁSICO ======================

        /// <summary>
        /// Calcula dano base considerando ATK vs DEF.
        /// Fórmula: ATK - (DEF * 0.5), com mínimo de ATK * 0.1
        /// </summary>
        public int CalculateBaseDamage(int attackerATK, int defenderDEF)
        {
            float rawDmg = attackerATK - (defenderDEF * ARMOR_REDUCTION_FACTOR);
            float minDmg = attackerATK * MIN_DAMAGE_RATIO;
            return Mathf.Max(Mathf.RoundToInt(rawDmg), Mathf.Max(1, Mathf.RoundToInt(minDmg)));
        }

        /// <summary>
        /// Calcula dano de uma skill, considerando scaling de atributo.
        /// </summary>
        public int CalculateSkillDamage(SkillData skill, int heroAtk, int defenderDEF)
        {
            float base_dmg = skill.baseDamage + (heroAtk * skill.scalingMultiplier);
            float afterArmor = base_dmg - (defenderDEF * ARMOR_REDUCTION_FACTOR);
            float minDmg = base_dmg * MIN_DAMAGE_RATIO;
            return Mathf.Max(Mathf.RoundToInt(afterArmor), Mathf.Max(1, Mathf.RoundToInt(minDmg)));
        }

        // ====================== CRÍTICO ======================

        /// <summary>
        /// Rola se o ataque é crítico.
        /// </summary>
        public bool RollCrit(int critChance)
        {
            float chance = Mathf.Clamp(critChance, 1f, 75f) / 100f;
            return Random.value <= chance;
        }

        /// <summary>
        /// Aplica multiplicador de crítico ao dano.
        /// </summary>
        public int ApplyCritMultiplier(int baseDamage, float additionalMultiplier = 0f)
        {
            return Mathf.RoundToInt(baseDamage * (CRIT_MULTIPLIER + additionalMultiplier));
        }

        // ====================== COMBO ======================

        /// <summary>
        /// Aplica bônus de combo ao dano.
        /// Port de: comboCounter system
        /// </summary>
        public int ApplyComboBonus(int baseDamage, int comboCount)
        {
            float bonus = Mathf.Min(comboCount * COMBO_BONUS_PER_HIT, MAX_COMBO_BONUS);
            return Mathf.RoundToInt(baseDamage * (1f + bonus));
        }

        // ====================== EVASÃO ======================

        /// <summary>
        /// Calcula se o ataque foi esquivado.
        /// Port de: cálculo de evasão baseado em agilidade
        /// </summary>
        public bool RollEvasion(int defenderAgi, int attackerAgi)
        {
            float baseChance = (defenderAgi - attackerAgi) * 0.5f;
            float chance = Mathf.Clamp(baseChance, 2f, 40f) / 100f;
            return Random.value <= chance;
        }

        // ====================== POSTURA (STAGGER) ======================

        /// <summary>
        /// Calcula dano de postura causado por um ataque.
        /// Port de: sistema de stagger em CombatEngine.js
        /// </summary>
        public float CalculatePostureDamage(int damage, bool isCrit)
        {
            float postureDmg = damage * POSTURE_BASE_RATE;
            if (isCrit) postureDmg += damage * POSTURE_CRIT_BONUS;
            return postureDmg;
        }

        // ====================== ELEMENTAL ======================

        /// <summary>
        /// Aplica multiplicador elemental (fraqueza/resistência).
        /// </summary>
        public float GetElementalMultiplier(ElementType attackElement, ElementType defenseElement)
        {
            // Tabela de vantagem elemental
            // Fogo > Gelo > Raio > Fogo (triângulo)
            // Luz > Profano > Arcano > Luz (triângulo mágico)
            // Venenosa é neutra mas ignora defesa parcialmente

            if (attackElement == ElementType.Fogo && defenseElement == ElementType.Gelo) return 1.5f;
            if (attackElement == ElementType.Gelo && defenseElement == ElementType.Raio) return 1.5f;
            if (attackElement == ElementType.Raio && defenseElement == ElementType.Fogo) return 1.5f;

            if (attackElement == ElementType.Luz && defenseElement == ElementType.Profano) return 1.5f;
            if (attackElement == ElementType.Profano && defenseElement == ElementType.Arcano) return 1.5f;
            if (attackElement == ElementType.Arcano && defenseElement == ElementType.Luz) return 1.5f;

            // Resistências (inverso das vantagens)
            if (attackElement == ElementType.Gelo && defenseElement == ElementType.Fogo) return 0.7f;
            if (attackElement == ElementType.Raio && defenseElement == ElementType.Gelo) return 0.7f;
            if (attackElement == ElementType.Fogo && defenseElement == ElementType.Raio) return 0.7f;

            if (attackElement == ElementType.Profano && defenseElement == ElementType.Luz) return 0.7f;
            if (attackElement == ElementType.Arcano && defenseElement == ElementType.Profano) return 0.7f;
            if (attackElement == ElementType.Luz && defenseElement == ElementType.Arcano) return 0.7f;

            return 1.0f; // Neutro
        }

        // ====================== LEVEL UP ======================

        /// <summary>
        /// Calcula XP necessário para o próximo nível.
        /// Curva exponencial com aceleração.
        /// </summary>
        public int CalculateXPToNextLevel(int currentLevel)
        {
            // Fórmula progressiva: 100 * level^1.5
            return Mathf.RoundToInt(100 * Mathf.Pow(currentLevel, 1.5f));
        }

        /// <summary>
        /// Calcula HP máximo baseado em constituição e nível.
        /// </summary>
        public int CalculateMaxHP(int constituicao, int level, float classHPPerCon = 10f)
        {
            return Mathf.RoundToInt((constituicao * classHPPerCon) + (level * 5));
        }

        /// <summary>
        /// Calcula Mana máxima baseada em inteligência e nível.
        /// </summary>
        public int CalculateMaxMana(int inteligencia, int level, float classManaPerInt = 5f)
        {
            return Mathf.RoundToInt((inteligencia * classManaPerInt) + (level * 3));
        }

        /// <summary>
        /// Calcula ATK efetivo baseado em atributos, equipamento e buffs.
        /// </summary>
        public int CalculateEffectiveATK(int forca, int weaponATK, float classMultiplier = 1.0f)
        {
            return Mathf.RoundToInt((forca * 2 + weaponATK) * classMultiplier);
        }

        /// <summary>
        /// Calcula DEF efetiva baseada em equipamento e constituição.
        /// </summary>
        public int CalculateEffectiveDEF(int constituicao, int armorDEF, float classMultiplier = 1.0f)
        {
            return Mathf.RoundToInt((constituicao + armorDEF) * classMultiplier);
        }

        // ====================== LOOT ======================

        /// <summary>
        /// Determina a raridade de um drop com base no andar e sorte.
        /// </summary>
        public ItemRarity RollItemRarity(int dungeonLevel, float luckBonus = 0f)
        {
            float roll = Random.value + luckBonus + (dungeonLevel * 0.005f);

            if (roll > 0.99f) return ItemRarity.Mitico;
            if (roll > 0.95f) return ItemRarity.Lendario;
            if (roll > 0.85f) return ItemRarity.Epico;
            if (roll > 0.65f) return ItemRarity.Raro;
            if (roll > 0.40f) return ItemRarity.Incomum;
            return ItemRarity.Comum;
        }
    }
}
