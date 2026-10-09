// =========================================================================
//  SANCTUARY — Enumerações Globais
//  Portado de: constants.js, database.js, main_v3.js
// =========================================================================

namespace Sanctuary
{
    // ====================== ESTADOS DO JOGO ======================
    public enum GameState
    {
        MainMenu,
        CharacterCreation,
        Exploration,
        Combat,
        BossFight,
        Victory,
        Death,
        GameOver,
        Forge,
        Alchemy,
        Inventory,
        Bestiary,
        Quests,
        Pantheon,
        Lore,
        Achievements
    }

    // ====================== ATRIBUTOS ======================
    public enum StatType
    {
        Forca,
        Constituicao,
        Agilidade,
        Inteligencia,
        Sabedoria
    }

    // ====================== TIPOS ELEMENTAIS ======================
    public enum ElementType
    {
        Fisico,
        Fogo,
        Gelo,
        Raio,
        Venenosa,
        Luz,
        Profano,
        Arcano,
        Suporte,
        Cura
    }

    // ====================== CLASSES ======================
    public enum HeroClass
    {
        Guerreiro,
        Arcanista,
        Ranger,
        Barbaro,
        Paladino,
        Necromante
    }

    // ====================== RAÇAS ======================
    public enum HeroRace
    {
        Humano,
        ElfoSombrio,
        AnaoDaForja,
        OrcDecaido
    }

    // ====================== BIOMAS ======================
    public enum BiomeType
    {
        CatacumbasSombrias,
        PantanoDePeste,
        CavernasDeMagma,
        AbismoDeCristal
    }

    // ====================== RARIDADE DE ITENS ======================
    public enum ItemRarity
    {
        Comum,       // Cinza
        Incomum,     // Verde
        Raro,        // Azul
        Epico,       // Roxo
        Lendario,    // Dourado
        Mitico       // Vermelho
    }

    // ====================== SLOTS DE EQUIPAMENTO ======================
    public enum EquipSlot
    {
        Arma,
        Armadura,
        Amuleto,
        Anel,
        Botas
    }

    // ====================== TIPOS DE IA ======================
    public enum AIType
    {
        Aggressive,
        Defensive,
        Caster,
        Balanced
    }

    // ====================== AFIXOS DE ELITE ======================
    public enum EliteAffix
    {
        None,
        Vampiric,
        Armored,
        Agile
    }

    // ====================== STATUS EFFECTS ======================
    public enum StatusType
    {
        Poison,
        Burn,
        Bleed,
        Stun,
        Frozen,
        AtkBuff,
        DefBuff,
        AtkDebuff,
        DefDebuff,
        Regen,
        Shield,
        Vampiric,
        Thorns,
        DodgeBuff
    }

    // ====================== MATERIAIS ======================
    public enum MaterialID
    {
        Ferro,
        Cobre,
        Carvao,
        Couro,
        Prata,
        OuroBruto,
        Quartzo,
        Escamas,
        Mithril,
        Esmeralda,
        Rubi,
        PanoEspectral,
        Adamantium,
        Platina,
        Diamante,
        ChifreDemoniaco,
        EssenciaMenor,
        EssenciaMaior,
        EssenciaEpica,
        LagrimaDivina,
        // Ingredientes Alquímicos
        ErvaSangue,
        GosmaToxica,
        FlorLotus,
        RaizForte,
        CogumeloUmbrifugo
    }

    // ====================== AÇÃO DE COMBATE DO JOGADOR ======================
    public enum PlayerCombatAction
    {
        Attack,
        Skill,
        Defend,
        UseItem,
        Flee
    }
}
