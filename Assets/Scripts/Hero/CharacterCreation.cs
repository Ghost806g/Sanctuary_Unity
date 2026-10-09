// =========================================================================
//  SANCTUARY — CharacterCreation (Ritual de Criação)
//  Port de: MenuPrincipal.html + menu.js (6 fases do ritual)
//  Fluxo: Raça → Classe → Subclasse → Profissão → Nome → Cristalização
// =========================================================================

using System;
using UnityEngine;
using Sanctuary.Core;
using Sanctuary.Data;

namespace Sanctuary.Hero
{
    /// <summary>
    /// Controla o fluxo de criação de personagem em 6 fases rituais.
    /// Port direto das 6 fases do ritual em MenuPrincipal.html.
    /// </summary>
    public class CharacterCreation : MonoBehaviour
    {
        [Header("Fase Atual")]
        [SerializeField] private int currentPhase = 1;
        public int CurrentPhase => currentPhase;

        [Header("Dados da Criação (acumulados entre fases)")]
        [SerializeField] private HeroRace selectedRace;
        [SerializeField] private HeroClass selectedClass;
        [SerializeField] private string selectedProfession;
        [SerializeField] private string heroName;

        [Header("Database")]
        [SerializeField] private RaceData[] availableRaces;
        [SerializeField] private ClassData[] availableClasses;

        // Eventos para a UI se inscrever
        public event Action<int> OnPhaseChanged;
        public event Action<RaceData> OnRaceSelected;
        public event Action<ClassData> OnClassSelected;
        public event Action<ProfessionData> OnProfessionSelected;
        public event Action<string> OnNameConfirmed;
        public event Action<HeroSaveData> OnCreationCompleted;

        // ====================== FASE 1: LORE INTRO ======================

        /// <summary>
        /// Inicia o ritual de criação.
        /// Port de: fase 1 do ritual (intro lore)
        /// </summary>
        public void StartRitual()
        {
            currentPhase = 1;
            OnPhaseChanged?.Invoke(1);
            EventBus.BGMRequested("duality");
            Debug.Log("[CharacterCreation] 🔮 Ritual de Criação iniciado — Fase 1: Intro");
        }

        // ====================== FASE 2: RAÇA ======================

        /// <summary>
        /// Seleciona uma raça.
        /// Port de: ritual-phase-2 (cartas de raça)
        /// </summary>
        public void SelectRace(HeroRace race)
        {
            selectedRace = race;
            RaceData raceData = GetRaceData(race);
            OnRaceSelected?.Invoke(raceData);
            Debug.Log($"[CharacterCreation] Raça selecionada: {race}");
        }

        /// <summary>
        /// Confirma raça e avança.
        /// </summary>
        public void ConfirmRace()
        {
            GoToPhase(3);
        }

        // ====================== FASE 3: CLASSE ======================

        /// <summary>
        /// Seleciona uma classe.
        /// Port de: ritual-phase-3 (cartas de classe com tarot)
        /// </summary>
        public void SelectClass(HeroClass cls)
        {
            selectedClass = cls;
            ClassData classData = GetClassData(cls);
            OnClassSelected?.Invoke(classData);
            Debug.Log($"[CharacterCreation] Classe selecionada: {cls}");
        }

        public void ConfirmClass()
        {
            GoToPhase(4);
        }

        // ====================== FASE 4: PROFISSÃO ======================

        /// <summary>
        /// Retorna as profissões disponíveis para a classe selecionada.
        /// Port de: ritual-phase-4 (5 profissões por classe)
        /// </summary>
        public ProfessionData[] GetAvailableProfessions()
        {
            ClassData classData = GetClassData(selectedClass);
            return classData?.professions;
        }

        public void SelectProfession(string profName)
        {
            selectedProfession = profName;
            Debug.Log($"[CharacterCreation] Profissão selecionada: {profName}");
        }

        public void ConfirmProfession()
        {
            GoToPhase(5);
        }

