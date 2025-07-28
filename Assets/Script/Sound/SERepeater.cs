using UnityEngine;
using System.Collections;
using Sound;

public class SERepeater : MonoBehaviour
{
    [Header("SEを鳴らす間隔（秒）")]
    public float interval = 1.0f;

    [Header("SEの音量 (0〜1)")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("SEのピッチ (0.5〜2)")]
    [Range(0.5f, 2f)]
    public float pitch = 1.0f;

    [Header("SEのpan (-1=左, 0=中央, 1=右)")]
    [Range(-1f, 1f)]
    public float pan = 0f;

    [Header("自動スタート")]
    public bool autoStart = true;

    [Header("繰り返すごとに間隔を短くする")]
    public bool intervalShorten = false;

    [Header("1回ごとに短縮する秒数")]
    public float intervalStep = 0.2f;

    [Header("短縮される最小間隔")]
    public float minInterval = 0.1f;

    private Coroutine repeatCoroutine;

    void Start()
    {
        if (autoStart)
        {
            StartRepeatingSE();
        }
    }

    public void StartRepeatingSE()
    {
        if (repeatCoroutine == null)
            repeatCoroutine = StartCoroutine(RepeatSECoroutine());
    }

    public void StopRepeatingSE()
    {
        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
            repeatCoroutine = null;
        }
    }

    private IEnumerator RepeatSECoroutine()
    {
        float currentInterval = interval;

        while (true)
        {
            SoundManager.Instance.PlaySE("OK", volume: volume, pan: pan, pitch: pitch);
            yield return new WaitForSeconds(currentInterval);

            if (intervalShorten)
            {
                currentInterval = Mathf.Max(minInterval, currentInterval - intervalStep);
                if (currentInterval <= minInterval)
                {
                    currentInterval = interval; // minIntervalに達したらリセット
                }
            }
        }
    }
}
