using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class VideoPauseXR : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public InputActionProperty pauseAction;

    void OnEnable()
    {
        pauseAction.action.Enable();
    }

    void OnDisable()
    {
        pauseAction.action.Disable();
    }

    void Update()
    {
        if (pauseAction.action.WasPressedThisFrame())
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
            else
                videoPlayer.Play();
        }
    }
}
