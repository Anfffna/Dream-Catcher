using UnityEngine;
using System.Collections.Generic;

public class PersistentObject : MonoBehaviour
{
    [Header("Unique ID")]
    public string objectID = "MyGlobalObject";

    [Header("Только для GlobalSystem")]
    [SerializeField] private GameObject eventSystemObject;

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
            PersistentObject existingObject =
                _instances[objectID];

            // Это уже зарегистрированный объект.
            if (existingObject == this)
                return;

            MovePersistentPlayerToScenePlayer(
                existingObject
            );

            Debug.Log(
                $"Уничтожаем дубликат {gameObject.name} " +
                $"(ID: {objectID})"
            );

            Destroy(gameObject);
            return;
        }

        // 4. Регистрируем объект в словаре
        _instances[objectID] = this;

        // 5. Делаем объект глобальным
        DontDestroyOnLoad(gameObject);

        if (eventSystemObject != null)
            eventSystemObject.SetActive(true);
    }

    private void MovePersistentPlayerToScenePlayer(
    PersistentObject existingObject)
    {
        if (existingObject == null)
            return;

        // При загрузке сохранения ничего не трогаем.
        // SaveManager сам поставит Player
        // в сохранённые координаты.
        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            return;
        }

        // Проверяем, что оба PersistentObject
        // действительно являются Player.
        PlayerController scenePlayer =
            GetComponent<PlayerController>();

        PlayerController persistentPlayer =
            existingObject
                .GetComponent<PlayerController>();

        if (scenePlayer == null ||
            persistentPlayer == null)
        {
            return;
        }

        // Обнуляем старую скорость/гравитацию,
        // которую Player мог принести
        // из предыдущей сцены.
        bool oldCanMove =
            persistentPlayer.canMove;

        persistentPlayer
            .SetMovementEnabled(false);

        CharacterController controller =
            persistentPlayer
                .GetComponent<CharacterController>();

        bool controllerWasEnabled =
            controller != null &&
            controller.enabled;

        if (controllerWasEnabled)
        {
            controller.enabled = false;
        }

        // Позиция и поворот берутся прямо
        // от Player prefab новой сцены.
        persistentPlayer.transform
            .SetPositionAndRotation(
                transform.position,
                transform.rotation
            );

        Physics.SyncTransforms();

        if (controllerWasEnabled)
        {
            controller.enabled = true;
        }

        persistentPlayer
            .SetMovementEnabled(
                oldCanMove
            );
    }

    void OnDestroy()
    {
        // При уничтожении объекта удаляем его из словаря
        if (_instances.ContainsKey(objectID) && _instances[objectID] == this)
            _instances.Remove(objectID);
    }
}