using UnityEngine;

namespace UnderwaterDiving
{
    public enum FishSpecies
    {
        // Salt — reef / shallow
        ReefChromis, Clownfish, Parrotfish, Angelfish, Butterflyfish,
        Lionfish, Pufferfish, Seahorse, MorayEel, SeaSnake,
        // Salt — mid
        AmberJack, Mackerel, Tuna, Barracuda, Marlin,
        Squid, Cuttlefish, Jellyfish, EagleRay, MantaRay,
        SeaTurtle, Dolphin, Octopus, MedGrouper,
        // Sharks
        ReefShark, Hammerhead, TigerShark, GreatWhite, ThresherShark, WhaleShark,
        // Salt — deep
        AbyssGrouper, Lanternfish, GulperEel, GiantIsopod, VampireSquid, Anglerfish,
        // Nearshore / surface salt
        WaterSnake, Needlefish,
        // Freshwater
        Pike, Perch, Trout, Carp, Catfish, Sturgeon, FreshEel, Bass, Salmon, Crayfish
    }

    public enum CreatureBody
    {
        SmallFish, LongFish, BulkyFish, Eel, Ray, Turtle, Shark,
        Jelly, Squid, Seahorse, Snake, Dolphin, Isopod, Crayfish
    }

    public enum WaterSalinity
    {
        Salt,
        Fresh
    }

    public readonly struct CreatureStats
    {
        public readonly string ItemId;
        public readonly string DisplayName;
        public readonly int SellPrice;
        public readonly DepthBand PreferredBand;
        public readonly CreatureBody Body;
        public readonly WaterSalinity Salinity;
        public readonly bool Hostile;
        public readonly float Speed;
        public readonly float Scale;
        public readonly float ColliderRadius;
        public readonly float AttackAirDrain;
        public readonly Color Tint;

        public CreatureStats(
            string itemId, string displayName, int sellPrice, DepthBand band, CreatureBody body,
            WaterSalinity salinity, bool hostile, float speed, float scale, float radius, float drain, Color tint)
        {
            ItemId = itemId;
            DisplayName = displayName;
            SellPrice = sellPrice;
            PreferredBand = band;
            Body = body;
            Salinity = salinity;
            Hostile = hostile;
            Speed = speed;
            Scale = scale;
            ColliderRadius = radius;
            AttackAirDrain = drain;
            Tint = tint;
        }
    }

    public static class CreatureCatalog
    {
        static readonly CreatureStats[] Table = Build();

        public static CreatureStats Get(FishSpecies s) => Table[(int)s];

        public static ItemDef AsItem(FishSpecies s)
        {
            var c = Get(s);
            return new ItemDef(c.ItemId, c.DisplayName, ItemKind.FishCatch, c.SellPrice);
        }

