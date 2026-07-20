using UnityEngine;
using UnityEngine.Video;

public class MainMenuTVVideoBackground : MonoBehaviour
{
    [System.Serializable]
    public class SceneVideo
    {
        public string sceneName;
        public VideoClip videoClip;
    }

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Default")]
    public VideoClip defaultVideo;

    [Header("Scene Videos")]
    public SceneVideo[] sceneVideos;

    [Header("PlayerPrefs")]
    public string lastGameplaySceneKey = "LastGameplaySceneName";

    [Header("Debug")]
    public bool debugLogs = false;

    private void Start()
    {
        PlayVideoForLastScene();
    }

    public void PlayVideoForLastScene()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("MainMenuTVVideoBackground: VideoPlayer не назначен.", this);
            return;
        }

        string lastSceneName = PlayerPrefs.GetString(lastGameplaySceneKey, "");

        VideoClip selectedClip = GetVideoForScene(lastSceneName);

        if (selectedClip == null)
            selectedClip = defaultVideo;

        if (selectedClip == null)
        {
            Debug.LogWarning("MainMenuTVVideoBackground: видео не назначено.", this);
            return;
        }

        videoPlayer.Stop();

        videoPlayer.clip = selectedClip;
        videoPlayer.isLooping = true;

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();

        if (debugLogs)
            Debug.Log($"MainMenuTVVideoBackground: последн€€ сцена '{lastSceneName}', видео '{selectedClip.name}'.", this);
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;
        source.Play();
    }

    private VideoClip GetVideoForScene(string sceneName)
    {
        if (sceneVideos == null)
            return null;

        for (int i = 0; i < sceneVideos.Length; i++)
        {
            if (sceneVideos[i] == null)
                continue;

            if (sceneVideos[i].sceneName == sceneName)
                return sceneVideos[i].videoClip;
        }

        return null;
    }
}