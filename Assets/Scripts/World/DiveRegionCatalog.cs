using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Country travel sites (separate from OpenOcean default). Draft fauna lists.
    /// </summary>
    public enum DiveRegionId
    {
        Egypt,
        Greece,
        China,
        Australia,
        FreshwaterLake
    }

    public readonly struct DiveRegionDef
    {
        public readonly DiveRegionId Id;
        public readonly string CountryName;
        public readonly string WaterName;
        public readonly WaterSalinity Salinity;
        public readonly Vector3 Spawn;
        public readonly Vector2 MapAnchor;
        public readonly Color WaterTint;
        public readonly FishSpecies[] Fauna;

        public DiveRegionDef(
            DiveRegionId id, string country, string water, WaterSalinity salinity,
            Vector3 spawn, Vector2 mapAnchor, Color waterTint, FishSpecies[] fauna)
        {
            Id = id;
            CountryName = country;
            WaterName = water;
            Salinity = salinity;
            Spawn = spawn;
            MapAnchor = mapAnchor;
            WaterTint = waterTint;
            Fauna = fauna;
        }
    }

    public static class DiveRegionCatalog
    {
        public static readonly DiveRegionDef[] All =
        {
            new(DiveRegionId.Egypt, "Egypt", "Red Sea", WaterSalinity.Salt,
                new Vector3(0f, -8f, -12f), new Vector2(0.56f, 0.48f), new Color(0.05f, 0.45f, 0.55f),
                new[]
                {
                    FishSpecies.ReefChromis, FishSpecies.Clownfish, FishSpecies.Parrotfish,
                    FishSpecies.MorayEel, FishSpecies.ReefShark, FishSpecies.EagleRay
                }),
            new(DiveRegionId.Greece, "Greece", "Aegean Sea", WaterSalinity.Salt,
                new Vector3(160f, -7f, 40f), new Vector2(0.54f, 0.58f), new Color(0.08f, 0.4f, 0.62f),
                new[]
                {
                    FishSpecies.MedGrouper, FishSpecies.Mackerel, FishSpecies.Octopus,
                    FishSpecies.Dolphin, FishSpecies.Barracuda, FishSpecies.Jellyfish
                }),
            new(DiveRegionId.China, "China", "South China Sea", WaterSalinity.Salt,
                new Vector3(-140f, -8f, 80f), new Vector2(0.78f, 0.52f), new Color(0.06f, 0.35f, 0.48f),
                new[]
                {
                    FishSpecies.Squid, FishSpecies.Cuttlefish, FishSpecies.Hammerhead,
                    FishSpecies.Lionfish, FishSpecies.Pufferfish, FishSpecies.Tuna
                }),
            new(DiveRegionId.Australia, "Australia", "Coral Sea", WaterSalinity.Salt,
                new Vector3(90f, -9f, -140f), new Vector2(0.86f, 0.28f), new Color(0.04f, 0.5f, 0.58f),
                new[]
                {
                    FishSpecies.GreatWhite, FishSpecies.TigerShark, FishSpecies.WhaleShark,
                    FishSpecies.MantaRay, FishSpecies.SeaTurtle, FishSpecies.Marlin
                }),
            new(DiveRegionId.FreshwaterLake, "Canada", "Great Lakes", WaterSalinity.Fresh,
                new Vector3(-200f, -6f, 20f), new Vector2(0.22f, 0.62f), new Color(0.15f, 0.35f, 0.4f),
                new[]
                {
                    FishSpecies.Pike, FishSpecies.Perch, FishSpecies.Trout, FishSpecies.Bass,
                    FishSpecies.Catfish, FishSpecies.Salmon, FishSpecies.Crayfish, FishSpecies.Sturgeon
                }),
        };
    }
}
