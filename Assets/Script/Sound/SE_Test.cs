using UnityEngine;

public class SE_Test : MonoBehaviour
{
    public AudioSource testSource;
    public AudioClip testClip;

    void Start()
    {
        testSource.outputAudioMixerGroup = null; // ←Mixerグループを一切経由しない
        testSource.volume = 1;
        testSource.PlayOneShot(testClip);
    }
}
