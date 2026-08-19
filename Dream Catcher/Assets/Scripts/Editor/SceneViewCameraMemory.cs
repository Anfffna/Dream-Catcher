using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneViewCameraMemory
{
    private const string KeyPrefix =
        "SceneViewCameraMemory_";

    [System.Serializable]
    private class SceneViewStateData
    {
        public Vector3 pivot;
        public Quaternion rotation;
        public float size;
        public bool orthographic;
    }


    static SceneViewCameraMemory()
    {
        EditorSceneManager.sceneClosing +=
            OnSceneClosing;

        EditorSceneManager.sceneOpened +=
            OnSceneOpened;
    }


    // =====================================================
    // СОХРАНЕНИЕ ПОЛОЖЕНИЯ
    // =====================================================

    private static void OnSceneClosing(
        Scene scene,
        bool removingScene)
    {
        // Не вмешиваемся в переходы,
        // которые происходят из-за Play Mode.
        if (EditorApplication
            .isPlayingOrWillChangePlaymode)
        {
            return;
        }

        SaveSceneView(scene);
    }


    private static void SaveSceneView(
        Scene scene)
    {
        if (!scene.IsValid())
            return;

        if (string.IsNullOrEmpty(scene.path))
            return;

        SceneView sceneView =
            SceneView.lastActiveSceneView;

        if (sceneView == null)
            return;

        SceneViewStateData data =
            new SceneViewStateData();

        data.pivot =
            sceneView.pivot;

        data.rotation =
            sceneView.rotation;

        data.size =
            sceneView.size;

        data.orthographic =
            sceneView.orthographic;

        string json =
            JsonUtility.ToJson(data);

        EditorPrefs.SetString(
            GetKey(scene.path),
            json
        );
    }


    // =====================================================
    // ВОССТАНОВЛЕНИЕ ПОЛОЖЕНИЯ
    // =====================================================

    private static void OnSceneOpened(
        Scene scene,
        OpenSceneMode mode)
    {
        if (EditorApplication
            .isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // Даём Unity один Editor update,
        // чтобы новая сцена полностью открылась.
        EditorApplication.delayCall += () =>
        {
            RestoreSceneView(scene);
        };
    }


    private static void RestoreSceneView(
        Scene scene)
    {
        if (!scene.IsValid())
            return;

        if (string.IsNullOrEmpty(scene.path))
            return;

        string key =
            GetKey(scene.path);

        if (!EditorPrefs.HasKey(key))
            return;

        string json =
            EditorPrefs.GetString(key);

        if (string.IsNullOrEmpty(json))
            return;

        SceneViewStateData data =
            JsonUtility.FromJson
                <SceneViewStateData>(json);

        if (data == null)
            return;

        SceneView sceneView =
            SceneView.lastActiveSceneView;

        if (sceneView == null)
            return;

        // Мгновенно возвращаем:
        // позицию/центр,
        // поворот,
        // масштаб,
        // perspective/orthographic.
        sceneView.LookAt(
            data.pivot,
            data.rotation,
            data.size,
            data.orthographic,
            true
        );

        sceneView.Repaint();
    }


    // =====================================================
    // КЛЮЧ ДЛЯ КАЖДОЙ СЦЕНЫ
    // =====================================================

    private static string GetKey(
        string scenePath)
    {
        // Application.dataPath добавляем,
        // чтобы разные Unity-проекты
        // не смешивали сохранённые камеры.
        string projectId =
            Application.dataPath
                .GetHashCode()
                .ToString();

        return
            KeyPrefix +
            projectId +
            "_" +
            scenePath;
    }
}