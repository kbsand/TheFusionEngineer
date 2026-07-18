using UnityEngine;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 씬이 바뀌어도 끊기지 않아야 하는 짧은 효과음을 임시 오브젝트에서 재생합니다.
    /// </summary>
    public static class PersistentSfxPlayer
    {
        // Play 관련 게임 로직을 수행합니다.
        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            // [런타임 자동 생성] 효과음이 끝난 뒤 함께 제거되는 일회용 재생 오브젝트입니다.
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
