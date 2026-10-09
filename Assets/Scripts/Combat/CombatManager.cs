// =========================================================================
//  SANCTUARY — CombatManager
//  Port de: CombatEngine.js (initCombatInstance + resolveHeroTurn + resolveEnemyTurn)
//  O CORAÇÃO do jogo — sistema de combate por turnos
// =========================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sanctuary.Data;

namespace Sanctuary.Combat
{
    /// <summary>
    /// Engine de combate por turnos. Gerencia turnos, dano, status,
    /// postura (stagger) e resolução de vitória/derrota.
    /// Port direto do CombatEngine.js com melhorias.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        // ====================== ESTADO DO COMBATE ======================

        [Header("Estado")]
        [SerializeField] private bool isInCombat = false;
        [SerializeField] private bool isPlayerTurn = true;
        [SerializeField] private bool isBossFight = false;
        public bool IsInCombat => isInCombat;
        public bool IsPlayerTurn => isPlayerTurn;
        public bool IsBossFight => isBossFight;

        // Instância do monstro atual (clonado do ScriptableObject)
        private CombatMonsterInstance currentEnemy;
        public CombatMonsterInstance CurrentEnemy => currentEnemy;

        // Estado de combate do herói (temporário, resetado por combate)
        private HeroCombatState heroCombatState;
        public HeroCombatState HeroCombatState => heroCombatState;

        // Combo (Port de comboCounter)
        private int comboCounter = 0;
        public int ComboCounter => comboCounter;

        // Run Stats (sobrevive entre combates do mesmo run)
        private RunStats currentRunStats;
        public RunStats CurrentRunStats => currentRunStats;

        // Referências
        private CombatCalculator calculator;

