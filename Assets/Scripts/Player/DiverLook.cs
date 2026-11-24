using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// First-person cosmetic accents (wrist, charm, gun tint hooks).
    /// </summary>
    public class DiverLook : MonoBehaviour
    {
        public static DiverLook Instance { get; private set; }

        Transform camPivot;
        GameObject wrist;
        GameObject charm;
        GameObject finTip;
        MeshRenderer wristRenderer;
        MeshRenderer charmRenderer;
        MeshRenderer finRenderer;
        Material wristMat;
        Material charmMat;
        Material finMat;

        public void Bind(Transform cameraPivot)
        {
            camPivot = cameraPivot;
            BuildPieces();
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void BuildPieces()
        {
            if (camPivot == null)
                return;

            wrist = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wrist.name = "WristBand";
            Object.Destroy(wrist.GetComponent<Collider>());
            wrist.transform.SetParent(camPivot, false);
            wrist.transform.localPosition = new Vector3(0.28f, -0.22f, 0.45f);
            wrist.transform.localRotation = Quaternion.Euler(90f, 0f, 20f);
            wrist.transform.localScale = new Vector3(0.06f, 0.04f, 0.06f);
            wristMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SuitAlbedo, ProceduralTextures.SuitNormal,
                new Color(0.2f, 0.55f, 0.65f), 0.45f, 0.2f, new Vector2(2f, 2f));
            wristRenderer = wrist.GetComponent<MeshRenderer>();
            wristRenderer.sharedMaterial = wristMat;

            charm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            charm.name = "Charm";
            Object.Destroy(charm.GetComponent<Collider>());
            charm.transform.SetParent(camPivot, false);
            charm.transform.localPosition = new Vector3(-0.18f, -0.12f, 0.35f);
            charm.transform.localScale = Vector3.one * 0.045f;
            charmMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                Color.white, 0.7f, 0.35f, new Vector2(1f, 1f));
            charmRenderer = charm.GetComponent<MeshRenderer>();
            charmRenderer.sharedMaterial = charmMat;
            charm.SetActive(false);

            finTip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finTip.name = "FinAccent";
            Object.Destroy(finTip.GetComponent<Collider>());
            finTip.transform.SetParent(camPivot, false);
            finTip.transform.localPosition = new Vector3(0.22f, -0.35f, 0.55f);
            finTip.transform.localScale = new Vector3(0.08f, 0.02f, 0.14f);
            finMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SuitAlbedo, ProceduralTextures.SuitNormal,
                new Color(0.1f, 0.2f, 0.25f), 0.4f, 0.15f, new Vector2(1f, 1f));
            finRenderer = finTip.GetComponent<MeshRenderer>();
            finRenderer.sharedMaterial = finMat;
        }

        public void ApplyCosmetic(CosmeticId? id)
        {
            if (wrist == null)
                BuildPieces();
            if (wrist == null)
                return;

            Color accent = id.HasValue ? ItemCatalog.CosmeticAccent(id.Value) : new Color(0.2f, 0.55f, 0.65f);
            wristMat.color = accent;
            finMat.color = Color.Lerp(new Color(0.1f, 0.2f, 0.25f), accent, 0.7f);
            charm.SetActive(id == CosmeticId.PearlCharm || id == CosmeticId.ShellMask);
            if (charm.activeSelf)
                charmMat.color = accent;

            if (Speargun.Instance != null)
                Speargun.Instance.ApplyCosmeticTint(accent, id);
        }

        void LateUpdate()
        {
            if (charm == null || !charm.activeSelf)
                return;
            float bob = Mathf.Sin(Time.time * 2.4f) * 0.01f;
            charm.transform.localPosition = new Vector3(-0.18f, -0.12f + bob, 0.35f);
            charm.transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.Self);
        }
    }
}
