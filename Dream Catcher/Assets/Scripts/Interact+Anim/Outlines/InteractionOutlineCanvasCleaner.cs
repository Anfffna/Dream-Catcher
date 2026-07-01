using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InteractionOutlineCanvasCleaner : MonoBehaviour
{
    public string lineObjectName = "InteractionOutline_UI_Line";

    private Coroutine clearCoroutine;

    private void Awake()
    {
        ClearLines();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (gameObject.activeInHierarchy)
        {
            if (clearCoroutine != null)
                StopCoroutine(clearCoroutine);

            clearCoroutine = StartCoroutine(ClearAndRedrawNextFrame());
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (clearCoroutine != null)
        {
            StopCoroutine(clearCoroutine);
            clearCoroutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Если GlobalCanvas / InteractionOutlineCanvas выключен в MainMenu,
        // корутину запускать нельзя и не нужно.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ClearLines();
            return;
        }

        if (clearCoroutine != null)
            StopCoroutine(clearCoroutine);

        clearCoroutine = StartCoroutine(ClearAndRedrawNextFrame());
    }

    private IEnumerator ClearAndRedrawNextFrame()
    {
        ClearLines();

        yield return null;

        ClearLines();

        yield return null;

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
            InteractionOutlineRegistry.RedrawVisibleOutlines();

        clearCoroutine = null;
    }

    public void ClearLines()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child == null)
                continue;

            if (child.name.StartsWith(lineObjectName))
            {
                Destroy(child.gameObject);
            }
        }
    }
}