// =========================================================================
//  SANCTUARY — HeroTraitsSystem
//  Port de: HeroTraitsSystem.js
//  Modificadores condicionais de Raça/Classe/Profissão
// =========================================================================

using UnityEngine;
using Sanctuary.Data;

namespace Sanctuary.Hero
{
    /// <summary>
    /// Sistema de traits — modificadores condicionais aplicados ao herói
    /// baseados em raça, classe e profissão.
    /// Port direto de HeroTraitsSystem.js.
    /// 
    /// Uso: Chamado pelo CombatManager antes de aplicar dano/cura/evasão
    /// para modificar valores baseado nos traits do herói.
    /// </summary>
    public static class HeroTraitsSystem
    {
        // ====================== CHECKERS DE TAG ======================
        // Port de: HeroTraits.hasEnemyTag()

        public static bool HasEnemyTag(string enemyName, string tag)
        {
            if (string.IsNullOrEmpty(enemyName)) return false;
            string n = enemyName.ToLower();

            switch (tag)
            {
                case "morto-vivo":
                    return n.Contains("esqueleto") || n.Contains("zumbi") ||
                           n.Contains("morte") || n.Contains("coveiro") ||
                           n.Contains("carniça");
                case "demonio":
                    return n.Contains("demônio") || n.Contains("diabólico") ||
                           n.Contains("infernal") || n.Contains("súcubo");
                default:
                    return false;
            }
        }

        // ====================== MODIFICADORES DE DANO ======================

