using UnityEngine;

public class AudioDetection : MonoBehaviour
{
    public int sampleWindow = 64;
    public AudioClip microphoneClip;
    private bool _isMicAvailable = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        micrpohoneToAudio();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float GetLoudnessFromMicrpohone(int clipPosition , AudioClip clip)
    {

        int startPosition = clipPosition - sampleWindow;
        if(startPosition < 0 )
        {
            return 0;
        }
        float[] waveData = new float[sampleWindow];
        clip.GetData(waveData, startPosition);
        float totalLoudness = 0;
        for (int i = 0; i<sampleWindow; i++)
        {
            totalLoudness += Mathf.Abs(waveData[i]);
        }
        return totalLoudness / sampleWindow;
    }
     public float GetLoudnessFromMicrpohone()
     {
    
        return GetLoudnessFromMicrpohone(Microphone.GetPosition(Microphone.devices[0]), microphoneClip);
     }
    void micrpohoneToAudio()
    {
        Debug.Log("Kamera ismi: " + WebCamTexture.devices[0].name);
        string microphoneName = Microphone.devices[0];
        microphoneClip = Microphone.Start(microphoneName, true, 20, AudioSettings.outputSampleRate);
    }
}
