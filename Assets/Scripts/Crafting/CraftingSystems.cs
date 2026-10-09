// =========================================================================
//  SANCTUARY — ForgeManager + AlchemyManager + FusionEngine
//  Port de: forgeFX.js, AlquimiaEngine.js, fusionEngine.js, skills.js
//  Todos os sistemas de crafting unificados
// =========================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sanctuary.Core;
using Sanctuary.Data;

namespace Sanctuary.Crafting
{
    // ====================== FORJA ======================

    /// <summary>
    /// Sistema de forja: craftar armas/armaduras com materiais.
    /// Port de: forgeFX.js
    /// </summary>
    public class ForgeManager : MonoBehaviour
    {
        public static ForgeManager Instance { get; private set; }

        [Header("Database de Itens Craftáveis")]
        [SerializeField] private ItemData[] craftableItems;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Tenta craftar um item com os materiais do inventário.
        /// </summary>
        public CraftResult TryCraft(ItemData item)
        {
            if (item.recipe.ingredients == null || item.recipe.ingredients.Length == 0)
                return new CraftResult { success = false, message = "Este item não possui receita." };

            var hero = Hero.HeroManager.Instance;
            if (hero?.HeroData == null)
                return new CraftResult { success = false, message = "Sem herói ativo." };

            // Checar ouro
            if (hero.HeroData.gold < item.recipe.goldCost)
                return new CraftResult { success = false, message = $"Ouro insuficiente! Precisa de {item.recipe.goldCost}." };

            // Checar materiais
            foreach (var ingredient in item.recipe.ingredients)
            {
                int have = hero.GetMaterialCount(ingredient.material);
                if (have < ingredient.quantity)
                    return new CraftResult
                    {
                        success = false,
                        message = $"Material insuficiente: {ingredient.material} ({have}/{ingredient.quantity})"
                    };
            }

            // Gastar recursos
            hero.SpendGold(item.recipe.goldCost);
            foreach (var ingredient in item.recipe.ingredients)
                hero.SpendMaterial(ingredient.material, ingredient.quantity);

            // Criar item
            // TODO: Adicionar ao inventário

            EventBus.ItemForged(item.displayName);
            EventBus.SFXRequested("forge");
            EventBus.ToastRequested($"🔨 {item.displayName} forjado com sucesso!", "info");

            return new CraftResult { success = true, craftedItem = item, message = $"🔨 {item.displayName} forjado!" };
        }

        /// <summary>
        /// Transmutação: combinar 3 itens da mesma raridade em 1 de raridade superior.
        /// Port de: sellTrashLoot() / transmute logic
        /// </summary>
        public CraftResult Transmute(ItemData item1, ItemData item2, ItemData item3)
        {
            // Verifica se os 3 itens são da mesma raridade
            if (item1.rarity != item2.rarity || item2.rarity != item3.rarity)
                return new CraftResult { success = false, message = "Os 3 itens devem ser da mesma raridade!" };

            // Não pode transmutar Mítico
            if (item1.rarity == ItemRarity.Mitico)
                return new CraftResult { success = false, message = "Itens Míticos não podem ser transmutados!" };

            // Resultado: 1 item da raridade acima
            ItemRarity newRarity = item1.rarity + 1;

            EventBus.SFXRequested("forge");
            EventBus.ToastRequested($"⚗️ Transmutação! Novo item {newRarity}!", "legendary");

            return new CraftResult
            {
                success = true,
                message = $"⚗️ 3 itens {item1.rarity} → 1 item {newRarity}!"
            };
        }

        public ItemData[] GetCraftableItems() => craftableItems;
    }

    // ====================== ALQUIMIA ======================

    /// <summary>
    /// Sistema de alquimia: misturar ingredientes para criar poções.
    /// Port de: AlquimiaEngine.js
    /// </summary>
    public class AlchemyManager : MonoBehaviour
    {
        public static AlchemyManager Instance { get; private set; }

        [Header("Receitas de Poções")]
        [SerializeField] private PotionData[] allPotions;

        [Header("Caldeirão (máximo 3 ingredientes)")]
        private List<MaterialID> cauldronIngredients = new List<MaterialID>();
        public const int MAX_CAULDRON_SLOTS = 3;

