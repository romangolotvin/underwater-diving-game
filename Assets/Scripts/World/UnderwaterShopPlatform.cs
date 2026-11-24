using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Beach shop stall for the sunny shore arc (deck on sand + awning + vendor NPC).
    /// Name kept for call-site compatibility with GearMarket / DecorShop.
    /// </summary>
    public static class UnderwaterShopPlatform
    {
        public static GameObject Build(Transform parent, Vector3 pos, string name, Color tint)
        {
            return Build(parent, pos, name, tint, 0f);
        }

        public static GameObject Build(Transform parent, Vector3 pos, string name, Color tint, float yawDegrees)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            var deckMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                Color.Lerp(tint, new Color(0.85f, 0.78f, 0.62f), 0.35f), 0.45f, 0.18f, new Vector2(2.5f, 2.5f));
            var woodMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.55f, 0.42f, 0.28f), 0.35f, 0.12f, new Vector2(1.5f, 1.5f));
            var clothMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                Color.Lerp(tint, new Color(0.95f, 0.85f, 0.55f), 0.4f), 0.4f, 0.08f, new Vector2(1f, 1f));
            var accentMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                Color.Lerp(tint, new Color(0.2f, 0.85f, 0.8f), 0.35f), 0.5f, 0.15f, new Vector2(1f, 1f));

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(root.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            deck.transform.localScale = new Vector3(5.4f, 0.28f, 4.2f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = deckMat;

            // Short stilts into sand (beach, not abyss).
            Vector3[] legs =
            {
                new Vector3(-2.1f, -0.55f, -1.5f),
                new Vector3(2.1f, -0.55f, -1.5f),
                new Vector3(-2.1f, -0.55f, 1.5f),
                new Vector3(2.1f, -0.55f, 1.5f)
            };
            for (int i = 0; i < legs.Length; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg" + i;
                Object.Destroy(leg.GetComponent<Collider>());
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = legs[i];
                leg.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
                leg.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            Object.Destroy(counter.GetComponent<Collider>());
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.7f, 1.15f);
            counter.transform.localScale = new Vector3(3.6f, 0.7f, 0.55f);
            counter.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awning.name = "Awning";
            Object.Destroy(awning.GetComponent<Collider>());
            awning.transform.SetParent(root.transform, false);
            awning.transform.localPosition = new Vector3(0f, 2.05f, 0.2f);
            awning.transform.localScale = new Vector3(5.2f, 0.08f, 3.8f);
            awning.GetComponent<MeshRenderer>().sharedMaterial = clothMat;

            var postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.name = "PostL";
            Object.Destroy(postL.GetComponent<Collider>());
            postL.transform.SetParent(root.transform, false);
            postL.transform.localPosition = new Vector3(-2.2f, 1.1f, -1.4f);
            postL.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);
            postL.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.name = "PostR";
            Object.Destroy(postR.GetComponent<Collider>());
            postR.transform.SetParent(root.transform, false);
            postR.transform.localPosition = new Vector3(2.2f, 1.1f, -1.4f);
            postR.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);
            postR.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "SignPanel";
            Object.Destroy(sign.GetComponent<Collider>());
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 1.35f, 1.45f);
            sign.transform.localScale = new Vector3(2.2f, 0.7f, 0.08f);
            sign.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            var lightGo = new GameObject("StallLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.35f, 0.3f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.92f, 0.75f);

            ShopVendorVisual.Create(root.transform, new Vector3(0f, 1.15f, 0.15f), name);
            return root;
        }
    }

    /// <summary>Shared beach / stall vendor NPC body (approach to trade).</summary>
    public static class ShopVendorVisual
    {
        public static GameObject Create(Transform parent, Vector3 localPos, string label)
        {
            var npc = new GameObject("Vendor_" + label);
            npc.transform.SetParent(parent, false);
            npc.transform.localPosition = localPos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(npc.transform, false);
            body.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            body.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SuitAlbedo, ProceduralTextures.SuitNormal,
                new Color(0.95f, 0.9f, 0.75f), 0.4f, 0.12f, new Vector2(1.5f, 1.5f));

            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Tank";
            Object.Destroy(tank.GetComponent<Collider>());
            tank.transform.SetParent(npc.transform, false);
            tank.transform.localPosition = new Vector3(0f, 0.15f, -0.22f);
            tank.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tank.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            tank.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.7f, 0.75f, 0.8f), 0.55f, 0.45f, new Vector2(1f, 1f));

            var helm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helm.name = "Helmet";
            Object.Destroy(helm.GetComponent<Collider>());
            helm.transform.SetParent(npc.transform, false);
            helm.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            helm.transform.localScale = Vector3.one * 0.42f;
            helm.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.55f, 0.75f, 0.9f), 0.75f, 0.35f, new Vector2(1f, 1f));

            npc.AddComponent<VendorBob>();
            return npc;
        }
    }
}
