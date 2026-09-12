using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhoneCameraFollow))]
public sealed class PhoneCameraFollowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PhoneCameraFollow follow = (PhoneCameraFollow)target;
        EditorGUILayout.Space();

        if (follow.Reference == null)
        {
            EditorGUILayout.HelpBox("Перед Play Mode создай файл калибровки кнопкой ниже.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("1. Создать файл калибровки")) CreateReference(follow);
            }
            return;
        }

        EditorGUILayout.HelpBox(
            follow.Reference.Calibrated
                ? "Эталонный взгляд сохранён. Для нового рабочего места можно записать его повторно."
                : "Запусти Play, сядь за стол и посмотри в исходном направлении, для которого делались анимации. Телефон должен лежать на столе.",
            follow.Reference.Calibrated ? MessageType.Info : MessageType.Warning);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("2. Сохранить текущий взгляд — навсегда"))
            {
                Undo.RecordObject(follow.Reference, "Calibrate phone camera");
                if (follow.CaptureReference(out string error))
                {
                    EditorUtility.SetDirty(follow.Reference);
                    AssetDatabase.SaveAssetIfDirty(follow.Reference);
                    Debug.Log("[Phone] Взгляд сохранён в asset. Он не пропадёт после Stop. Теперь можно проверить обе ветки телефона.", follow);
                }
                else Debug.LogError("[Phone] " + error, follow);
            }
        }
    }

    private void CreateReference(PhoneCameraFollow follow)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Сохранить калибровку телефона", "PhoneCameraReference", "asset",
            "Один файл калибровки на одно рабочее место.");
        if (string.IsNullOrEmpty(path)) return;
        PhoneCameraReference asset = CreateInstance<PhoneCameraReference>();
        AssetDatabase.CreateAsset(asset, path);
        serializedObject.Update();
        serializedObject.FindProperty("reference").objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(follow);
        AssetDatabase.SaveAssetIfDirty(asset);
    }
}
