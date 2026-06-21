using UnityEngine;
using System.Collections.Generic;

public class PersistentObject : MonoBehaviour
{
    [Header("Unique ID")]
    public string objectID = "MyGlobalObject";

    // Статический словарь: ID ? экземпляр
    private static Dictionary<string, PersistentObject> _instances = new Dictionary<string, PersistentObject>();

    void Awake()
    {
        // 1. Если объект уже находится в сцене DontDestroyOnLoad – выходим (предотвращает повторный вызов)
        if (gameObject.scene.name == "DontDestroyOnLoad")
            return;

        // 2. Если ID не задан, используем имя объекта
        if (string.IsNullOrEmpty(objectID))
            objectID = gameObject.name;

        // 3. Проверяем дубликат по ID
        if (_instances.ContainsKey(objectID))
        {
            // Если это тот же объект (уже зарегистрирован) – ничего не делаем
            if (_instances[objectID] == this)
                return;

            // Иначе – это дубликат, уничтожаем его
            Debug.Log($"Уничтожаем дубликат {gameObject.name} (ID: {objectID})");
            Destroy(gameObject);
            return;
        }

        // 4. Регистрируем объект в словаре
        _instances[objectID] = this;

        // 5. Делаем объект глобальным
        DontDestroyOnLoad(gameObject);
        Debug.Log($"Объект {gameObject.name} (ID: {objectID}) стал глобальным");
    }

    void OnDestroy()
    {
        // При уничтожении объекта удаляем его из словаря
        if (_instances.ContainsKey(objectID) && _instances[objectID] == this)
            _instances.Remove(objectID);
    }
}