        static CreatureStats[] Build()
        {
            var n = System.Enum.GetValues(typeof(FishSpecies)).Length;
            var t = new CreatureStats[n];

            void S(FishSpecies id, string item, string name, int price, DepthBand band, CreatureBody body,
                WaterSalinity sal, bool hostile, float speed, float scale, float radius, float drain, Color tint)
            {
                t[(int)id] = new CreatureStats(item, name, price, band, body, sal, hostile, speed, scale, radius, drain, tint);
            }

            // Salt reef
            S(FishSpecies.ReefChromis, "fish_reef", "Reef Chromis", 12, DepthBand.Shallow, CreatureBody.SmallFish, WaterSalinity.Salt, false, 2.8f, 0.9f, 0.35f, 0f, new Color(0.2f, 0.65f, 0.75f));
            S(FishSpecies.Clownfish, "fish_clown", "Clownfish", 18, DepthBand.Shallow, CreatureBody.SmallFish, WaterSalinity.Salt, false, 2.4f, 0.85f, 0.32f, 0f, new Color(0.95f, 0.45f, 0.12f));
            S(FishSpecies.Parrotfish, "fish_parrot", "Parrotfish", 32, DepthBand.Shallow, CreatureBody.BulkyFish, WaterSalinity.Salt, false, 2.6f, 1.15f, 0.5f, 0f, new Color(0.25f, 0.75f, 0.55f));
            S(FishSpecies.Angelfish, "fish_angel", "Angelfish", 26, DepthBand.Shallow, CreatureBody.SmallFish, WaterSalinity.Salt, false, 2.5f, 1f, 0.4f, 0f, new Color(0.9f, 0.85f, 0.2f));
            S(FishSpecies.Butterflyfish, "fish_butter", "Butterflyfish", 22, DepthBand.Shallow, CreatureBody.SmallFish, WaterSalinity.Salt, false, 2.7f, 0.9f, 0.35f, 0f, new Color(0.95f, 0.8f, 0.25f));
            S(FishSpecies.Lionfish, "fish_lion", "Lionfish", 48, DepthBand.Shallow, CreatureBody.BulkyFish, WaterSalinity.Salt, true, 2.2f, 1.05f, 0.48f, 10f, new Color(0.85f, 0.55f, 0.35f));
            S(FishSpecies.Pufferfish, "fish_puffer", "Pufferfish", 30, DepthBand.Shallow, CreatureBody.BulkyFish, WaterSalinity.Salt, false, 1.8f, 1f, 0.45f, 0f, new Color(0.7f, 0.75f, 0.35f));
            S(FishSpecies.Seahorse, "fish_horse", "Seahorse", 40, DepthBand.Shallow, CreatureBody.Seahorse, WaterSalinity.Salt, false, 1.2f, 0.7f, 0.28f, 0f, new Color(0.75f, 0.55f, 0.35f));
            S(FishSpecies.MorayEel, "fish_moray", "Moray Eel", 42, DepthBand.Shallow, CreatureBody.Eel, WaterSalinity.Salt, false, 1.1f, 1.1f, 0.55f, 0f, new Color(0.35f, 0.55f, 0.32f));
            S(FishSpecies.SeaSnake, "fish_seasnake", "Sea Snake", 55, DepthBand.Shallow, CreatureBody.Snake, WaterSalinity.Salt, true, 2.8f, 1f, 0.4f, 12f, new Color(0.2f, 0.35f, 0.25f));

            // Salt mid
            S(FishSpecies.AmberJack, "fish_jack", "Amber Jack", 28, DepthBand.Mid, CreatureBody.LongFish, WaterSalinity.Salt, false, 3.2f, 1.1f, 0.5f, 0f, new Color(0.75f, 0.62f, 0.28f));
            S(FishSpecies.Mackerel, "fish_mack", "Mackerel", 24, DepthBand.Mid, CreatureBody.LongFish, WaterSalinity.Salt, false, 3.6f, 1f, 0.42f, 0f, new Color(0.35f, 0.55f, 0.55f));
            S(FishSpecies.Tuna, "fish_tuna", "Tuna", 65, DepthBand.Mid, CreatureBody.BulkyFish, WaterSalinity.Salt, false, 4.0f, 1.35f, 0.7f, 0f, new Color(0.3f, 0.4f, 0.55f));
            S(FishSpecies.Barracuda, "fish_cuda", "Barracuda", 85, DepthBand.Mid, CreatureBody.LongFish, WaterSalinity.Salt, true, 4.4f, 1.2f, 0.7f, 8f, new Color(0.55f, 0.58f, 0.52f));
            S(FishSpecies.Marlin, "fish_marlin", "Marlin", 110, DepthBand.Mid, CreatureBody.LongFish, WaterSalinity.Salt, false, 4.8f, 1.45f, 0.75f, 0f, new Color(0.25f, 0.45f, 0.7f));
            S(FishSpecies.Squid, "fish_squid", "Squid", 38, DepthBand.Mid, CreatureBody.Squid, WaterSalinity.Salt, false, 2.8f, 1f, 0.5f, 0f, new Color(0.7f, 0.45f, 0.55f));
            S(FishSpecies.Cuttlefish, "fish_cuttle", "Cuttlefish", 44, DepthBand.Mid, CreatureBody.Squid, WaterSalinity.Salt, false, 2.2f, 0.95f, 0.48f, 0f, new Color(0.65f, 0.5f, 0.35f));
            S(FishSpecies.Jellyfish, "fish_jelly", "Jellyfish", 20, DepthBand.Mid, CreatureBody.Jelly, WaterSalinity.Salt, false, 1.0f, 1.1f, 0.55f, 0f, new Color(0.75f, 0.9f, 1f));
            S(FishSpecies.EagleRay, "fish_ray", "Eagle Ray", 60, DepthBand.Mid, CreatureBody.Ray, WaterSalinity.Salt, false, 2.5f, 1.2f, 0.85f, 0f, new Color(0.45f, 0.42f, 0.48f));
            S(FishSpecies.MantaRay, "fish_manta", "Manta Ray", 95, DepthBand.Mid, CreatureBody.Ray, WaterSalinity.Salt, false, 2.8f, 1.7f, 1.1f, 0f, new Color(0.2f, 0.25f, 0.35f));
            S(FishSpecies.SeaTurtle, "fish_turtle", "Sea Turtle", 70, DepthBand.Shallow, CreatureBody.Turtle, WaterSalinity.Salt, false, 1.6f, 1.2f, 0.9f, 0f, new Color(0.35f, 0.48f, 0.28f));
            S(FishSpecies.Dolphin, "fish_dolphin", "Dolphin", 120, DepthBand.Mid, CreatureBody.Dolphin, WaterSalinity.Salt, false, 5.0f, 1.5f, 0.9f, 0f, new Color(0.55f, 0.65f, 0.75f));
            S(FishSpecies.Octopus, "fish_octo", "Octopus", 70, DepthBand.Mid, CreatureBody.Squid, WaterSalinity.Salt, false, 1.8f, 1.05f, 0.55f, 0f, new Color(0.55f, 0.3f, 0.35f));
            S(FishSpecies.MedGrouper, "fish_medgroup", "Mediterranean Grouper", 50, DepthBand.Mid, CreatureBody.BulkyFish, WaterSalinity.Salt, false, 2.3f, 1.25f, 0.7f, 0f, new Color(0.45f, 0.42f, 0.38f));

            // Sharks
            S(FishSpecies.ReefShark, "shark_reef", "Reef Shark", 130, DepthBand.Shallow, CreatureBody.Shark, WaterSalinity.Salt, true, 4.2f, 1.5f, 0.95f, 14f, new Color(0.45f, 0.5f, 0.55f));
            S(FishSpecies.Hammerhead, "shark_hammer", "Hammerhead", 160, DepthBand.Mid, CreatureBody.Shark, WaterSalinity.Salt, true, 4.0f, 1.6f, 1.05f, 15f, new Color(0.4f, 0.48f, 0.52f));
            S(FishSpecies.TigerShark, "shark_tiger", "Tiger Shark", 180, DepthBand.Mid, CreatureBody.Shark, WaterSalinity.Salt, true, 4.3f, 1.75f, 1.15f, 18f, new Color(0.55f, 0.48f, 0.32f));
            S(FishSpecies.GreatWhite, "shark_white", "Great White", 220, DepthBand.Mid, CreatureBody.Shark, WaterSalinity.Salt, true, 4.6f, 2.0f, 1.3f, 22f, new Color(0.55f, 0.58f, 0.6f));
            S(FishSpecies.ThresherShark, "shark_thresh", "Thresher Shark", 150, DepthBand.Mid, CreatureBody.Shark, WaterSalinity.Salt, true, 4.5f, 1.55f, 1.0f, 12f, new Color(0.35f, 0.42f, 0.5f));
            S(FishSpecies.WhaleShark, "shark_whale", "Whale Shark", 90, DepthBand.Mid, CreatureBody.Shark, WaterSalinity.Salt, false, 2.4f, 2.6f, 1.6f, 0f, new Color(0.25f, 0.32f, 0.4f));

            // Deep salt
            S(FishSpecies.AbyssGrouper, "fish_grouper", "Abyss Grouper", 55, DepthBand.Deep, CreatureBody.BulkyFish, WaterSalinity.Salt, false, 2.4f, 1.3f, 0.75f, 0f, new Color(0.22f, 0.28f, 0.38f));
            S(FishSpecies.Lanternfish, "fish_lantern", "Lanternfish", 35, DepthBand.Deep, CreatureBody.SmallFish, WaterSalinity.Salt, false, 2.0f, 0.8f, 0.3f, 0f, new Color(0.35f, 0.55f, 0.45f));
            S(FishSpecies.GulperEel, "fish_gulper", "Gulper Eel", 75, DepthBand.Deep, CreatureBody.Eel, WaterSalinity.Salt, true, 2.2f, 1.4f, 0.65f, 10f, new Color(0.15f, 0.12f, 0.2f));
            S(FishSpecies.GiantIsopod, "fish_isopod", "Giant Isopod", 80, DepthBand.Deep, CreatureBody.Isopod, WaterSalinity.Salt, false, 1.0f, 1.1f, 0.6f, 0f, new Color(0.55f, 0.45f, 0.35f));
            S(FishSpecies.VampireSquid, "fish_vamp", "Vampire Squid", 100, DepthBand.Deep, CreatureBody.Squid, WaterSalinity.Salt, false, 2.0f, 1.15f, 0.55f, 0f, new Color(0.45f, 0.15f, 0.25f));
            S(FishSpecies.Anglerfish, "fish_angler", "Anglerfish", 115, DepthBand.Deep, CreatureBody.BulkyFish, WaterSalinity.Salt, true, 1.8f, 1.2f, 0.65f, 16f, new Color(0.35f, 0.28f, 0.2f));

            S(FishSpecies.WaterSnake, "snake_water", "Water Snake", 36, DepthBand.Shallow, CreatureBody.Snake, WaterSalinity.Salt, false, 2.5f, 0.95f, 0.35f, 0f, new Color(0.3f, 0.4f, 0.22f));
            S(FishSpecies.Needlefish, "fish_needle", "Needlefish", 28, DepthBand.Shallow, CreatureBody.LongFish, WaterSalinity.Salt, false, 3.8f, 1.05f, 0.4f, 0f, new Color(0.55f, 0.7f, 0.65f));

            // Freshwater
            S(FishSpecies.Pike, "fw_pike", "Pike", 34, DepthBand.Shallow, CreatureBody.LongFish, WaterSalinity.Fresh, true, 3.4f, 1.15f, 0.55f, 7f, new Color(0.35f, 0.5f, 0.32f));
            S(FishSpecies.Perch, "fw_perch", "Perch", 16, DepthBand.Shallow, CreatureBody.SmallFish, WaterSalinity.Fresh, false, 2.4f, 0.9f, 0.35f, 0f, new Color(0.55f, 0.45f, 0.25f));
            S(FishSpecies.Trout, "fw_trout", "Trout", 28, DepthBand.Shallow, CreatureBody.LongFish, WaterSalinity.Fresh, false, 3.0f, 1.0f, 0.42f, 0f, new Color(0.55f, 0.6f, 0.4f));
            S(FishSpecies.Carp, "fw_carp", "Carp", 22, DepthBand.Shallow, CreatureBody.BulkyFish, WaterSalinity.Fresh, false, 2.0f, 1.2f, 0.55f, 0f, new Color(0.55f, 0.42f, 0.3f));
            S(FishSpecies.Catfish, "fw_cat", "Catfish", 40, DepthBand.Mid, CreatureBody.BulkyFish, WaterSalinity.Fresh, false, 2.1f, 1.3f, 0.65f, 0f, new Color(0.35f, 0.32f, 0.28f));
            S(FishSpecies.Sturgeon, "fw_sturgeon", "Sturgeon", 95, DepthBand.Mid, CreatureBody.LongFish, WaterSalinity.Fresh, false, 2.6f, 1.55f, 0.8f, 0f, new Color(0.4f, 0.42f, 0.4f));
            S(FishSpecies.FreshEel, "fw_eel", "Freshwater Eel", 38, DepthBand.Mid, CreatureBody.Eel, WaterSalinity.Fresh, false, 1.6f, 1.05f, 0.45f, 0f, new Color(0.25f, 0.3f, 0.22f));
            S(FishSpecies.Bass, "fw_bass", "Bass", 26, DepthBand.Shallow, CreatureBody.BulkyFish, WaterSalinity.Fresh, false, 2.8f, 1.05f, 0.48f, 0f, new Color(0.35f, 0.4f, 0.28f));
            S(FishSpecies.Salmon, "fw_salmon", "Salmon", 48, DepthBand.Mid, CreatureBody.LongFish, WaterSalinity.Fresh, false, 3.5f, 1.15f, 0.55f, 0f, new Color(0.75f, 0.45f, 0.35f));
            S(FishSpecies.Crayfish, "fw_cray", "Crayfish", 14, DepthBand.Shallow, CreatureBody.Crayfish, WaterSalinity.Fresh, false, 1.2f, 0.65f, 0.3f, 0f, new Color(0.65f, 0.25f, 0.2f));

            return t;
        }
    }
}
