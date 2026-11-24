using UnityEngine;

namespace UnderwaterDiving
{
    public class ArtifactPickup : MonoBehaviour
    {
        ArtifactId id;
        float spin;

        public static ArtifactPickup Spawn(Transform parent, ArtifactId artifactId, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Artifact_" + artifactId;
            Object.Destroy(go.GetComponent<BoxCollider>());
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.8f;

            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.35f;

            var mat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                ItemCatalog.ArtifactTint(artifactId), 0.75f, 0.55f, new Vector2(1.5f, 1.5f));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 1.1f;
            light.color = ItemCatalog.ArtifactTint(artifactId);

            var a = go.AddComponent<ArtifactPickup>();
            a.id = artifactId;
            return a;
        }

        void Update()
        {
            spin += Time.deltaTime * 50f;
            transform.rotation = Quaternion.Euler(15f, spin, 10f);
            transform.position += Vector3.up * (Mathf.Sin(Time.time * 2f + spin * 0.01f) * 0.003f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            var inv = Inventory.Instance;
            if (inv == null || !inv.CanAddCatch())
                return;
            var def = ItemCatalog.ForArtifact(id);
            inv.Add(def.Id, 1);
            Destroy(gameObject);
        }
    }
}
