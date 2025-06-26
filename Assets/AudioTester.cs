using Sirenix.OdinInspector;
using UnityEngine;

public class AudioTester : MonoBehaviour
{
    public AudioClip TestClip;
    public AudioSource TestAudioSource;
    
    [Button]
    public void PlayAudio()
    {
        TestAudioSource.clip = TestClip;
        TestAudioSource.Play();
    }
}
