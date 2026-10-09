// =========================================================================
//  SANCTUARY — Utility AI (O Cérebro dos Monstros)
//  Port de: UtilityAI.js, Evaluators.js, Actions.js
//  Sistema de IA baseada em utilidade com scoring e fuzziness
// =========================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Sanctuary.Combat.AI
{
    /// <summary>
    /// Engine de IA por utilidade. Cada monstro "pensa" sobre suas ações
    /// e escolhe a melhor baseada em scores contextuais.
    /// Port direto do UtilityAI.js + AIActions + AIEvaluators.
    /// </summary>
    public static class UtilityAI
    {
        /// <summary>
        /// Executa a IA e retorna a melhor ação para o monstro.
        /// Port de: window.executeUtilityAI()
        /// </summary>
        public static AIActionResult Execute(CombatMonsterInstance enemy, AIContext context)
        {
            // 1. Define o kit de ações baseado no tipo
            List<AIAction> availableActions;
            if (enemy.IsBoss)
                availableActions = AIActionDatabase.GetBossKit();
            else if (enemy.IsElite)
                availableActions = AIActionDatabase.GetEliteKit();
            else
                availableActions = AIActionDatabase.GetCommonKit();

            // 2. Inicializa cooldowns se necessário
            if (enemy.Cooldowns == null)
                return Fallback(context.baseDmg);

            // 3. O monstro "pensa" sobre cada ação
            AIAction bestAction = null;
            float highestScore = -1f;

            foreach (var action in availableActions)
            {
                // Checar cooldown
                if (enemy.Cooldowns.TryGetValue(action.id, out int cd) && cd > 0)
                    continue;

                // Calcular score
                float score = action.ScoreFunction(context, enemy);

                // Fuzziness — pequena aleatoriedade para não ser 100% previsível
                // Port de: score += (Math.random() * 0.1)
                score += Random.Range(0f, 0.1f);

                Debug.Log($"[AI Thinking] Ação: {action.name} | Score: {score:F2}");

                if (score > highestScore)
                {
                    highestScore = score;
                    bestAction = action;
                }
            }

            // 4. Executa a melhor ação
            if (bestAction != null)
            {
                Debug.Log($"[AI Decision] Escolheu: {bestAction.name}");

                // Aplica cooldown
                if (bestAction.cooldown > 0)
                    enemy.Cooldowns[bestAction.id] = bestAction.cooldown;

                // Decrementa todos os cooldowns
                var keys = new List<string>(enemy.Cooldowns.Keys);
                foreach (var key in keys)
                {
                    if (enemy.Cooldowns[key] > 0)
                        enemy.Cooldowns[key]--;
                }

                return bestAction.Execute(context, enemy);
            }

            // Fallback
            return Fallback(context.baseDmg);
        }

        private static AIActionResult Fallback(int baseDmg)
        {
            return new AIActionResult
            {
                damage = baseDmg,
                skillName = "Ataque Básico (Fallback)",
                skillType = ElementType.Fisico,
                isAttack = true
            };
        }
    }

    // ====================== CONTEXTO ======================

    /// <summary>
    /// Tudo que o monstro "vê" para tomar decisões.
    /// Port de: const context = { enemy, hero, calc, baseDmg }
    /// </summary>
    public class AIContext
    {
        public int heroCurrentHP;
        public int heroMaxHP;
        public int heroDEF;
        public int heroATK;
        public int baseDmg;
        public bool heroIsDefending;
        public List<ActiveStatusEffect> heroStatuses;
        public int turnCount;

        public float HeroHPRatio => heroMaxHP > 0 ? (float)heroCurrentHP / heroMaxHP : 1f;
    }

    // ====================== AÇÃO DE IA ======================

    /// <summary>
    /// Define uma ação que o monstro pode executar.
    /// Port de: window.AIActions
    /// </summary>
    public class AIAction
    {
        public string id;
        public string name;
        public int cooldown;

        // Função de scoring (retorna 0-1)
        public System.Func<AIContext, CombatMonsterInstance, float> ScoreFunction;

        // Função de execução
        public System.Func<AIContext, CombatMonsterInstance, AIActionResult> Execute;
    }

    // ====================== RESULTADO ======================

    public class AIActionResult
    {
        public int damage;
        public string skillName;
        public ElementType skillType;
        public bool isAttack;
        public int healAmount;
        public string logMessage;
    }

    // ====================== BANCO DE AÇÕES DA IA ======================
    // Port de: AIActions + AIBossKits

    public static class AIActionDatabase
    {
        // ========== AÇÕES ==========

        private static AIAction BasicAttack = new AIAction
        {
            id = "basic_attack",
            name = "Ataque Básico",
            cooldown = 0,
            ScoreFunction = (ctx, enemy) =>
            {
                // Sempre tem um score base — é o fallback
                float score = 0.4f;
                if (enemy.AiType == AIType.Aggressive) score += 0.2f;
                return score;
            },
            Execute = (ctx, enemy) => new AIActionResult
            {
                damage = ctx.baseDmg,
                skillName = "Ataque Básico",
                skillType = ElementType.Fisico,
                isAttack = true,
                logMessage = $"⚔️ {enemy.Name} desfere um golpe violento!"
            }
        };

        private static AIAction HeavyAttack = new AIAction
        {
            id = "heavy_attack",
            name = "Golpe Pesado",
            cooldown = 2,
            ScoreFunction = (ctx, enemy) =>
            {
                float score = 0.3f;
                // Mais score se o herói está com HP baixo (executar!)
                if (ctx.HeroHPRatio < 0.3f) score += 0.5f;
                if (enemy.AiType == AIType.Aggressive) score += 0.15f;
                return score;
            },
            Execute = (ctx, enemy) => new AIActionResult
            {
                damage = Mathf.RoundToInt(ctx.baseDmg * 1.8f),
                skillName = "Golpe Pesado",
                skillType = ElementType.Fisico,
                isAttack = true,
                logMessage = $"💥 {enemy.Name} concentra toda sua força num golpe devastador!"
            }
        };

        private static AIAction Defend = new AIAction
        {
            id = "defend",
            name = "Defesa",
            cooldown = 3,
            ScoreFunction = (ctx, enemy) =>
            {
                float hpRatio = (float)enemy.CurrentHP / enemy.MaxHP;
                float score = 0.1f;
                // Defende mais quando com HP baixo
                if (hpRatio < 0.3f) score += 0.5f;
                if (enemy.AiType == AIType.Defensive) score += 0.3f;
                return score;
            },
            Execute = (ctx, enemy) => new AIActionResult
            {
                damage = 0,
                skillName = "Postura Defensiva",
                skillType = ElementType.Fisico,
                isAttack = false,
                logMessage = $"🛡️ {enemy.Name} adota uma postura defensiva!"
            }
        };

        private static AIAction Heal = new AIAction
        {
            id = "heal",
            name = "Regeneração",
            cooldown = 4,
            ScoreFunction = (ctx, enemy) =>
            {
                float hpRatio = (float)enemy.CurrentHP / enemy.MaxHP;
                float score = 0f;
                // Score altíssimo quando HP está muito baixo
                if (hpRatio < 0.2f) score = 0.9f;
                else if (hpRatio < 0.5f) score = 0.5f;
                if (enemy.AiType == AIType.Defensive) score += 0.1f;
                return score;
            },
            Execute = (ctx, enemy) =>
            {
                int healAmt = Mathf.RoundToInt(enemy.MaxHP * 0.15f);
                enemy.Heal(healAmt);
                return new AIActionResult
                {
                    damage = 0,
                    healAmount = healAmt,
                    skillName = "Regeneração Sombria",
                    skillType = ElementType.Profano,
                    isAttack = false,
                    logMessage = $"💚 {enemy.Name} se regenera por {healAmt} HP!"
                };
            }
        };

        private static AIAction AOEAttack = new AIAction
        {
            id = "aoe_attack",
            name = "Ataque de Área",
            cooldown = 3,
            ScoreFunction = (ctx, enemy) =>
            {
                float score = 0.25f;
                if (enemy.AiType == AIType.Caster) score += 0.3f;
                if (enemy.IsBoss) score += 0.1f;
                return score;
            },
            Execute = (ctx, enemy) => new AIActionResult
            {
                damage = Mathf.RoundToInt(ctx.baseDmg * 1.4f),
                skillName = "Devastação",
                skillType = ElementType.Arcano,
                isAttack = true,
                logMessage = $"🌀 {enemy.Name} libera uma onda de energia devastadora!"
            }
        };

        private static AIAction Enrage = new AIAction
        {
            id = "enrage",
            name = "Fúria",
            cooldown = 5,
            ScoreFunction = (ctx, enemy) =>
            {
                float hpRatio = (float)enemy.CurrentHP / enemy.MaxHP;
                float score = 0.1f;
                // Enrageça quando abaixo de 40% HP
                if (hpRatio < 0.4f) score += 0.6f;
                if (enemy.IsBoss) score += 0.1f;
                return score;
            },
            Execute = (ctx, enemy) =>
            {
                // Buff de ATK permanente
                // O CombatManager aplicará o buff
                return new AIActionResult
                {
                    damage = 0,
                    skillName = "FÚRIA!",
                    skillType = ElementType.Fisico,
                    isAttack = false,
                    logMessage = $"🔥 {enemy.Name} ENFURECE! Seu ataque aumenta dramaticamente!"
                };
            }
        };

        // ========== KITS ==========
        // Port de: window.AIBossKits

        public static List<AIAction> GetCommonKit()
        {
            return new List<AIAction> { BasicAttack, HeavyAttack, Defend };
        }

        public static List<AIAction> GetEliteKit()
        {
            return new List<AIAction> { BasicAttack, HeavyAttack, Defend, Heal };
        }

        public static List<AIAction> GetBossKit()
        {
            return new List<AIAction> { BasicAttack, HeavyAttack, Defend, Heal, AOEAttack, Enrage };
        }
    }
}
