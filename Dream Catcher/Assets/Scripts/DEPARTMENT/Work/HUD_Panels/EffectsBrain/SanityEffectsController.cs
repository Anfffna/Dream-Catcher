using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SanityEffectsController :
    MonoBehaviour
{
    private enum SanityEffectLevel
    {
        Calm,
        Mild,
        Strong,
        Severe,
        Breakdown
    }

    // =====================================================
    // ГРАНИЦЫ
    // =====================================================

    [Header("Границы рассудка")]

    [Tooltip("80 и ниже — слабые эффекты.")]
    [SerializeField]
    private int mildMaximumSanity = 80;

    [Tooltip("60 и ниже — заметные эффекты.")]
    [SerializeField]
    private int strongMaximumSanity = 60;

    [Tooltip("20 и ниже — тяжёлые эффекты.")]
    [SerializeField]
    private int severeMaximumSanity = 20;

    [Tooltip("1 и ниже — срыв.")]
    [SerializeField]
    private int breakdownMaximumSanity = 1;

    // =====================================================
    // ВРЕМЯ ЭФФЕКТОВ
    // =====================================================

    [Header("Длительность эффектов")]

    [Tooltip(
        "Через сколько секунд после начала приступа " +
        "начнётся его затухание."
    )]
    [SerializeField]
    private float effectDuration = 5f;

    [Tooltip(
        "Длительность плавного появления виньетки, " +
        "дрожи и других эффектов."
    )]
    [SerializeField]
    private float fadeInDuration = 2f;

    [Tooltip(
        "Длительность плавного исчезновения эффектов."
    )]
    [SerializeField]
    private float fadeOutDuration = 2f;

    [Header("Повторные приступы")]

    [Tooltip(
        "При рассудке 20–60 эффект может повториться " +
        "сам по себе через этот интервал. " +
        "300 секунд = 5 минут."
    )]
    [SerializeField]
    private float strongReminderInterval = 300f;

    // =====================================================
    // HUD
    // =====================================================

    [Header("Панель рассудка")]

    [Tooltip(
        "Можно оставить пустым. " +
        "Панель будет найдена автоматически."
    )]
    [SerializeField]
    private SanityHUDController sanityHUD;

    // =====================================================
    // VIGNETTE
    // =====================================================

    [Header("Виньетка")]

    [SerializeField]
    private Volume sanityVolume;

    [SerializeField]
    [Range(0f, 1f)]
    private float mildVignette = 0.12f;

    [SerializeField]
    [Range(0f, 1f)]
    private float strongVignette = 0.20f;

    [Tooltip("Виньетка при рассудке 2–20.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float severeVignette = 0.30f;

    [Tooltip("Виньетка при рассудке 0–1.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float breakdownVignette = 1f;

    // =====================================================
    // BLACKOUT
    // =====================================================

    [Header("Полное затемнение при срыве")]

    [Tooltip(
        "CanvasGroup чёрного Image на весь экран."
    )]
    [SerializeField]
    private CanvasGroup breakdownBlackout;

    // =====================================================
    // ПУЛЬСАЦИЯ
    // =====================================================

    [Header("Пульсация")]

    [SerializeField]
    [Range(0f, 0.5f)]
    private float severePulseAmount = 0.04f;

    [SerializeField]
    [Range(0f, 0.5f)]
    private float breakdownPulseAmount = 0.10f;

    [SerializeField]
    private float pulseSpeed = 1.4f;

    // =====================================================
    // ДРОЖЬ
    // =====================================================

    [Header("Дрожь изображения")]

    [Tooltip(
        "Можно оставить пустым. " +
        "На Main Camera компонент найдётся " +
        "или добавится автоматически."
    )]
    [SerializeField]
    private SanityCameraJitter cameraJitter;

    [SerializeField]
    private float strongJitter = 0.0013f;

    [SerializeField]
    private float severeJitter = 0.0025f;

    [SerializeField]
    private float breakdownJitter = 0.006f;

    // =====================================================
    // ОБЫЧНЫЕ ЗВУКИ
    // =====================================================

    [Header("Звуки Mild / Strong")]

    [Tooltip(
        "Один AudioSource для коротких звуков. " +
        "PlayOneShot позволяет Strong-звукам накладываться."
    )]
    [SerializeField]
    private AudioSource effectSoundSource;

    [Tooltip("Звуки для рассудка 61–80.")]
    [SerializeField]
    private AudioClip[] mildSoundClips;

    [Tooltip("Звуки для рассудка 21–60.")]
    [SerializeField]
    private AudioClip[] strongSoundClips;

    [SerializeField]
    [Range(0f, 1f)]
    private float mildSoundVolume = 0.25f;

    [SerializeField]
    [Range(0f, 1f)]
    private float strongSoundVolume = 0.45f;

    [Header("Наложение Strong-звуков")]

    [Tooltip(
        "Минимальное количество одновременно " +
        "запускаемых Strong-звуков."
    )]
    [SerializeField]
    private int strongMinLayers = 1;

    [Tooltip(
        "Максимальное количество одновременно " +
        "запускаемых Strong-звуков."
    )]
    [SerializeField]
    private int strongMaxLayers = 2;

    // =====================================================
    // HEARTBEAT
    // =====================================================

    [Header("Сердцебиение")]

    [SerializeField]
    private AudioSource heartbeatSource;

    [SerializeField]
    [Range(0f, 1f)]
    private float severeHeartbeatVolume = 0.50f;

    [SerializeField]
    [Range(0f, 1f)]
    private float breakdownHeartbeatVolume = 0.75f;

    // =====================================================
    // RUNTIME
    // =====================================================

    private SessionStatsManager statsManager;

    private Vignette vignette;

    private Coroutine effectCoroutine;
    private Coroutine findStatsCoroutine;

    private float currentBaseVignette;
    private float currentJitter;
    private float currentPulseAmount;
    private float currentBlackoutAlpha;

    private bool wasSanityPanelVisible;

    private float reminderTimer;

    // Для Mild не разрешаем один и тот же
    // звук два раза подряд.
    private int lastMildClipIndex = -1;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        FindVignette();
        FindCameraJitter();
        FindSanityHUD();

        StopAllEffectsImmediate();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded +=
            HandleSceneLoaded;

        TryConnectToStats();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -=
            HandleSceneLoaded;

        DisconnectFromStats();

        if (findStatsCoroutine != null)
        {
            StopCoroutine(
                findStatsCoroutine
            );

            findStatsCoroutine = null;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );

            effectCoroutine = null;
        }

        StopAllEffectsImmediate();
    }

    private void Update()
    {
        if (cameraJitter == null)
        {
            FindCameraJitter();
        }

        bool panelVisible =
            CanShowEffectsNow();

        // Панель только что реально появилась.
        if (panelVisible &&
            !wasSanityPanelVisible)
        {
            TriggerCurrentSanityOnPanelOpen();
        }

        wasSanityPanelVisible =
            panelVisible;

        UpdateReminderTimer(
            panelVisible
        );

        RefreshVignette();
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );

            effectCoroutine = null;
        }

        StopAllEffectsImmediate();

        reminderTimer = 0f;

        // Заставляем следующую реально
        // появившуюся панель считаться новой.
        wasSanityPanelVisible = false;

        // Если HUD был сценовым,
        // старая ссылка могла исчезнуть.
        if (sanityHUD == null)
        {
            FindSanityHUD();
        }

        if (cameraJitter == null)
        {
            FindCameraJitter();
        }
    }

    // =====================================================
    // STATS
    // =====================================================

    private void TryConnectToStats()
    {
        if (SessionStatsManager.Instance != null)
        {
            ConnectToStats(
                SessionStatsManager.Instance
            );

            return;
        }

        if (findStatsCoroutine == null)
        {
            findStatsCoroutine =
                StartCoroutine(
                    WaitForStatsManager()
                );
        }
    }

    private IEnumerator WaitForStatsManager()
    {
        while (SessionStatsManager.Instance == null)
        {
            yield return null;
        }

        findStatsCoroutine = null;

        ConnectToStats(
            SessionStatsManager.Instance
        );
    }

    private void ConnectToStats(
        SessionStatsManager manager)
    {
        DisconnectFromStats();

        statsManager =
            manager;

        if (statsManager == null)
            return;

        statsManager.SanityChanged +=
            HandleSanityChanged;

        statsManager.SanityRestored +=
            HandleSanityRestored;

        reminderTimer = 0f;
    }

    private void DisconnectFromStats()
    {
        if (statsManager == null)
            return;

        statsManager.SanityChanged -=
            HandleSanityChanged;

        statsManager.SanityRestored -=
            HandleSanityRestored;

        statsManager = null;
    }

    private void HandleSanityChanged(
        int oldValue,
        int newValue)
    {
        reminderTimer = 0f;

        // Если HUD сейчас скрыт,
        // приступ не показываем.
        //
        // Но когда HUD потом появится,
        // Update() увидит первое появление
        // и покажет эффект уже текущего значения.
        if (!CanShowEffectsNow())
            return;

        TriggerEffect(
            newValue
        );
    }

    private void HandleSanityRestored(
        int restoredValue)
    {
        // При Load не хотим показывать
        // изменение за загрузочным экраном.
        StopCurrentEffect();

        reminderTimer = 0f;

        // После загрузки / появления новой сцены
        // эффект запустится, когда HUD реально
        // снова появится.
        wasSanityPanelVisible = false;
    }

    // =====================================================
    // ПЕРВОЕ ПОЯВЛЕНИЕ HUD
    // =====================================================

    private void TriggerCurrentSanityOnPanelOpen()
    {
        if (statsManager == null)
            return;

        int sanity =
            statsManager.CurrentSanity;

        if (GetLevel(sanity) ==
            SanityEffectLevel.Calm)
        {
            return;
        }

        TriggerEffect(
            sanity
        );
    }

    // =====================================================
    // ПЕРИОДИЧЕСКОЕ НАПОМИНАНИЕ
    // =====================================================

    private void UpdateReminderTimer(
    bool panelVisible)
    {
        if (statsManager == null)
            return;

        int sanity =
            statsManager.CurrentSanity;

        // Периодические приступы только
        // при рассудке 20–60.
        if (sanity < 20 ||
            sanity > 60)
        {
            reminderTimer = 0f;
            return;
        }

        // Пока панель рассудка видна,
        // периодический приступ вообще не нужен.
        // Отсчёт начнётся заново после её исчезновения.
        if (panelVisible)
        {
            reminderTimer = 0f;
            return;
        }

        // Во время загрузки время не считаем.
        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            return;
        }

        // Обычный deltaTime:
        // при Time.timeScale = 0 таймер стоит.
        reminderTimer +=
            Time.deltaTime;

        if (reminderTimer <
            strongReminderInterval)
        {
            return;
        }

        reminderTimer = 0f;

        // true = разрешаем этот конкретный
        // приступ именно БЕЗ панели HUD.
        TriggerEffect(
            sanity,
            true
        );
    }

    // =====================================================
    // EFFECT
    // =====================================================

    private void TriggerEffect(
        int sanity,
        bool intervalEffectWithoutPanel = false)
    {
        SanityEffectLevel level =
            GetLevel(sanity);

        if (level ==
            SanityEffectLevel.Calm)
        {
            StartFadeOut();
            return;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );

            effectCoroutine = null;
        }

        PrepareAudioForLevel(
            level
        );

        reminderTimer = 0f;

        effectCoroutine =
            StartCoroutine(
                EffectRoutine(
                    level,
                    intervalEffectWithoutPanel
                )
            );
    }

    private IEnumerator EffectRoutine(
        SanityEffectLevel level,
        bool intervalEffectWithoutPanel)
    {
        GetTargets(
            level,
            out float targetVignette,
            out float targetJitter,
            out float targetPulse,
            out float targetBlackout,
            out float targetHeartbeatVolume
        );

        float startVignette =
            currentBaseVignette;

        float startJitter =
            currentJitter;

        float startPulse =
            currentPulseAmount;

        float startBlackout =
            currentBlackoutAlpha;

        float startHeartbeat =
            heartbeatSource != null
                ? heartbeatSource.volume
                : 0f;

        float elapsed = 0f;

        // =================================================
        // 2 СЕКУНДЫ — ПЛАВНОЕ ПОЯВЛЕНИЕ
        // =================================================

        while (elapsed <
               fadeInDuration)
        {
            if (!CanEffectRun(
                intervalEffectWithoutPanel
                ))
            {
                break;
            }

            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                fadeInDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        fadeInDuration
                    );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            currentBaseVignette =
                Mathf.Lerp(
                    startVignette,
                    targetVignette,
                    smoothT
                );

            currentJitter =
                Mathf.Lerp(
                    startJitter,
                    targetJitter,
                    smoothT
                );

            currentPulseAmount =
                Mathf.Lerp(
                    startPulse,
                    targetPulse,
                    smoothT
                );

            currentBlackoutAlpha =
                Mathf.Lerp(
                    startBlackout,
                    targetBlackout,
                    smoothT
                );

            if (heartbeatSource != null)
            {
                heartbeatSource.volume =
                    Mathf.Lerp(
                        startHeartbeat,
                        targetHeartbeatVolume,
                        smoothT
                    );
            }

            ApplyCurrentState();

            yield return null;
        }

        if (!CanEffectRun(
            intervalEffectWithoutPanel
            ))
        {
            yield return
                FadeOutRoutine();

            effectCoroutine = null;
            yield break;
        }

        currentBaseVignette =
            targetVignette;

        currentJitter =
            targetJitter;

        currentPulseAmount =
            targetPulse;

        currentBlackoutAlpha =
            targetBlackout;

        if (heartbeatSource != null)
        {
            heartbeatSource.volume =
                targetHeartbeatVolume;
        }

        ApplyCurrentState();

        // Effect Duration = 5:
        // исчезновение начинается через 5 секунд
        // после самого начала приступа.
        float holdDuration =
            Mathf.Max(
                0f,
                effectDuration -
                fadeInDuration
            );

        elapsed = 0f;

        while (elapsed <
               holdDuration)
        {
            if (!CanEffectRun(
                    intervalEffectWithoutPanel
                ))
            {
                break;
            }

            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        // =================================================
        // 2 СЕКУНДЫ — ПЛАВНОЕ ИСЧЕЗНОВЕНИЕ
        // =================================================

        yield return
            FadeOutRoutine();

        effectCoroutine = null;
    }

    // =====================================================
    // FADE OUT
    // =====================================================

    private void StartFadeOut()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );
        }

        effectCoroutine =
            StartCoroutine(
                FadeOutAndClearRoutine()
            );
    }

    private IEnumerator
        FadeOutAndClearRoutine()
    {
        yield return
            FadeOutRoutine();

        effectCoroutine = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float startVignette =
            currentBaseVignette;

        float startJitter =
            currentJitter;

        float startPulse =
            currentPulseAmount;

        float startBlackout =
            currentBlackoutAlpha;

        float startEffectVolume =
            effectSoundSource != null
                ? effectSoundSource.volume
                : 0f;

        float startHeartbeat =
            heartbeatSource != null
                ? heartbeatSource.volume
                : 0f;

        float elapsed = 0f;

        while (elapsed <
               fadeOutDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                fadeOutDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        fadeOutDuration
                    );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            currentBaseVignette =
                Mathf.Lerp(
                    startVignette,
                    0f,
                    smoothT
                );

            currentJitter =
                Mathf.Lerp(
                    startJitter,
                    0f,
                    smoothT
                );

            currentPulseAmount =
                Mathf.Lerp(
                    startPulse,
                    0f,
                    smoothT
                );

            currentBlackoutAlpha =
                Mathf.Lerp(
                    startBlackout,
                    0f,
                    smoothT
                );

            if (effectSoundSource != null)
            {
                effectSoundSource.volume =
                    Mathf.Lerp(
                        startEffectVolume,
                        0f,
                        smoothT
                    );
            }

            if (heartbeatSource != null)
            {
                heartbeatSource.volume =
                    Mathf.Lerp(
                        startHeartbeat,
                        0f,
                        smoothT
                    );
            }

            ApplyCurrentState();

            yield return null;
        }

        StopAllEffectsImmediate();
    }

    private void StopCurrentEffect()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );

            effectCoroutine = null;
        }

        StopAllEffectsImmediate();
    }

    // =====================================================
    // LEVEL TARGETS
    // =====================================================

    private void GetTargets(
        SanityEffectLevel level,
        out float vignetteTarget,
        out float jitterTarget,
        out float pulseTarget,
        out float blackoutTarget,
        out float heartbeatTarget)
    {
        vignetteTarget = 0f;
        jitterTarget = 0f;
        pulseTarget = 0f;
        blackoutTarget = 0f;
        heartbeatTarget = 0f;

        switch (level)
        {
            case SanityEffectLevel.Mild:

                vignetteTarget =
                    mildVignette;

                break;

            case SanityEffectLevel.Strong:

                vignetteTarget =
                    strongVignette;

                jitterTarget =
                    strongJitter;

                break;

            case SanityEffectLevel.Severe:

                vignetteTarget =
                    severeVignette;

                jitterTarget =
                    severeJitter;

                pulseTarget =
                    severePulseAmount;

                heartbeatTarget =
                    severeHeartbeatVolume;

                break;

            case SanityEffectLevel.Breakdown:

                vignetteTarget =
                    breakdownVignette;

                jitterTarget =
                    breakdownJitter;

                pulseTarget =
                    breakdownPulseAmount;

                blackoutTarget =
                    breakdownBlackout != null
                        ? 1f
                        : 0f;

                heartbeatTarget =
                    breakdownHeartbeatVolume;

                break;
        }
    }

    private SanityEffectLevel GetLevel(
        int sanity)
    {
        if (sanity <=
            breakdownMaximumSanity)
        {
            return SanityEffectLevel
                .Breakdown;
        }

        if (sanity <=
            severeMaximumSanity)
        {
            return SanityEffectLevel
                .Severe;
        }

        if (sanity <=
            strongMaximumSanity)
        {
            return SanityEffectLevel
                .Strong;
        }

        if (sanity <=
            mildMaximumSanity)
        {
            return SanityEffectLevel
                .Mild;
        }

        return SanityEffectLevel.Calm;
    }

    // =====================================================
    // AUDIO
    // =====================================================

    private void PrepareAudioForLevel(
        SanityEffectLevel level)
    {
        if (effectSoundSource != null)
        {
            // Stop останавливает все старые
            // OneShot этого AudioSource.
            effectSoundSource.Stop();

            effectSoundSource.volume =
                1f;

            effectSoundSource.loop =
                false;
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
            heartbeatSource.volume = 0f;
        }

        if (level ==
            SanityEffectLevel.Mild)
        {
            PlayMildSound();

            return;
        }

        if (level ==
            SanityEffectLevel.Strong)
        {
            PlayStrongLayeredSounds();

            return;
        }

        if (level ==
                SanityEffectLevel.Severe ||
            level ==
                SanityEffectLevel.Breakdown)
        {
            if (heartbeatSource == null ||
                heartbeatSource.clip == null)
            {
                return;
            }

            heartbeatSource.loop =
                true;

            heartbeatSource.volume =
                0f;

            heartbeatSource.Play();
        }
    }

    // =====================================================
    // MILD — БЕЗ ПОВТОРА
    // =====================================================

    private void PlayMildSound()
    {
        if (effectSoundSource == null ||
            mildSoundClips == null ||
            mildSoundClips.Length == 0)
        {
            return;
        }

        List<int> validIndices =
            new List<int>();

        for (int i = 0;
             i < mildSoundClips.Length;
             i++)
        {
            if (mildSoundClips[i] != null)
            {
                validIndices.Add(i);
            }
        }

        if (validIndices.Count == 0)
            return;

        // Если есть выбор хотя бы из двух,
        // запрещаем прошлый индекс.
        if (validIndices.Count > 1)
        {
            validIndices.Remove(
                lastMildClipIndex
            );
        }

        int selectedIndex =
            validIndices[
                Random.Range(
                    0,
                    validIndices.Count
                )
            ];

        lastMildClipIndex =
            selectedIndex;

        effectSoundSource.PlayOneShot(
            mildSoundClips[selectedIndex],
            mildSoundVolume
        );
    }

    // =====================================================
    // STRONG — НАЛОЖЕНИЕ
    // =====================================================

    private void PlayStrongLayeredSounds()
    {
        if (effectSoundSource == null ||
            strongSoundClips == null ||
            strongSoundClips.Length == 0)
        {
            return;
        }

        List<int> available =
            new List<int>();

        for (int i = 0;
             i < strongSoundClips.Length;
             i++)
        {
            if (strongSoundClips[i] != null)
            {
                available.Add(i);
            }
        }

        if (available.Count == 0)
            return;

        int minimum =
            Mathf.Clamp(
                strongMinLayers,
                1,
                available.Count
            );

        int maximum =
            Mathf.Clamp(
                strongMaxLayers,
                minimum,
                available.Count
            );

        int layerCount =
            Random.Range(
                minimum,
                maximum + 1
            );

        for (int layer = 0;
             layer < layerCount;
             layer++)
        {
            int randomListIndex =
                Random.Range(
                    0,
                    available.Count
                );

            int clipIndex =
                available[randomListIndex];

            effectSoundSource.PlayOneShot(
                strongSoundClips[clipIndex],
                strongSoundVolume
            );

            // Один и тот же файл не накладываем
            // сам на себя внутри одного приступа.
            available.RemoveAt(
                randomListIndex
            );
        }
    }

    // =====================================================
    // HUD VISIBILITY
    // =====================================================

    private bool CanShowEffectsNow()
    {
        // Не запускаем приступ под загрузочным экраном.
        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            return false;
        }

        return IsSanityPanelVisible();
    }

    private bool CanEffectRun(
    bool intervalEffectWithoutPanel)
    {
        // Никакие приступы не проигрываем
        // под загрузочным экраном.
        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            return false;
        }

        bool panelVisible =
            IsSanityPanelVisible();

        if (intervalEffectWithoutPanel)
        {
            // Интервальный приступ существует
            // ТОЛЬКО когда панели нет.
            return !panelVisible;
        }

        // Обычный приступ от изменения значения
        // или появления HUD — наоборот,
        // только при видимой панели.
        return panelVisible;
    }

    private bool IsSanityPanelVisible()
    {
        if (sanityHUD == null)
        {
            FindSanityHUD();
        }

        if (sanityHUD == null)
            return false;

        if (!sanityHUD.gameObject
            .activeInHierarchy)
        {
            return false;
        }

        CanvasGroup[] groups =
            sanityHUD
                .GetComponentsInParent
                    <CanvasGroup>(true);

        for (int i = 0;
             i < groups.Length;
             i++)
        {
            CanvasGroup group =
                groups[i];

            if (group == null)
                continue;

            if (group.alpha <= 0.05f)
            {
                return false;
            }
        }

        return true;
    }

    private void FindSanityHUD()
    {
        if (sanityHUD != null)
            return;

        sanityHUD =
            FindFirstObjectByType
                <SanityHUDController>(
                    FindObjectsInactive.Include
                );
    }

    // =====================================================
    // CAMERA
    // =====================================================

    private void FindCameraJitter()
    {
        if (cameraJitter != null)
            return;

        Camera mainCamera =
            Camera.main;

        if (mainCamera == null)
        {
            mainCamera =
                FindFirstObjectByType<Camera>(
                    FindObjectsInactive.Include
                );
        }

        if (mainCamera == null)
            return;

        cameraJitter =
            mainCamera.GetComponent
                <SanityCameraJitter>();

        if (cameraJitter == null)
        {
            cameraJitter =
                mainCamera.gameObject
                    .AddComponent
                        <SanityCameraJitter>();
        }
    }

    // =====================================================
    // VIGNETTE
    // =====================================================

    private void FindVignette()
    {
        if (sanityVolume == null)
        {
            sanityVolume =
                GetComponent<Volume>();
        }

        if (sanityVolume == null ||
            sanityVolume.profile == null)
        {
            return;
        }

        sanityVolume.profile.TryGet(
            out vignette
        );

        if (vignette != null)
        {
            vignette.intensity.overrideState =
                true;
        }
    }

    private void RefreshVignette()
    {
        if (vignette == null)
        {
            FindVignette();
        }

        if (vignette == null)
            return;

        float pulse = 0f;

        if (currentPulseAmount > 0f)
        {
            float wave =
                (
                    Mathf.Sin(
                        Time.unscaledTime *
                        pulseSpeed *
                        Mathf.PI *
                        2f
                    ) + 1f
                ) *
                0.5f;

            pulse =
                wave *
                currentPulseAmount;
        }

        vignette.intensity.value =
            Mathf.Clamp01(
                currentBaseVignette +
                pulse
            );
    }

    // =====================================================
    // APPLY / RESET
    // =====================================================

    private void ApplyCurrentState()
    {
        if (cameraJitter == null)
        {
            FindCameraJitter();
        }

        if (cameraJitter != null)
        {
            cameraJitter.SetJitterAmount(
                currentJitter
            );
        }

        if (breakdownBlackout != null)
        {
            breakdownBlackout.alpha =
                currentBlackoutAlpha;

            breakdownBlackout.interactable =
                false;

            breakdownBlackout.blocksRaycasts =
                false;
        }

        RefreshVignette();
    }

    private void StopAllEffectsImmediate()
    {
        currentBaseVignette = 0f;
        currentJitter = 0f;
        currentPulseAmount = 0f;
        currentBlackoutAlpha = 0f;

        if (cameraJitter != null)
        {
            cameraJitter.SetJitterAmount(
                0f
            );
        }

        if (effectSoundSource != null)
        {
            effectSoundSource.Stop();

            // Следующий PlayOneShot снова
            // должен звучать нормально.
            effectSoundSource.volume =
                1f;
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
            heartbeatSource.volume = 0f;
        }

        if (breakdownBlackout != null)
        {
            breakdownBlackout.alpha = 0f;
            breakdownBlackout.interactable = false;
            breakdownBlackout.blocksRaycasts = false;
        }

        if (vignette == null)
        {
            FindVignette();
        }

        if (vignette != null)
        {
            vignette.intensity.value =
                0f;
        }
    }

    // =====================================================
    // TEST
    // =====================================================

    [ContextMenu(
        "TEST: повторить эффект текущего рассудка"
    )]
    private void TestCurrentSanityEffect()
    {
        if (statsManager == null)
            return;

        TriggerEffect(
            statsManager.CurrentSanity
        );
    }

    // =====================================================
    // VALIDATE
    // =====================================================

    private void OnValidate()
    {
        mildMaximumSanity =
            Mathf.Clamp(
                mildMaximumSanity,
                0,
                100
            );

        strongMaximumSanity =
            Mathf.Clamp(
                strongMaximumSanity,
                0,
                mildMaximumSanity
            );

        severeMaximumSanity =
            Mathf.Clamp(
                severeMaximumSanity,
                0,
                strongMaximumSanity
            );

        breakdownMaximumSanity =
            Mathf.Clamp(
                breakdownMaximumSanity,
                0,
                severeMaximumSanity
            );

        effectDuration =
            Mathf.Max(
                0f,
                effectDuration
            );

        fadeInDuration =
            Mathf.Max(
                0f,
                fadeInDuration
            );

        fadeOutDuration =
            Mathf.Max(
                0f,
                fadeOutDuration
            );

        strongReminderInterval =
            Mathf.Max(
                1f,
                strongReminderInterval
            );

        strongMinLayers =
            Mathf.Max(
                1,
                strongMinLayers
            );

        strongMaxLayers =
            Mathf.Max(
                strongMinLayers,
                strongMaxLayers
            );

        strongJitter =
            Mathf.Max(
                0f,
                strongJitter
            );

        severeJitter =
            Mathf.Max(
                0f,
                severeJitter
            );

        breakdownJitter =
            Mathf.Max(
                0f,
                breakdownJitter
            );

        pulseSpeed =
            Mathf.Max(
                0f,
                pulseSpeed
            );
    }
}