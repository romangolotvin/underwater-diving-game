using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Submerged open-ocean shop platform (no shore pier). Textured deck + legs + beacon.
    /// </summary>
    public static class UnderwaterShopPlatform
    {
        public static GameObject Build(Transform parent, Vector3 pos, string name, Color tint)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            var deckMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                tint, 0.4f, 0.2f, new Vector2(2.5f, 2.5f));
            var metalMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.55f, 0.6f, 0.65f), 0.65f, 0.55f, new Vector2(1.5f, 1.5f));
            var accentMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                Color.Lerp(tint, new Color(0.2f, 0.9f, 0.85f), 0.45f), 0.5f, 0.15f, new Vector2(1f, 1f));

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(root.transform, false);
            deck.transform.localScale = new Vector3(6.2f, 0.4f, 4.6f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = deckMat;

            // Four legs down toward the abyss (visual only — no shore).
            Vector3[] legs =
            {
                new Vector3(-2.4f, -4f, -1.6f),
                new Vector3(2.4f, -4f, -1.6f),
                new Vector3(-2.4f, -4f, 1.6f),
                new Vector3(2.4f, -4f, 1.6f)
            };
            for (int i = 0; i < legs.Length; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg" + i;
                Object.Destroy(leg.GetComponent<Collider>());
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = legs[i];
                leg.transform.localScale = new Vector3(0.28f, 4f, 0.28f);
                leg.GetComponent<MeshRenderer>().sharedMaterial = metalMat;
            }

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            Object.Destroy(rail.GetComponent<Collider>());
            rail.transform.SetParent(root.transform, false);
            rail.transform.localPosition = new Vector3(0f, 0.55f, -2.1f);
            rail.transform.localScale = new Vector3(5.8f, 0.12f, 0.12f);
            rail.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Beacon";
            Object.Destroy(beacon.GetComponent<Collider>());
            beacon.transform.SetParent(root.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            beacon.transform.localScale = new Vector3(0.2f, 0.7f, 0.2f);
            beacon.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            var lightGo = new GameObject("BeaconLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 2.2f;
            light.color = new Color(0.35f, 0.95f, 0.9f);

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "SignPanel";
            Object.Destroy(sign.GetComponent<Collider>());
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 0.85f, 1.9f);
            sign.transform.localScale = new Vector3(2.4f, 0.9f, 0.08f);
            sign.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            return root;
        }
    }
}
