
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage videoRawImage;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Texture placeholderTexture;       // placeholderImage.png
    [SerializeField] private RenderTexture videoRenderTexture; // RT_Video

    private void Awake()
    {
        // Default state = placeholder
        ShowPlaceholder();
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.loopPointReached += OnFinished;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.loopPointReached -= OnFinished;
        }
    }

    public void Play()
    {
        if (!videoPlayer) return;

        // Keep placeholder until the video is prepared (avoids black frame)
        ShowPlaceholder();

        // Prepare -> when ready, OnPrepared will switch texture and play
        videoPlayer.Prepare();
    }

    public void Pause()
    {
        if (!videoPlayer) return;

        if (videoPlayer.isPlaying) videoPlayer.Pause();
        else videoPlayer.Play();
    }

    public void Stop()
    {
        if (!videoPlayer) return;

        videoPlayer.Stop();
        ShowPlaceholder();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        ShowVideo();
        vp.Play();
    }

    private void OnFinished(VideoPlayer vp)
    {
        ShowPlaceholder();
    }

    private void ShowPlaceholder()
    {
        if (videoRawImage && placeholderTexture)
            videoRawImage.texture = placeholderTexture;
    }

    private void ShowVideo()
    {
        if (videoRawImage && videoRenderTexture)
            videoRawImage.texture = videoRenderTexture;
    }
}