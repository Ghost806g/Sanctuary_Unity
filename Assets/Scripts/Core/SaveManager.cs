// =========================================================================
//  SANCTUARY — SaveManager (Sistema de Salvamento)
//  Port de: main_v3.js (DATABASE_KEY_V5 + localStorage)
//  Agora usa JSON files em vez de localStorage.
// =========================================================================

using System;
using System.IO;
using UnityEngine;

namespace Sanctuary.Core
{
    /// <summary>
    /// Gerencia 3 slots de save em arquivos JSON.
    /// Port do sistema localStorage → JSON persistente.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SAVE_FOLDER = "SaveData";
        private const string SAVE_PREFIX = "SANCTUARY_APEX_";
        private const int SAVE_VERSION = 5; // Compatível com V5 do original

        private string SaveDirectory
            => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

        // ====================== LIFECYCLE ======================

        private void Awake()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
                Debug.Log($"[SaveManager] Diretório de saves criado: {SaveDirectory}");
            }
        }

        // ====================== SAVE / LOAD ======================

        /// <summary>
        /// Salva os dados do herói no slot especificado.
        /// </summary>
        public void SaveGame(int slotIndex, HeroSaveData data)
        {
            if (slotIndex < 0 || slotIndex >= GameManager.MAX_SAVE_SLOTS)
            {
                Debug.LogError($"[SaveManager] Slot inválido: {slotIndex}");
                return;
            }

            try
            {
                data.saveVersion = SAVE_VERSION;
                data.lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string json = JsonUtility.ToJson(data, true);
                string path = GetSlotPath(slotIndex);
                File.WriteAllText(path, json);

                Debug.Log($"[SaveManager] ✅ Slot {slotIndex} salvo: {data.heroName}");
                EventBus.ToastRequested($"💾 Progresso salvo — Slot {slotIndex + 1}", "info");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] ❌ Erro ao salvar slot {slotIndex}: {e.Message}");
                EventBus.ToastRequested("⚠️ Erro ao salvar!", "error");
            }
        }

        /// <summary>
        /// Carrega os dados do herói do slot especificado.
        /// </summary>
        public HeroSaveData LoadGame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= GameManager.MAX_SAVE_SLOTS)
            {
                Debug.LogError($"[SaveManager] Slot inválido: {slotIndex}");
                return null;
            }

            string path = GetSlotPath(slotIndex);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] Nenhum save no slot {slotIndex}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                HeroSaveData data = JsonUtility.FromJson<HeroSaveData>(json);

                if (data.saveVersion < SAVE_VERSION)
                {
                    Debug.LogWarning($"[SaveManager] Save antigo (v{data.saveVersion}), migrando para v{SAVE_VERSION}...");
                    data = MigrateSaveData(data);
                }

                Debug.Log($"[SaveManager] ✅ Slot {slotIndex} carregado: {data.heroName} (Lv{data.level})");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] ❌ Erro ao carregar slot {slotIndex}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica se um slot tem save.
        /// </summary>
        public bool HasSave(int slotIndex)
        {
            return File.Exists(GetSlotPath(slotIndex));
        }

        /// <summary>
        /// Deleta o save de um slot.
        /// </summary>
        public void DeleteSave(int slotIndex)
        {
            string path = GetSlotPath(slotIndex);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] 🗑️ Save deletado: Slot {slotIndex}");
            }
        }

        /// <summary>
        /// Retorna info resumida de todos os slots (para a tela de seleção).
        /// </summary>
        public SaveSlotInfo[] GetAllSlotInfo()
        {
            SaveSlotInfo[] infos = new SaveSlotInfo[GameManager.MAX_SAVE_SLOTS];

            for (int i = 0; i < GameManager.MAX_SAVE_SLOTS; i++)
            {
                if (HasSave(i))
                {
                    HeroSaveData data = LoadGame(i);
                    if (data != null)
                    {
                        infos[i] = new SaveSlotInfo
                        {
                            isEmpty = false,
                            heroName = data.heroName,
                            heroClass = data.heroClass,
                            level = data.level,
                            lastSaved = data.lastSaved
                        };
                    }
                }
                else
                {
                    infos[i] = new SaveSlotInfo { isEmpty = true };
                }
            }

            return infos;
        }

        // ====================== AUTO-SAVE ======================

        /// <summary>
        /// Auto-save no slot ativo (chamado após eventos importantes).
        /// </summary>
        public void AutoSave(HeroSaveData data)
        {
            int slot = GameManager.Instance.ActiveSlotIndex;
            SaveGame(slot, data);
        }

        // ====================== HELPERS ======================

        private string GetSlotPath(int slotIndex)
        {
            return Path.Combine(SaveDirectory, $"{SAVE_PREFIX}SLOT_{slotIndex}.json");
        }

        private HeroSaveData MigrateSaveData(HeroSaveData oldData)
        {
            // Migração de versões futuras seria feita aqui
            oldData.saveVersion = SAVE_VERSION;
            return oldData;
        }
    }

    // ====================== DATA CLASSES ======================

    /// <summary>
    /// Dados completos do herói para serialização.
    /// Port de: hero object no main_v3.js
    /// </summary>
    [Serializable]
    public class HeroSaveData
    {
        public int saveVersion;
        public string lastSaved;

        // Identidade
        public string heroName;
        public HeroClass heroClass;
        public HeroRace heroRace;
        public string profession;

        // Atributos base
        public int forca;
        public int constituicao;
        public int agilidade;
        public int inteligencia;
        public int sabedoria;

        // Progressão
        public int level = 1;
        public int xp = 0;
        public int xpToNext = 100;

        // Recursos
        public int hp;
        public int maxHp;
        public int mana;
        public int maxMana;
        public int vigor;
        public int maxVigor;
        public int gold = 0;

        // Dungeon
        public int dungeonLevel = 1;
        public int currentBiomeIndex = 0;
        public int killCount = 0;
        public int bossesDefeated = 0;

        // Flags
        public bool hardmode = false;

        // Inventário (serializado como arrays)
        public string[] equippedItemIds = new string[5]; // 5 slots de equip
        public string[] inventoryItemIds = new string[0];
        public int[] materialQuantities = new int[25]; // Quantidade de cada MaterialID

        // Skills desbloqueadas
        public string[] unlockedSkillIds = new string[0];
        public string[] equippedSkillIds = new string[4]; // 4 slots de skill

        // Quests ativas
        public string[] activeQuestIds = new string[0];
        public string[] completedQuestIds = new string[0];

        // Bestiário (kills por monstro)
        public string[] bestiaryMonsterIds = new string[0];
        public int[] bestiaryKillCounts = new int[0];

        // Conquistas
        public string[] unlockedAchievementIds = new string[0];

        // Panteão (upgrades permanentes comprados)
        public string[] pantheonUpgradeIds = new string[0];

        // Lore (fragmentos encontrados)
        public string[] foundFragmentIds = new string[0];

        // Poções no inventário
        public string[] potionIds = new string[0];
        public int[] potionQuantities = new int[0];

        // Companion
        public string companionId = "";
    }

    /// <summary>
    /// Info resumida de um slot (para a UI de seleção).
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public bool isEmpty = true;
        public string heroName;
        public HeroClass heroClass;
        public int level;
        public string lastSaved;
    }
}