        public event System.Action OnCauldronChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Adiciona um ingrediente ao caldeirão.
        /// Port de: drag and drop do caldeirão em AlquimiaEngine.js
        /// </summary>
        public bool AddIngredient(MaterialID ingredient)
        {
            if (cauldronIngredients.Count >= MAX_CAULDRON_SLOTS)
            {
                EventBus.ToastRequested("⚠️ Caldeirão cheio! Máximo 3 ingredientes.", "warning");
                return false;
            }

            // Verificar se o herói tem o ingrediente
            var hero = Hero.HeroManager.Instance;
            if (hero == null || hero.GetMaterialCount(ingredient) <= 0)
            {
                EventBus.ToastRequested("⚠️ Você não tem este ingrediente!", "warning");
                return false;
            }

            cauldronIngredients.Add(ingredient);
            OnCauldronChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Remove um ingrediente do caldeirão.
        /// </summary>
        public void RemoveIngredient(int index)
        {
            if (index >= 0 && index < cauldronIngredients.Count)
            {
                cauldronIngredients.RemoveAt(index);
                OnCauldronChanged?.Invoke();
            }
        }

        /// <summary>
        /// Limpa o caldeirão.
        /// </summary>
        public void ClearCauldron()
        {
            cauldronIngredients.Clear();
            OnCauldronChanged?.Invoke();
        }

        /// <summary>
        /// Tenta criar uma poção com os ingredientes no caldeirão.
        /// Port de: brew() em AlquimiaEngine.js
        /// </summary>
        public CraftResult Brew()
        {
            if (cauldronIngredients.Count < 2)
                return new CraftResult { success = false, message = "Adicione pelo menos 2 ingredientes!" };

            // Procura receita que combine com os ingredientes
            var sortedIngredients = cauldronIngredients.OrderBy(x => x).ToList();

            foreach (var potion in allPotions)
            {
                if (potion.ingredients == null) continue;

                var sortedRecipe = potion.ingredients.OrderBy(x => x).ToList();

                if (sortedIngredients.Count == sortedRecipe.Count &&
                    sortedIngredients.SequenceEqual(sortedRecipe))
                {
                    // Receita encontrada! Gastar ingredientes
                    var hero = Hero.HeroManager.Instance;
                    foreach (var ing in cauldronIngredients)
                        hero?.SpendMaterial(ing, 1);

                    ClearCauldron();

                    EventBus.PotionBrewed(potion.displayName);
                    EventBus.SFXRequested("potion");
                    EventBus.ToastRequested($"🧪 {potion.displayName} criada!", "info");

                    return new CraftResult
                    {
                        success = true,
                        message = $"🧪 {potion.displayName} criada com sucesso!"
                    };
                }
            }

            // Nenhuma receita — resultado aleatório (pode ser veneno!)
            ClearCauldron();
            EventBus.ToastRequested("💥 A mistura explodiu! Ingredientes perdidos.", "error");
            return new CraftResult { success = false, message = "💥 Mistura falhou! Nenhuma receita conhecida." };
        }

        public List<MaterialID> GetCauldronContents() => new List<MaterialID>(cauldronIngredients);
    }

    // ====================== FUSÃO DE SKILLS ======================

    /// <summary>
    /// Sistema de fusão de habilidades (Tier 2+).
    /// Port de: combineSkills() em skills.js
    /// </summary>
    public class FusionEngine : MonoBehaviour
    {
        public static FusionEngine Instance { get; private set; }

        [Header("Todas as Skills (para lookup de fusão)")]
        [SerializeField] private SkillData[] allSkills;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Tenta fundir duas habilidades em uma nova.
        /// Port de: combineSkills(skillId1, skillId2, playerState)
        /// REGRA DE OURO: Adicionar a nova, preservando as antigas!
        /// </summary>
        public FusionResult TryFuse(SkillData skill1, SkillData skill2)
        {
            // Procura um skill que tenha skill1 e skill2 como ingredientes
            foreach (var skill in allSkills)
            {
                if (!skill.isFusion) continue;

                bool match1 = (skill.fusionIngredient1 == skill1 && skill.fusionIngredient2 == skill2);
                bool match2 = (skill.fusionIngredient1 == skill2 && skill.fusionIngredient2 == skill1);

                if (match1 || match2)
                {
                    // Checar se já possui
                    // TODO: Verificar no HeroManager

                    EventBus.SkillFused(skill.displayName);
                    EventBus.SFXRequested("level_up");
                    EventBus.ToastRequested($"✨ Fusão de Sucesso! {skill.displayName} aprendida!", "legendary");

                    return new FusionResult
                    {
                        success = true,
                        newSkill = skill,
                        message = $"✨ Fusão de Sucesso! Você aprendeu: {skill.displayName}!"
                    };
                }
            }

            return new FusionResult
            {
                success = false,
                message = "Essas habilidades não reagem entre si. Tente outra combinação."
            };
        }
    }

    // ====================== RESULT CLASSES ======================

    public class CraftResult
    {
        public bool success;
        public string message;
        public ItemData craftedItem;
    }

    public class FusionResult
    {
        public bool success;
        public string message;
        public SkillData newSkill;
    }
}