        // ====================== FASE 5: NOME (PACTO DE SANGUE) ======================

        /// <summary>
        /// Define o nome do herói.
        /// Port de: ritual-phase-5 (input de nome + rune echo)
        /// </summary>
        public bool SetHeroName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 20)
            {
                EventBus.ToastRequested("⚠️ Nome inválido! (1-20 caracteres)", "warning");
                return false;
            }

            heroName = name.Trim();
            OnNameConfirmed?.Invoke(heroName);
            Debug.Log($"[CharacterCreation] Nome: {heroName}");
            return true;
        }

        public void ConfirmName()
        {
            if (!string.IsNullOrWhiteSpace(heroName))
                GoToPhase(6);
        }

        // ====================== FASE 6: CRISTALIZAÇÃO ======================

        /// <summary>
        /// Sela o pacto e cria o herói definitivamente.
        /// Port de: sealThePact()
        /// </summary>
        public void SealThePact()
        {
            Debug.Log($"[CharacterCreation] ⚡ SELANDO O PACTO: {heroName} ({selectedRace} {selectedClass} - {selectedProfession})");

            // Criar o herói
            HeroSaveData newHero = HeroManager.Instance.CreateNewHero(
                heroName, selectedClass, selectedRace, selectedProfession
            );

            // Salvar no slot ativo
            if (GameManager.Instance.saveManager != null)
            {
                GameManager.Instance.saveManager.SaveGame(
                    GameManager.Instance.ActiveSlotIndex, newHero
                );
            }

            // Efeito de cristalização
            EventBus.SFXRequested("level_up");
            EventBus.ToastRequested($"⚡ {heroName}, o Nephalem, despertou!", "legendary");

            OnCreationCompleted?.Invoke(newHero);

            // Transição para o jogo
            GameManager.Instance.ChangeState(GameState.Exploration);

            Debug.Log("[CharacterCreation] ✅ Ritual completo! Entrando no Santuário...");
        }

        /// <summary>
        /// Cancela o ritual e volta ao menu.
        /// </summary>
        public void CancelRitual()
        {
            currentPhase = 1;
            heroName = "";
            selectedProfession = "";
            OnPhaseChanged?.Invoke(1);
            GameManager.Instance.ChangeState(GameState.MainMenu);
            Debug.Log("[CharacterCreation] ❌ Ritual cancelado");
        }

        // ====================== NAVEGAÇÃO ======================

        public void GoToPhase(int phase)
        {
            if (phase < 1 || phase > 6) return;

            // Validações
            if (phase == 3 && selectedRace == default) return;
            if (phase == 4 && selectedClass == default) return;
            if (phase == 5 && string.IsNullOrEmpty(selectedProfession)) return;
            if (phase == 6 && string.IsNullOrEmpty(heroName)) return;

            currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
            Debug.Log($"[CharacterCreation] → Fase {phase}");
        }

        public void GoBack()
        {
            if (currentPhase > 1)
                GoToPhase(currentPhase - 1);
        }

        // ====================== HELPERS ======================

        private RaceData GetRaceData(HeroRace race)
        {
            if (availableRaces == null) return null;
            foreach (var r in availableRaces)
                if (r.raceType == race) return r;
            return null;
        }

        private ClassData GetClassData(HeroClass cls)
        {
            if (availableClasses == null) return null;
            foreach (var c in availableClasses)
                if (c.classType == cls) return c;
            return null;
        }

        // ====================== SUMMARY (Para a UI da Fase 6) ======================

        public CreationSummary GetSummary()
        {
            return new CreationSummary
            {
                heroName = heroName,
                race = selectedRace,
                heroClass = selectedClass,
                profession = selectedProfession,
                raceData = GetRaceData(selectedRace),
                classData = GetClassData(selectedClass)
            };
        }
    }

    [Serializable]
    public class CreationSummary
    {
        public string heroName;
        public HeroRace race;
        public HeroClass heroClass;
        public string profession;
        public RaceData raceData;
        public ClassData classData;
    }
}
