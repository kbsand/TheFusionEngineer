using UnityEngine;

namespace TheFusionEngineer.Core
{
    public static class PersistentSfxPlayer
    {
        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            GameObject host = new($"One Shot SFX ({clip.name})");
            Object.DontDestroyOnLoad(host);
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.ignoreListenerPause = true;
            source.volume = Mathf.Clamp01(volume);
            source.clip = clip;
            source.Play();
            Object.Destroy(host, Mathf.Max(0.01f, clip.length));
        }
    }
}
