using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Layered procedural-ish underwater ambience (noise buffers). Inspired by layered
    /// rumble/wash/rush — original implementation, not a port.
    /// </summary>
    public class OceanAmbience : MonoBehaviour
    {
        AudioSource rumble;
        AudioSource wash;
        AudioSource rush;

        public static OceanAmbience Create(Transform parent)
        {
            var go = new GameObject("OceanAmbience");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<OceanAmbience>();
            a.Build();
            return a;
        }

        void Build()
        {
            rumble = MakeSource("Rumble", MakeNoiseClip(2.5f, 0.35f, true), 0.22f, 0.4f);
            wash = MakeSource("Wash", MakeNoiseClip(1.8f, 0.7f, true), 0.12f, 0.9f);
            rush = MakeSource("Rush", MakeNoiseClip(1.2f, 1.1f, true), 0f, 1.2f);
        }

        AudioSource MakeSource(string name, AudioClip clip, float vol, float pitch)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = vol;
            src.pitch = pitch;
            src.spatialBlend = 0f;
            src.Play();
            return src;
        }

        void Update()
        {
            var swim = SwimController.Instance;
            float depth = swim != null ? swim.CurrentDepth : 0f;
            bool under = swim != null && swim.IsUnderwater;
            float speed = swim != null ? swim.Velocity.magnitude : 0f;

            float underMix = under ? 1f : 0.15f;
            if (rumble != null)
                rumble.volume = Mathf.Lerp(rumble.volume, 0.18f * underMix * Mathf.Lerp(0.7f, 1.2f, depth / 60f), Time.deltaTime);
            if (wash != null)
            {
                float nearSurface = under ? Mathf.Clamp01(1f - depth / 18f) : 0.55f;
                wash.volume = Mathf.Lerp(wash.volume, 0.1f + 0.2f * nearSurface, Time.deltaTime);
            }
            if (rush != null)
                rush.volume = Mathf.Lerp(rush.volume, under ? Mathf.Clamp01(speed / 9f) * 0.28f : 0f, Time.deltaTime * 3f);
        }

        static AudioClip MakeNoiseClip(float seconds, float bright, bool brown)
        {
            int sr = 22050;
            int n = Mathf.CeilToInt(sr * seconds);
            var data = new float[n];
            float b = 0f;
            for (int i = 0; i < n; i++)
            {
                float white = Random.Range(-1f, 1f);
                if (brown)
                {
                    b = (b + 0.02f * white) / 1.02f;
                    data[i] = Mathf.Clamp(b * 3.2f * bright, -1f, 1f);
                }
                else
                {
                    data[i] = white * 0.35f * bright;
                }
            }
            // Crossfade loop ends
            int fade = Mathf.Min(2048, n / 8);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] = data[i] * k + data[n - fade + i] * (1f - k);
            }
            var clip = AudioClip.Create("OceanNoise", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
