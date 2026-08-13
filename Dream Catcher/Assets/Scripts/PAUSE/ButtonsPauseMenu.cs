using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsPauseMenu : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip[] buttonClickSounds;
    private AudioSource buttonAudioSource;
    private AudioClip lastButtonClickSound;

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
        // НЕ вызываем ResumeGame().
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .HidePauseMenuBeforeLoading();
        }

        IndicatorHover.ResetSelection();

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance
                .StartLoading("MainMenu");
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState =
                CursorLockMode.Locked;

            SceneManager.LoadScene(
                "MainMenu"
            );
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSounds == null || buttonClickSounds.Length == 0)
            return;

        // Берём существующий AudioSource или создаём автоматически
        if (buttonAudioSource == null)
        {
            buttonAudioSource = GetComponent<AudioSource>();

            if (buttonAudioSource == null)
            {
                buttonAudioSource = gameObject.AddComponent<AudioSource>();
                buttonAudioSource.playOnAwake = false;
                buttonAudioSource.loop = false;
                buttonAudioSource.spatialBlend = 0f;
            }
        }

        // Считаем доступные клипы,
        // исключая null и только что проигранный
        int availableCount = 0;

        for (int i = 0; i < buttonClickSounds.Length; i++)
        {
            if (buttonClickSounds[i] != null &&
                buttonClickSounds[i] != lastButtonClickSound)
            {
                availableCount++;
            }
        }

        AudioClip selectedClip = null;

        // Есть хотя бы один другой звук
        if (availableCount > 0)
        {
            int randomIndex = Random.Range(0, availableCount);
            int currentIndex = 0;

            for (int i = 0; i < buttonClickSounds.Length; i++)
            {
                AudioClip clip = buttonClickSounds[i];

                if (clip == null || clip == lastButtonClickSound)
                    continue;

                if (currentIndex == randomIndex)
                {
                    selectedClip = clip;
                    break;
                }

                currentIndex++;
            }
        }
        else
        {
            // Если в списке фактически только один рабочий звук,
            // разрешаем использовать его снова
            for (int i = 0; i < buttonClickSounds.Length; i++)
            {
                if (buttonClickSounds[i] != null)
                {
                    selectedClip = buttonClickSounds[i];
                    break;
                }
            }
        }

        if (selectedClip == null)
            return;

        lastButtonClickSound = selectedClip;
        buttonAudioSource.PlayOneShot(selectedClip);
    }
}