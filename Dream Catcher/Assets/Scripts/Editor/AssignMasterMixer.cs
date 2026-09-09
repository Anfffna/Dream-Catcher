using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

public static class AssignMasterMixer
{
    private const string MixerAssetName = "MainAudioMixer";
    private const string MasterGroupName = "Master";

    [MenuItem("Tools/Audio/Assign Master Mixer To ALL AudioSources")]
    public static void AssignMasterToEverything()
    {
        // На всякий случай предлагает сохранить текущую сцену,
        // если в ней есть несохранённые изменения.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;


        // ============================
        // 1. ИЩЕМ MAIN AUDIO MIXER
        // ============================

        string[] mixerGuids =
            AssetDatabase.FindAssets("t:AudioMixer");

        string mixerPath = mixerGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path)
                == MixerAssetName);

        if (string.IsNullOrEmpty(mixerPath))
        {
            Debug.LogError(
                $"Не найден Audio Mixer с именем " +
                $"'{MixerAssetName}'."
            );

            return;
        }


        AudioMixer mixer =
            AssetDatabase.LoadAssetAtPath<AudioMixer>(
                mixerPath
            );

        AudioMixerGroup masterGroup =
            mixer.FindMatchingGroups(MasterGroupName)
                .FirstOrDefault(group =>
                    group.name == MasterGroupName);

        if (masterGroup == null)
        {
            Debug.LogError(
                $"В миксере '{MixerAssetName}' " +
                $"не найдена группа '{MasterGroupName}'."
            );

            return;
        }


        int changedSources = 0;
        int changedPrefabs = 0;
        int changedScenes = 0;


        // ============================
        // 2. ОБРАБАТЫВАЕМ ВСЕ PREFAB
        // ============================

        string[] prefabGuids =
            AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            // Не трогаем Packages.
            if (!path.StartsWith("Assets/"))
                continue;

            GameObject prefabRoot = null;

            try
            {
                prefabRoot =
                    PrefabUtility.LoadPrefabContents(path);

                AudioSource[] sources =
                    prefabRoot.GetComponentsInChildren<AudioSource>(
                        true
                    );

                bool prefabChanged = false;

                foreach (AudioSource source in sources)
                {
                    if (source.outputAudioMixerGroup ==
                        masterGroup)
                    {
                        continue;
                    }

                    source.outputAudioMixerGroup =
                        masterGroup;

                    EditorUtility.SetDirty(source);

                    changedSources++;
                    prefabChanged = true;
                }

                if (prefabChanged)
                {
                    PrefabUtility.SaveAsPrefabAsset(
                        prefabRoot,
                        path
                    );

                    changedPrefabs++;
                }
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(
                        prefabRoot
                    );
                }
            }
        }


        // ============================
        // 3. ЗАПОМИНАЕМ ТЕКУЩУЮ СЦЕНУ
        // ============================

        string originalScenePath =
            SceneManager.GetActiveScene().path;


        // ============================
        // 4. ОБРАБАТЫВАЕМ ВСЕ СЦЕНЫ
        // ============================

        string[] sceneGuids =
            AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in sceneGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            if (!path.StartsWith("Assets/"))
                continue;

            Scene scene =
                EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Single
                );

            bool sceneChanged = false;

            foreach (GameObject root
                     in scene.GetRootGameObjects())
            {
                AudioSource[] sources =
                    root.GetComponentsInChildren<AudioSource>(
                        true
                    );

                foreach (AudioSource source in sources)
                {
                    if (source.outputAudioMixerGroup ==
                        masterGroup)
                    {
                        continue;
                    }

                    source.outputAudioMixerGroup =
                        masterGroup;

                    EditorUtility.SetDirty(source);

                    changedSources++;
                    sceneChanged = true;
                }
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                changedScenes++;
            }
        }


        // ============================
        // 5. ВОЗВРАЩАЕМ ТВОЮ СЦЕНУ
        // ============================

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(
                originalScenePath,
                OpenSceneMode.Single
            );
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        // ============================
        // ГОТОВО
        // ============================

        Debug.Log(
            "ГОТОВО!\n" +
            $"Изменено AudioSource: {changedSources}\n" +
            $"Изменено Prefab: {changedPrefabs}\n" +
            $"Изменено сцен: {changedScenes}\n\n" +
            $"Все AudioSource теперь используют " +
            $"{MixerAssetName} → {MasterGroupName}."
        );
    }
}