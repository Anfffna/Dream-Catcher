using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string firstSceneName = "House";

    [Header("Quit Panel")]
    public GameObject quitPanel; //  панель выхода (по умолчанию скрыта)

    private void Start()
    {
        // Скрываем панель выхода при старте
        if (quitPanel != null)
            quitPanel.SetActive(false);
    }

    // Новая игра
    public void StartGame()
    {
        if (GlobalLoadingManager.Instance != null)
            GlobalLoadingManager.Instance.StartLoading(firstSceneName);
        else
            SceneManager.LoadScene(firstSceneName);
    }

    // Показать панель выхода (вызывается с кнопки "Выйти")
    public void OnQuitButton()
    {
        if (quitPanel != null)
            quitPanel.SetActive(true);
    }

    // Подтвердить выход (вызывается с кнопки "Да")
    public void ConfirmQuit()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // остановить в редакторе
#else
        Application.Quit(); // закрыть приложение в сборке
#endif
    }

    // Отменить выход (вызывается с кнопки "Нет")
    public void CancelQuit()
    {
        if (quitPanel != null)
            quitPanel.SetActive(false);
    }
}