
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

namespace Sound
{
    public enum SoundType
    {
        SE,
        BGM
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

        [SerializeField] private List<SoundData> soundList;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup seGroup;

        [SerializeField] private AudioSource bgmSource1;
        [SerializeField] private AudioSource bgmSource2;
        private bool isUsingFirstBGM = true;


        [Header("SE AudioSourceの自動生成を使う場合はON")]
        [SerializeField] private bool autoCreateSESources = true;
        [SerializeField] private List<AudioSource> seSources;
        [SerializeField] private int seSourcePoolSize = 5;

        private Dictionary<string, SoundData> soundDict = new Dictionary<string, SoundData>();

        private void Awake()
        {
            //if (Instance != null && Instance != this)
            //{
            //    Destroy(gameObject);
            //    return;
            //}
            Instance = this;
            //DontDestroyOnLoad(gameObject);

            foreach (var s in soundList)
            {
                if (!soundDict.ContainsKey(s.name))
                    soundDict.Add(s.name, s);
            }

           // ここをboolで切り替え
            if (autoCreateSESources)
            {
                seSources = new List<AudioSource>();
                for (int i = 0; i < seSourcePoolSize; i++)
                {
                    var src = gameObject.AddComponent<AudioSource>();
                    src.outputAudioMixerGroup = seGroup;
                    src.playOnAwake = false;
                    src.spatialBlend = 1f;
                    seSources.Add(src);
                }
            }

            else
            {
                // InspectorでセットされているseSourcesを使う（初期化しない）
                // 数が足りなければ警告
                if (seSources == null || seSources.Count == 0)
                {
                    Debug.LogWarning("SE AudioSourceの自動生成OFFですが、seSourcesリストが空です。InspectorでAudioSourceを割り当ててください。");
                }
                else if (seSources.Count < seSourcePoolSize)
                {
                    Debug.LogWarning("SE AudioSourceリストの数がseSourcePoolSize未満です。必要に応じて追加してください。");
                }
            }
        }

        public void PlayBGM(string name, bool loop = true, float fadeDuration = 1f)
        {
            if (!soundDict.TryGetValue(name, out var data) || data.type != SoundType.BGM) return;
           
            if (data.clip == null)
            {
                Debug.LogWarning($"{name} に対応する AudioClip が null です！");
                return;
            }
            var nextSource = isUsingFirstBGM ? bgmSource2 : bgmSource1;
            var prevSource = isUsingFirstBGM ? bgmSource1 : bgmSource2;
            isUsingFirstBGM = !isUsingFirstBGM;

            nextSource.clip = data.clip;
            nextSource.loop = loop;
            nextSource.outputAudioMixerGroup = bgmGroup;
            nextSource.volume = 0f;
            Debug.Log("BGM起動");
            nextSource.Play();

            StartCoroutine(CrossFadeBGM(prevSource, nextSource, fadeDuration));
        }

        private IEnumerator CrossFadeBGM(AudioSource from, AudioSource to, float duration)
        {
            float time = 0f;
            to.volume = 0f;
            to.Play();
            while (time < duration)
            {
                float t = time / duration;
                from.volume = Mathf.Lerp(1f, 0f, t);
                to.volume = Mathf.Lerp(0f, 1f, t);
                time += Time.deltaTime;
                yield return null;
            }
            from.volume = 0f;
            from.Stop(); // フェードアウト側だけ止める
            to.volume = 1f; // フェードイン側は最大
        }

        public void StopBGM(float fadeDuration = 1f)
        {
            StartCoroutine(FadeOut(bgmSource1, fadeDuration));
            StartCoroutine(FadeOut(bgmSource2, fadeDuration));
        }

        private IEnumerator FadeOut(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float time = 0f;
            while (time < duration)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            Debug.Log("BGM停止");
            source.Stop();
            source.volume = startVolume;
        }

        public void PlaySE(string name, float volume = 1f, float pan = 0f, float pitch = 1f)
        {
            if (!soundDict.TryGetValue(name, out var data) || data.type != SoundType.SE) return;
            if (data.clip == null)
            {
                Debug.LogWarning($"{name} に対応する AudioClip が null です！");
                return;
            }
            foreach (var src in seSources)
            {
                if (!src.isPlaying)
                {
                    src.clip = data.clip;
                    src.volume = volume;
                    src.panStereo = Mathf.Clamp(pan, -1f, 1f);
                    src.outputAudioMixerGroup = seGroup;
                    src.pitch = pitch; // ←ピッチ設定
                    src.Play();
                    //Debug.Log("SE起動 pitch=" + pitch);
                    return;
                }
            }
        }

        public void StopAllSE()
        {
            foreach (var src in seSources)
                src.Stop();
        }
    }
}