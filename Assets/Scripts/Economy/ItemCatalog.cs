using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    public enum ItemKind
    {
        FishCatch,
        Artifact,
        GearUpgrade,
        Cosmetic
    }

    public enum FishSpecies
    {
        ReefChromis,
        AmberJack,
        AbyssGrouper,
        MorayEel,
        SeaTurtle,
        EagleRay,
        Barracuda
    }

    public enum ArtifactId
    {
        CoralCharm,
        BronzeCompass,
        AbyssIdol
    }

    public enum GearUpgradeId
    {
        AirTank,
        DepthSuit,
        FinBoost,
        BuoyancyVest,
        LampBeam,
        CatchBag,
        RopeLength,
        SpearSharpness,
        SpearWeight
    }

    public enum CosmeticId
    {
        ReefStripe,
        GoldWrist,
        AbyssPaint,
        PearlCharm,
        NeonFins,
        ShellMask
    }

    public readonly struct ItemDef
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly ItemKind Kind;
        public readonly int SellPrice;
        public readonly int BuyPrice;

        public ItemDef(string id, string displayName, ItemKind kind, int sellPrice, int buyPrice = 0)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            SellPrice = sellPrice;
            BuyPrice = buyPrice;
        }
    }

    /// <summary>
    /// In-world item names and prices only — no meta/prompt text.
    /// </summary>
    public static class ItemCatalog
    {
        public static readonly ItemDef ReefChromis = new("fish_reef", "Reef Chromis", ItemKind.FishCatch, 12);
        public static readonly ItemDef AmberJack = new("fish_jack", "Amber Jack", ItemKind.FishCatch, 28);
        public static readonly ItemDef AbyssGrouper = new("fish_grouper", "Abyss Grouper", ItemKind.FishCatch, 55);
        public static readonly ItemDef MorayEel = new("fish_moray", "Moray Eel", ItemKind.FishCatch, 42);
        public static readonly ItemDef SeaTurtle = new("fish_turtle", "Sea Turtle", ItemKind.FishCatch, 70);
        public static readonly ItemDef EagleRay = new("fish_ray", "Eagle Ray", ItemKind.FishCatch, 60);
        public static readonly ItemDef Barracuda = new("fish_cuda", "Barracuda", ItemKind.FishCatch, 85);

        public static readonly ItemDef CoralCharmArt = new("art_charm", "Coral Charm", ItemKind.Artifact, 40);
        public static readonly ItemDef BronzeCompass = new("art_compass", "Bronze Compass", ItemKind.Artifact, 95);
        public static readonly ItemDef AbyssIdol = new("art_idol", "Abyss Idol", ItemKind.Artifact, 180);

        public static readonly ItemDef UpAir = new("up_air", "Air Tank", ItemKind.GearUpgrade, 0, 110);
        public static readonly ItemDef UpDepth = new("up_depth", "Depth Suit", ItemKind.GearUpgrade, 0, 140);
        public static readonly ItemDef UpFins = new("up_fins", "Fin Boost", ItemKind.GearUpgrade, 0, 130);
        public static readonly ItemDef UpBuoyancy = new("up_buoy", "Buoyancy Vest", ItemKind.GearUpgrade, 0, 125);
        public static readonly ItemDef UpLamp = new("up_lamp", "Dive Lamp", ItemKind.GearUpgrade, 0, 100);
        public static readonly ItemDef UpBag = new("up_bag", "Catch Bag", ItemKind.GearUpgrade, 0, 90);
        public static readonly ItemDef UpRope = new("up_rope", "Line Length", ItemKind.GearUpgrade, 0, 120);
        public static readonly ItemDef UpSharp = new("up_sharp", "Harpoon Edge", ItemKind.GearUpgrade, 0, 135);
        public static readonly ItemDef UpWeight = new("up_weight", "Harpoon Mass", ItemKind.GearUpgrade, 0, 115);

        public static readonly ItemDef CosReef = new("cos_reef", "Reef Stripe", ItemKind.Cosmetic, 0, 75);
        public static readonly ItemDef CosGold = new("cos_gold", "Gold Wrist", ItemKind.Cosmetic, 0, 90);
        public static readonly ItemDef CosAbyss = new("cos_abyss", "Abyss Paint", ItemKind.Cosmetic, 0, 110);
        public static readonly ItemDef CosPearl = new("cos_pearl", "Pearl Charm", ItemKind.Cosmetic, 0, 95);
        public static readonly ItemDef CosNeon = new("cos_neon", "Neon Fins", ItemKind.Cosmetic, 0, 85);
        public static readonly ItemDef CosShell = new("cos_shell", "Shell Mask", ItemKind.Cosmetic, 0, 100);

        static readonly Dictionary<string, ItemDef> ById = BuildMap();

        static Dictionary<string, ItemDef> BuildMap()
        {
            var d = new Dictionary<string, ItemDef>();
            void Add(ItemDef def) => d[def.Id] = def;
            Add(ReefChromis); Add(AmberJack); Add(AbyssGrouper);
            Add(MorayEel); Add(SeaTurtle); Add(EagleRay); Add(Barracuda);
            Add(CoralCharmArt); Add(BronzeCompass); Add(AbyssIdol);
            Add(UpAir); Add(UpDepth); Add(UpFins); Add(UpBuoyancy); Add(UpLamp); Add(UpBag);
            Add(UpRope); Add(UpSharp); Add(UpWeight);
            Add(CosReef); Add(CosGold); Add(CosAbyss); Add(CosPearl); Add(CosNeon); Add(CosShell);
            return d;
        }

        public static bool TryGet(string id, out ItemDef def) => ById.TryGetValue(id, out def);

        public static ItemDef ForFish(FishSpecies species) => species switch
        {
            FishSpecies.AmberJack => AmberJack,
            FishSpecies.AbyssGrouper => AbyssGrouper,
            FishSpecies.MorayEel => MorayEel,
            FishSpecies.SeaTurtle => SeaTurtle,
            FishSpecies.EagleRay => EagleRay,
            FishSpecies.Barracuda => Barracuda,
            _ => ReefChromis
        };

        public static ItemDef ForArtifact(ArtifactId id) => id switch
        {
            ArtifactId.BronzeCompass => BronzeCompass,
            ArtifactId.AbyssIdol => AbyssIdol,
            _ => CoralCharmArt
        };

        public static ItemDef ForGear(GearUpgradeId id) => id switch
        {
            GearUpgradeId.DepthSuit => UpDepth,
            GearUpgradeId.FinBoost => UpFins,
            GearUpgradeId.BuoyancyVest => UpBuoyancy,
            GearUpgradeId.LampBeam => UpLamp,
            GearUpgradeId.CatchBag => UpBag,
            GearUpgradeId.RopeLength => UpRope,
            GearUpgradeId.SpearSharpness => UpSharp,
            GearUpgradeId.SpearWeight => UpWeight,
            _ => UpAir
        };

        public static ItemDef ForCosmetic(CosmeticId id) => id switch
        {
            CosmeticId.GoldWrist => CosGold,
            CosmeticId.AbyssPaint => CosAbyss,
            CosmeticId.PearlCharm => CosPearl,
            CosmeticId.NeonFins => CosNeon,
            CosmeticId.ShellMask => CosShell,
            _ => CosReef
        };

        public static readonly GearUpgradeId[] AllGear =
        {
            GearUpgradeId.AirTank,
            GearUpgradeId.DepthSuit,
            GearUpgradeId.FinBoost,
            GearUpgradeId.BuoyancyVest,
            GearUpgradeId.LampBeam,
            GearUpgradeId.CatchBag,
            GearUpgradeId.RopeLength,
            GearUpgradeId.SpearSharpness,
            GearUpgradeId.SpearWeight
        };

        public static readonly CosmeticId[] AllCosmetics =
        {
            CosmeticId.ReefStripe,
            CosmeticId.GoldWrist,
            CosmeticId.AbyssPaint,
            CosmeticId.PearlCharm,
            CosmeticId.NeonFins,
            CosmeticId.ShellMask
        };

        public static Color FishTint(FishSpecies species) => species switch
        {
            FishSpecies.AmberJack => new Color(0.75f, 0.62f, 0.28f),
            FishSpecies.AbyssGrouper => new Color(0.22f, 0.28f, 0.38f),
            FishSpecies.MorayEel => new Color(0.35f, 0.55f, 0.32f),
            FishSpecies.SeaTurtle => new Color(0.35f, 0.48f, 0.28f),
            FishSpecies.EagleRay => new Color(0.45f, 0.42f, 0.48f),
            FishSpecies.Barracuda => new Color(0.55f, 0.58f, 0.52f),
            _ => new Color(0.2f, 0.65f, 0.75f)
        };

        public static Color ArtifactTint(ArtifactId id) => id switch
        {
            ArtifactId.BronzeCompass => new Color(0.72f, 0.48f, 0.22f),
            ArtifactId.AbyssIdol => new Color(0.55f, 0.35f, 0.7f),
            _ => new Color(0.85f, 0.45f, 0.4f)
        };

        public static Color CosmeticAccent(CosmeticId id) => id switch
        {
            CosmeticId.GoldWrist => new Color(0.92f, 0.75f, 0.28f),
            CosmeticId.AbyssPaint => new Color(0.35f, 0.2f, 0.55f),
            CosmeticId.PearlCharm => new Color(0.9f, 0.92f, 0.95f),
            CosmeticId.NeonFins => new Color(0.15f, 0.95f, 0.55f),
            CosmeticId.ShellMask => new Color(0.85f, 0.7f, 0.55f),
            _ => new Color(0.15f, 0.7f, 0.85f)
        };
    }
}
