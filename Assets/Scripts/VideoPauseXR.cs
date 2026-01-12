using UnityEngine;
using UnityEngine.Video;

public class VideoPauseXR : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;
    public OVRInput.Button pauseButton = OVRInput.Button.One;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioMute(0, true);
        videoPlayer.SetDirectAudioVolume(0, 0f);

        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    void Update()
    {
        if (OVRInput.GetDown(pauseButton))
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                audioSource.Pause();
            }
            else
            {
                videoPlayer.Play();
                audioSource.Play();
            }
        }
    }
}