using UnityEngine;

public class WorkerActivationDebug : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log(
            "WORKER БЫЛ ВКЛЮЧЕН!\n" +
            System.Environment.StackTrace,
            this
        );
    }

    private void OnDisable()
    {
        Debug.Log(
            "WORKER БЫЛ ВЫКЛЮЧЕН!",
            this
        );
    }
}