using System.Collections.Generic;
using UnityEngine;

namespace Sound
{
    public enum SoundType
    {
        SE,   // 効果音
        BGM   // BGM
    }

    [System.Serializable]
    public class SoundData
    {
        public string name;
        public AudioClip clip;
        public SoundType type;
    }

    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("サウンドデータリスト")]
        [SerializeField] private List<SoundData> soundList;

        [Header("BGM用AudioSource")]
        [SerializeField] private AudioSource bgmSource;

        [Header("SE用AudioSource(複数可)")]
        [SerializeField] private List<AudioSource> seSources;
        [SerializeField] private int seSourcePoolSize = 5;

        private Dictionary<string, SoundData> soundDict = new Dictionary<string, SoundData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 辞書化
            foreach (var s in soundList)
            {
                if (!soundDict.ContainsKey(s.name))
                    soundDict.Add(s.name, s);
            }

            // SE用AudioSourceプール
            if (seSources == null || seSources.Count < seSourcePoolSize)
            {
                seSources = new List<AudioSource>();
                for (int i = 0; i < seSourcePoolSize; i++)
                {
                    var src = gameObject.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    seSources.Add(src);
                }
            }
        }

        // BGM再生
        public void PlayBGM(string name, bool loop = true)
        {
            if (!soundDict.TryGetValue(name, out var data) || data.type != SoundType.BGM) return;
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }
            bgmSource.clip = data.clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        // BGM停止
        public void StopBGM()
        {
            if (bgmSource != null)
                bgmSource.Stop();
        }

        // SE再生
        public void PlaySE(string name, float volume = 1f)
        {
            if (!soundDict.TryGetValue(name, out var data) || data.type != SoundType.SE) return;
            foreach (var src in seSources)
            {
                if (!src.isPlaying)
                {
                    src.clip = data.clip;
                    src.volume = volume;
                    src.Play();
                    return;
                }
            }
        }

        // 全SE停止
        public void StopAllSE()
        {
            foreach (var src in seSources)
            {
                src.Stop();
            }
        }
    }
}