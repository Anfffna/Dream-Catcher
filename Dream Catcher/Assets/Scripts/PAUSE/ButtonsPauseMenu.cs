using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsPauseMenu : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonClickSound;

    // ----- Кнопка "Продолжить" -----
    public void OnResume()
    {
        PlayButtonSound();
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }

    // ----- Кнопка "Сохранить" -----
    public void OnSave()
    {
        PlayButtonSound();
        if (PauseManager.Instance != null)
            PauseManager.Instance.ShowSavePanel();
    }

    // ----- Кнопка "Загрузить" -----
    public void OnLoad()
    {
        PlayButtonSound();
        if (PauseManager.Instance != null)
            PauseManager.Instance.ShowDownloadPanel();
    }

    // ----- Кнопка "Настройки" -----
    public void OnSettings()
    {
        PlayButtonSound();
        if (PauseManager.Instance != null)
            PauseManager.Instance.ShowSettingsPanel();

        // Обновляем ссылки на слайдеры в текущей сцене
        SettingsManager.Instance?.RefreshSliders();
    }

    // ----- Кнопка "Главное меню" -----
    public void OnMainMenu()
    {
        PlayButtonSound();
        // Выходим из паузы (возвращаем время)
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();

        // Сбрасываем выделение индикаторов
        IndicatorHover.ResetSelection();

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.StartLoading(
                "MainMenu"
            );
        }
        else
        {
            SceneManager.LoadScene(
                "MainMenu"
            );
        }
    }

    private void PlayButtonSound()
    {
        if (buttonAudioSource != null && buttonClickSound != null)
        {
            buttonAudioSource.PlayOneShot(buttonClickSound);
        }
    }
}