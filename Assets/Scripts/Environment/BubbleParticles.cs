using UnityEngine;

namespace UnderwaterDiving
{
    public static class BubbleParticles
    {
        public static ParticleSystem CreateAroundPlayer(Transform player)
        {
            var go = new GameObject("Bubbles");
            go.transform.SetParent(player, false);
            go.transform.localPosition = new Vector3(0f, -0.2f, 0.3f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            main.startColor = new Color(0.75f, 0.9f, 1f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.15f;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.15f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            if (mat.HasProperty("_MainTex"))
                mat.mainTexture = ProceduralTextures.WaterAlbedo;
            mat.color = new Color(0.8f, 0.95f, 1f, 0.5f);
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            go.AddComponent<BubbleDrive>();
            return ps;
        }
    }

    public class BubbleDrive : MonoBehaviour
    {
        ParticleSystem ps;

        void Awake() => ps = GetComponent<ParticleSystem>();

        void Update()
        {
            var swim = SwimController.Instance;
            if (ps == null || swim == null)
                return;
            var emission = ps.emission;
            float speed = swim.Velocity.magnitude;
            emission.rateOverTime = swim.IsUnderwater ? Mathf.Lerp(4f, 28f, speed / 8f) : 0f;
        }
    }
}
