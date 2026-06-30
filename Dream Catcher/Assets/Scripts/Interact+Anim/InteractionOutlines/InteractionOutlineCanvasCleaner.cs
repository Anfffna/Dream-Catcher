using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InteractionOutlineCanvasCleaner : MonoBehaviour
{
    public string lineObjectName = "InteractionOutline_UI_Line";

    private void Awake()
    {
        ClearLines();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ClearAndRedrawNextFrame());
    }

    private IEnumerator ClearAndRedrawNextFrame()
    {
        ClearLines();

        yield return null;

        ClearLines();

        yield return null;

        InteractionOutlineRegistry.RedrawVisibleOutlines();
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