        // ====================== LIFECYCLE ======================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            calculator = new CombatCalculator();
            currentRunStats = new RunStats();
        }

        private void OnEnable()
        {
            Core.EventBus.OnCombatStarted += OnCombatStarted;
        }

        private void OnDisable()
        {
            Core.EventBus.OnCombatStarted -= OnCombatStarted;
        }

        // ====================== INICIAR COMBATE ======================
        // Port de: initCombatInstance(isBoss, forceElite)

        /// <summary>
        /// Inicia um combate com um monstro do bioma atual.
        /// </summary>
        public void InitCombat(MonsterData monsterData, int heroLevel, bool isBoss, bool forceElite = false, bool hardmode = false)
        {
            isInCombat = true;
            isBossFight = isBoss;
            isPlayerTurn = true;
            comboCounter = 0;

            // Clona o monstro para esta instância de combate
            currentEnemy = new CombatMonsterInstance(monsterData, heroLevel, isBoss, forceElite, hardmode);

            // Reset estado de combate do herói
            heroCombatState = new HeroCombatState();

            // Logs
            if (isBoss)
            {
                Core.EventBus.CombatLogEntry($"⚔️ O CHEFE DO BIOMA DESPERTOU: {currentEnemy.Name}!");
                Core.EventBus.SFXRequested("boss_appear");
            }
            else if (currentEnemy.IsElite)
            {
                Core.EventBus.CombatLogEntry($"⚠️ ELITE ENCONTRADO: {currentEnemy.Name} espreitava nas sombras!");
                Core.EventBus.SFXRequested("elite_appear");
            }
            else
            {
                Core.EventBus.CombatLogEntry($"🗡️ Um {currentEnemy.Name} selvagem ataca!");
            }

            Core.EventBus.ComboChanged(0);
            Debug.Log($"[CombatManager] Combate iniciado: {currentEnemy.Name} (HP:{currentEnemy.CurrentHP}/{currentEnemy.MaxHP})");
        }

        // ====================== TURNO DO JOGADOR ======================
        // Port de: resolveHeroTurn()

        /// <summary>
        /// Resolve a ação do jogador no turno atual.
        /// </summary>
        public CombatActionResult ExecutePlayerAction(PlayerCombatAction action, SkillData skill = null, int heroAtk = 0, int heroCrit = 0, int heroAgi = 0)
        {
            if (!isInCombat || !isPlayerTurn)
                return new CombatActionResult { success = false, message = "Não é seu turno!" };

            CombatActionResult result = new CombatActionResult { success = true };

            switch (action)
            {
                case PlayerCombatAction.Attack:
                    result = ResolveBasicAttack(heroAtk, heroCrit);
                    break;

                case PlayerCombatAction.Skill:
                    if (skill != null)
                        result = ResolveSkillAttack(skill, heroAtk, heroCrit);
                    break;

                case PlayerCombatAction.Defend:
                    result = ResolveDefend();
                    break;

                case PlayerCombatAction.Flee:
                    result = ResolveFlee(heroAgi);
                    break;

                case PlayerCombatAction.UseItem:
                    // Será tratado pelo InventoryManager
                    result.message = "Item usado.";
                    break;
            }

            // Checar morte do inimigo
            if (currentEnemy.CurrentHP <= 0)
            {
                result.combatEnded = true;
                result.victory = true;
                EndCombat(true);
                return result;
            }

            // Processar status do inimigo (veneno, burn, etc.)
            ProcessStatusEffects(currentEnemy.Statuses, currentEnemy);

            // Passar turno para o inimigo
            isPlayerTurn = false;

            return result;
        }

        // ====================== AÇÕES DO JOGADOR ======================

        private CombatActionResult ResolveBasicAttack(int heroAtk, int heroCrit)
        {
            var result = new CombatActionResult();

            // Calcular dano
            int baseDmg = calculator.CalculateBaseDamage(heroAtk, currentEnemy.DEF);
            bool isCrit = calculator.RollCrit(heroCrit);

            if (isCrit)
            {
                baseDmg = calculator.ApplyCritMultiplier(baseDmg);
                result.isCrit = true;
                comboCounter++;
                Core.EventBus.ComboChanged(comboCounter);
                Core.EventBus.SFXRequested("crit");
            }
            else
            {
                Core.EventBus.SFXRequested("hit");
            }

            // Aplicar combo bonus
            baseDmg = calculator.ApplyComboBonus(baseDmg, comboCounter);

            // Aplicar dano
            currentEnemy.TakeDamage(baseDmg);

            // Dano de postura (Port do sistema de stagger)
            float postureDmg = calculator.CalculatePostureDamage(baseDmg, isCrit);
            currentEnemy.DamagePosture(postureDmg);

            if (currentEnemy.IsStaggered)
            {
                Core.EventBus.EnemyStaggered();
                Core.EventBus.CombatLogEntry($"💫 {currentEnemy.Name} está ATORDOADO! Dano dobrado no próximo turno!");
            }

            result.damage = baseDmg;
            result.message = isCrit
                ? $"⚡ CRÍTICO! Você golpeia {currentEnemy.Name} por {baseDmg} de dano!"
                : $"🗡️ Você ataca {currentEnemy.Name} por {baseDmg} de dano.";

            Core.EventBus.DamageDealt(currentEnemy.Name, baseDmg, isCrit);
            Core.EventBus.CombatLogEntry(result.message);

            result.success = true;
            return result;
        }

        private CombatActionResult ResolveSkillAttack(SkillData skill, int heroAtk, int heroCrit)
        {
            var result = new CombatActionResult();

            // Calcular dano com scaling do skill
            int baseDmg = calculator.CalculateSkillDamage(skill, heroAtk, currentEnemy.DEF);
            bool isCrit = calculator.RollCrit(heroCrit + (int)skill.critBonus);

            if (isCrit)
            {
                baseDmg = calculator.ApplyCritMultiplier(baseDmg);
                result.isCrit = true;
                comboCounter++;
                Core.EventBus.ComboChanged(comboCounter);
            }

            baseDmg = calculator.ApplyComboBonus(baseDmg, comboCounter);

            // Multi-hit (Port do sistema de multi-hit)
            int totalDamage = 0;
            for (int i = 0; i < skill.hitCount; i++)
            {
                int hitDmg = Mathf.RoundToInt(baseDmg * skill.hitDamageMultiplier);
                currentEnemy.TakeDamage(hitDmg);
                totalDamage += hitDmg;
            }

            // Aplicar status (veneno, burn, etc.)
            if (skill.hasStatusEffect && UnityEngine.Random.value <= skill.statusChance)
            {
                currentEnemy.AddStatus(new ActiveStatusEffect
                {
                    type = skill.appliedStatus,
                    duration = skill.statusDuration,
                    tickDamage = Mathf.RoundToInt(totalDamage * 0.15f)
                });
                Core.EventBus.StatusApplied(currentEnemy.Name, skill.appliedStatus, skill.statusDuration);
            }

            // Postura
            float postureDmg = calculator.CalculatePostureDamage(totalDamage, isCrit);
            currentEnemy.DamagePosture(postureDmg);

            result.damage = totalDamage;
            result.success = true;
            result.message = $"✨ {skill.displayName} acerta {currentEnemy.Name} por {totalDamage}!";

            Core.EventBus.DamageDealt(currentEnemy.Name, totalDamage, isCrit);
            Core.EventBus.CombatLogEntry(result.message);

            return result;
        }

        private CombatActionResult ResolveDefend()
        {
            heroCombatState.defBuff += 50; // +50% DEF por 1 turno
            heroCombatState.isDefending = true;

            Core.EventBus.CombatLogEntry("🛡️ Você se prepara para defender!");
            Core.EventBus.SFXRequested("defend");

            return new CombatActionResult
            {
                success = true,
                message = "🛡️ Postura defensiva! Dano reduzido no próximo ataque."
            };
        }

        private CombatActionResult ResolveFlee(int heroAgi)
        {
            if (isBossFight)
            {
                return new CombatActionResult
                {
                    success = false,
                    message = "❌ Não há fuga de um Boss!"
                };
            }

            // Chance de fugir baseada em agilidade (Port do original)
            float fleeChance = Mathf.Clamp(0.3f + heroAgi * 0.01f, 0.1f, 0.85f);

            if (UnityEngine.Random.value <= fleeChance)
            {
                EndCombat(false, fled: true);
                return new CombatActionResult
                {
                    success = true,
                    combatEnded = true,
                    message = "🏃 Você foge do combate!"
                };
            }

            return new CombatActionResult
            {
                success = false,
                message = "❌ Falha ao fugir! O inimigo bloqueia sua saída!"
            };
        }

        // ====================== TURNO DO INIMIGO ======================
        // Port de: resolveEnemyTurn() + UtilityAI

        /// <summary>
        /// Executa o turno do inimigo usando a Utility AI.
        /// </summary>
        public CombatActionResult ExecuteEnemyTurn(int heroDEF, int heroMaxHP)
        {
            if (!isInCombat || isPlayerTurn)
                return new CombatActionResult { success = false };

            var result = new CombatActionResult();

            // IA decide a ação (Port da UtilityAI)
            int baseDmg = calculator.CalculateBaseDamage(currentEnemy.ATK, heroDEF);

            // Se o herói está defendendo, reduz o dano
            if (heroCombatState.isDefending)
            {
                baseDmg = Mathf.RoundToInt(baseDmg * 0.4f);
                heroCombatState.isDefending = false;
                heroCombatState.defBuff = 0;
            }

            // Se inimigo está staggered, pula turno
            if (currentEnemy.IsStaggered)
            {
                currentEnemy.RecoverPosture();
                result.message = $"💫 {currentEnemy.Name} está atordoado e perde o turno!";
                result.damage = 0;
                Core.EventBus.CombatLogEntry(result.message);
                isPlayerTurn = true;
                return result;
            }

            // Boss Phase 2 check (Port do boss phase 2)
            if (currentEnemy.IsBoss && currentEnemy.SourceData.hasPhase2 && !currentEnemy.IsPhase2)
            {
                float hpRatio = (float)currentEnemy.CurrentHP / currentEnemy.MaxHP;
                if (hpRatio <= currentEnemy.SourceData.phase2HPThreshold)
                {
                    currentEnemy.EnterPhase2();
                    Core.EventBus.CombatLogEntry($"🔥 {currentEnemy.Name} ENTRA NA FASE 2! O poder dele aumentou!");
                    Core.EventBus.SFXRequested("boss_phase2");
                }
            }

            // Aplicar dano
            result.damage = baseDmg;
            result.message = $"⚔️ {currentEnemy.Name} ataca por {baseDmg} de dano!";
            result.success = true;

            // Vampiric affix (Port do affix vampiric)
            if (currentEnemy.HasAffix(EliteAffix.Vampiric))
            {
                int heal = Mathf.RoundToInt(baseDmg * 0.3f);
                currentEnemy.Heal(heal);
                result.message += $" (Vampírico: curou {heal} HP)";
            }

            // Reset combo do jogador se tomou dano
            comboCounter = 0;
            Core.EventBus.ComboChanged(0);

            Core.EventBus.CombatLogEntry(result.message);
            Core.EventBus.DamageDealt("hero", baseDmg, false);
            Core.EventBus.SFXRequested("enemy_hit");

            // Processar status do inimigo
            ProcessStatusEffects(currentEnemy.Statuses, currentEnemy);

            // Volta pro turno do jogador
            isPlayerTurn = true;

            return result;
        }

        // ====================== STATUS EFFECTS ======================

        private void ProcessStatusEffects(List<ActiveStatusEffect> statuses, CombatMonsterInstance target)
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                var status = statuses[i];

                switch (status.type)
                {
                    case StatusType.Poison:
                    case StatusType.Burn:
                    case StatusType.Bleed:
                        target.TakeDamage(status.tickDamage);
                        Core.EventBus.CombatLogEntry(
                            $"🔥 {target.Name} sofre {status.tickDamage} de dano de {status.type}!");
                        break;

                    case StatusType.Regen:
                        target.Heal(status.tickDamage);
                        break;
                }

                status.duration--;
                if (status.duration <= 0)
                {
                    statuses.RemoveAt(i);
                }
            }
        }

        // ====================== FIM DO COMBATE ======================

        private void EndCombat(bool victory, bool fled = false)
        {
            isInCombat = false;

            if (victory)
            {
                currentRunStats.totalKills++;
                if (isBossFight) currentRunStats.bossKills++;

                Core.EventBus.CombatLogEntry($"🏆 {currentEnemy.Name} foi derrotado!");
                Core.EventBus.XPGained(currentEnemy.XPDrop);
                Core.EventBus.GoldGained(currentEnemy.GoldDrop);
            }
            else if (fled)
            {
                Core.EventBus.CombatLogEntry("🏃 Você fugiu do combate!");
            }

            Core.EventBus.CombatEnded(victory);
            Debug.Log($"[CombatManager] Combate finalizado. Vitória: {victory}");
        }

        private void OnCombatStarted()
        {
            // Pode ser usado para setup visual
        }
    }

    // ====================== INSTÂNCIA DE MONSTRO EM COMBATE ======================
    // Clone mutável dos dados do ScriptableObject para uso durante o combate

    [Serializable]
    public class CombatMonsterInstance
    {
        public string Name { get; private set; }
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public int ATK { get; private set; }
        public int DEF { get; private set; }
        public int XPDrop { get; private set; }
        public int GoldDrop { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsElite { get; private set; }
        public bool IsPhase2 { get; private set; }
        public AIType AiType { get; private set; }
        public MonsterData SourceData { get; private set; }
        public List<EliteAffix> Affixes { get; private set; }
        public List<ActiveStatusEffect> Statuses { get; private set; }

        // Sistema de Postura (Stagger) — Port de activeCombatInstance.posture
        public float MaxPosture { get; private set; }
        public float CurrentPosture { get; private set; }
        public bool IsStaggered { get; private set; }

        // Cooldowns da IA
        public Dictionary<string, int> Cooldowns { get; private set; }

        /// <summary>
        /// Cria uma instância de combate com escalonamento.
        /// Port de: initCombatInstance() em CombatEngine.js
        /// </summary>
        public CombatMonsterInstance(MonsterData data, int heroLevel, bool isBoss, bool forceElite, bool hardmode)
        {
            SourceData = data;
            IsBoss = isBoss;
            IsPhase2 = false;
            Affixes = new List<EliteAffix>();
            Statuses = new List<ActiveStatusEffect>();
            Cooldowns = new Dictionary<string, int>();

            // Escalonamento — Port da Curva Híbrida AAA
            float scale;
            if (isBoss)
            {
                scale = 1f + heroLevel * 0.1f + (Mathf.Pow(1.04f, heroLevel) - 1f);
            }
            else
            {
                scale = 1f + (heroLevel - 1) * 0.08f + (Mathf.Pow(1.035f, heroLevel - 1) - 1f);
            }

            if (hardmode)
            {
                scale *= 3.0f; // MODO SOULSLIKE: 300% mais forte
            }

            // Aplica scaling
            MaxHP = Mathf.RoundToInt(data.baseHP * scale);
            CurrentHP = MaxHP;
            ATK = Mathf.RoundToInt(data.baseATK * scale);
            DEF = Mathf.RoundToInt(data.baseDEF * scale);
            XPDrop = Mathf.RoundToInt(data.xpDrop * scale);
            GoldDrop = Mathf.RoundToInt(data.goldDrop * scale);
            AiType = data.aiType;

            Name = isBoss ? $"👑 {data.monsterName}" : data.monsterName;

            // Sistema de Afixos de Elite (Port do sistema de sufixos)
            if (!isBoss && (forceElite || (heroLevel >= 5 && UnityEngine.Random.value < 0.25f)))
            {
                EliteAffix[] possibleAffixes = { EliteAffix.Vampiric, EliteAffix.Armored, EliteAffix.Agile };
                EliteAffix affix = possibleAffixes[UnityEngine.Random.Range(0, possibleAffixes.Length)];
                Affixes.Add(affix);
                IsElite = true;

                // Buffs de elite
                MaxHP = Mathf.RoundToInt(MaxHP * 1.3f);
                CurrentHP = MaxHP;
                ATK = Mathf.RoundToInt(ATK * 1.2f);

                string affixName = affix switch
                {
                    EliteAffix.Vampiric => "Vampírico",
                    EliteAffix.Armored => "Blindado",
                    EliteAffix.Agile => "Ágil",
                    _ => ""
                };
                Name = $"{data.monsterName} [{affixName}]";
            }

            // Sistema de Postura (Port de activeCombatInstance.maxPosture)
            float postureMultiplier = isBoss ? 0.8f : 0.4f;
            if (HasAffix(EliteAffix.Armored)) postureMultiplier += 0.3f;
            if (HasAffix(EliteAffix.Agile)) postureMultiplier -= 0.2f;

            MaxPosture = Mathf.Floor(MaxHP * postureMultiplier);
            CurrentPosture = MaxPosture;
            IsStaggered = false;
        }

        public void TakeDamage(int dmg)
        {
            int finalDmg = IsStaggered ? dmg * 2 : dmg; // Dobro se staggered
            CurrentHP = Mathf.Max(0, CurrentHP - finalDmg);
        }

        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        }

        public void DamagePosture(float amount)
        {
            if (IsStaggered) return;
            CurrentPosture = Mathf.Max(0f, CurrentPosture - amount);
            Core.EventBus.PostureDamaged(CurrentPosture / MaxPosture);

            if (CurrentPosture <= 0f)
            {
                IsStaggered = true;
            }
        }

        public void RecoverPosture()
        {
            IsStaggered = false;
            CurrentPosture = MaxPosture * 0.5f; // Recupera 50%
        }

        public void EnterPhase2()
        {
            IsPhase2 = true;
            ATK = Mathf.RoundToInt(ATK * SourceData.phase2AtkMultiplier);
            if (!string.IsNullOrEmpty(SourceData.phase2Name))
                Name = $"👑 {SourceData.phase2Name}";
        }

        public bool HasAffix(EliteAffix affix) => Affixes.Contains(affix);

        public void AddStatus(ActiveStatusEffect status) => Statuses.Add(status);
    }

    // ====================== SUPPORTING CLASSES ======================

    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusType type;
        public int duration;
        public int tickDamage;
    }

    [Serializable]
    public class HeroCombatState
    {
        public List<ActiveStatusEffect> statuses = new List<ActiveStatusEffect>();
        public int defBuff = 0;
        public int atkBuff = 0;
        public float dodge = 0f;
        public bool isDefending = false;
    }

    [Serializable]
    public class RunStats
    {
        public int totalKills = 0;
        public int bossKills = 0;
        public int totalDamageDealt = 0;
        public int totalDamageTaken = 0;
        public int maxCombo = 0;
        public int potionsUsed = 0;
    }

    [Serializable]
    public class CombatActionResult
    {
        public bool success = false;
        public string message = "";
        public int damage = 0;
        public bool isCrit = false;
        public bool combatEnded = false;
        public bool victory = false;
    }
}
