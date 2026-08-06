using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsPauseMenu : MonoBehaviour
{
    // ----- Кнопка "Продолжить" -----
    public void OnResume()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }

    // ----- Кнопка "Сохранить" -----
    public void OnSave()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ShowSavePanel();
    }

    // ----- Кнопка "Загрузить" -----
    public void OnLoad()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ShowDownloadPanel();
    }

    // ----- Кнопка "Настройки" -----
    public void OnSettings()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ShowSettingsPanel();

        // Обновляем ссылки на слайдеры в текущей сцене
        SettingsManager.Instance?.RefreshSliders();
    }

    // ----- Кнопка "Главное меню" -----
    public void OnMainMenu()
    {
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
}