        /// <summary>
        /// Modifica o dano de saída baseado na classe/profissão/raça.
        /// Chamado ANTES de aplicar dano ao inimigo.
        /// </summary>
        public static int ModifyOutgoingDamage(int baseDamage, HeroClass cls, string profession,
            ElementType skillElement, string enemyName)
        {
            float multiplier = 1.0f;

            // === CLASSE ===
            switch (cls)
            {
                case HeroClass.Paladino:
                    // +30% vs morto-vivo e demônios
                    if (HasEnemyTag(enemyName, "morto-vivo") || HasEnemyTag(enemyName, "demonio"))
                        multiplier *= 1.3f;
                    break;
            }

            // === PROFISSÃO ===
            switch (profession)
            {
                // Paladino
                case "Inquisidor do Sol":
                    if (skillElement == ElementType.Luz) multiplier *= 1.3f;
                    break;
                case "Clérigo de Batalha":
                    multiplier *= 0.5f; // Dano reduzido (compensa com cura 2x)
                    break;
                case "Templário Corrompido":
                    if (skillElement == ElementType.Profano) multiplier *= 1.4f;
                    break;

                // Ranger
                case "Caçador de Cabeças":
                    // +crit handled elsewhere
                    break;

                // Bárbaro
                case "Executor Bestial":
                    if (skillElement == ElementType.Fisico) multiplier *= 1.15f;
                    break;

                // Necromante
                case "Senhor dos Ossos":
                    if (HasEnemyTag(enemyName, "morto-vivo")) multiplier *= 0.5f; // Não ataca seus "filhos"
                    break;
                case "Ceifador Sombrio":
                    if (skillElement == ElementType.Profano) multiplier *= 1.3f;
                    break;
            }

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        /// <summary>
        /// Modifica o dano recebido baseado na raça/profissão.
        /// Chamado ANTES de aplicar dano ao herói.
        /// Port de: HeroTraits.Races[...].takeDamageMod
        /// </summary>
        public static int ModifyIncomingDamage(int baseDamage, HeroRace race, string profession,
            ElementType damageType)
        {
            float multiplier = 1.0f;

            // === RAÇA ===
            switch (race)
            {
                case HeroRace.ElfoSombrio:
                    // Ossos frágeis: +15% dano Físico recebido
                    if (damageType == ElementType.Fisico) multiplier *= 1.15f;
                    break;
                case HeroRace.AnaoDaForja:
                    // Resistência ao fogo: -30% dano de Fogo recebido
                    if (damageType == ElementType.Fogo) multiplier *= 0.70f;
                    break;
            }

            // === CLASSE ===
            // Necromante: +50% dano de Luz recebido
            // (checado via HeroClass no chamador)

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        // ====================== MODIFICADORES DE EVASÃO ======================

        /// <summary>
        /// Modifica a chance de evasão baseada na raça/profissão.
        /// Port de: HeroTraits.Races.Elfos.evasionMod + Professions
        /// </summary>
        public static int ModifyEvasion(int baseEvasion, HeroRace race, string profession)
        {
            int evasion = baseEvasion;

            // === RAÇA ===
            switch (race)
            {
                case HeroRace.ElfoSombrio:
                    evasion += 15; // +15% evasão base
                    break;
            }

            // === PROFISSÃO ===
            switch (profession)
            {
                case "Vanguarda Sangrenta":
                    evasion = 0; // Evasão NULA — absorve no peito
                    break;
                case "Batedor das Sombras":
                    evasion += 10;
                    break;
            }

            return evasion;
        }

        // ====================== MODIFICADORES DE CURA ======================

        /// <summary>
        /// Modifica cura recebida pelo herói.
        /// Port de: Professions.healReceivedMod
        /// </summary>
        public static int ModifyHealReceived(int baseHeal, string profession)
        {
            float multiplier = 1.0f;

            switch (profession)
            {
                case "Mercenário de Fossa":
                    multiplier = 0.5f; // Desprezado pela fé — cura cai pela metade
                    break;
            }

            return Mathf.RoundToInt(baseHeal * multiplier);
        }

        /// <summary>
        /// Modifica cura de SAÍDA (para curandeiros).
        /// Port de: Professions.healMod
        /// </summary>
        public static int ModifyHealOutput(int baseHeal, string profession)
        {
            float multiplier = 1.0f;

            switch (profession)
            {
                case "Clérigo de Batalha":
                    multiplier = 2.0f; // Cura dobrada
                    break;
            }

            return Mathf.RoundToInt(baseHeal * multiplier);
        }

        // ====================== MODIFICADORES DE CRÍTICO ======================

        /// <summary>
        /// Modifica chance de crítico.
        /// Port de: Professions.critChanceMod
        /// </summary>
        public static int ModifyCritChance(int baseCrit, string profession)
        {
            int crit = baseCrit;

            switch (profession)
            {
                case "Mercenário de Fossa":
                    crit += 15;
                    break;
                case "Mestre das Armas":
                    crit += 10;
                    break;
                case "Caçador de Cabeças":
                    crit += 10;
                    break;
            }

            return crit;
        }

        // ====================== MODIFICADORES DE DEFESA ======================

        /// <summary>
        /// Modifica defesa baseada na raça.
        /// Port de: HeroTraits.Races.Anões.defenseMod
        /// </summary>
        public static int ModifyDefense(int baseDef, HeroRace race)
        {
            float multiplier = 1.0f;

            switch (race)
            {
                case HeroRace.AnaoDaForja:
                    multiplier = 1.25f; // +25% DEF
                    break;
            }

            return Mathf.RoundToInt(baseDef * multiplier);
        }

        // ====================== RESTRIÇÕES DE CAST ======================

        /// <summary>
        /// Verifica se o herói pode usar uma skill com base em restrições de raça/profissão.
        /// Port de: HeroTraits.Races.Orcs.canCast + Professions restrictions
        /// </summary>
        public static CastRestriction CheckCastRestriction(HeroRace race, string profession,
            ElementType skillType)
        {
            // === RAÇA ===
            switch (race)
            {
                case HeroRace.OrcDecaido:
                    if (skillType == ElementType.Arcano)
                        return new CastRestriction
                        {
                            canCast = false,
                            message = "Orcs não possuem intelecto para magias Arcanas complexas."
                        };
                    break;
            }

            // === PROFISSÃO ===
            switch (profession)
            {
                case "Inquisidor do Sol":
                    if (skillType == ElementType.Cura || skillType == ElementType.Suporte)
                        return new CastRestriction
                        {
                            canCast = false,
                            message = "Sua profissão abomina suporte. Purifique-os com o fogo e o aço!"
                        };
                    break;
            }

            return new CastRestriction { canCast = true };
        }
    }

    public struct CastRestriction
    {
        public bool canCast;
        public string message;
    }
}
