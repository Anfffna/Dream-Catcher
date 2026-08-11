using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class SanityCameraJitter : MonoBehaviour
{
    [Header("Дрожь изображения")]

    [Tooltip("Скорость изменения дрожи.")]
    [SerializeField]
    private float jitterSpeed = 18f;

    private Camera targetCamera;

    private float jitterAmount;

    public float JitterAmount =>
        jitterAmount;

    private void Awake()
    {
        targetCamera =
            GetComponent<Camera>();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering +=
            HandleBeginCameraRendering;

        RenderPipelineManager.endCameraRendering +=
            HandleEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -=
            HandleBeginCameraRendering;

        RenderPipelineManager.endCameraRendering -=
            HandleEndCameraRendering;

        if (targetCamera != null)
        {
            targetCamera
                .ResetProjectionMatrix();
        }
    }

    public void SetJitterAmount(
        float amount)
    {
        jitterAmount =
            Mathf.Max(
                0f,
                amount
            );
    }

    private void HandleBeginCameraRendering(
        ScriptableRenderContext context,
        Camera camera)
    {
        if (camera != targetCamera)
            return;

        targetCamera.ResetProjectionMatrix();

        if (jitterAmount <= 0f)
            return;

        float time =
            Time.unscaledTime *
            jitterSpeed;

        float offsetX =
            (
                Mathf.PerlinNoise(
                    time,
                    17.31f
                ) - 0.5f
            ) *
            2f *
            jitterAmount;

        float offsetY =
            (
                Mathf.PerlinNoise(
                    43.17f,
                    time
                ) - 0.5f
            ) *
            2f *
            jitterAmount;

        Matrix4x4 projection =
            targetCamera.projectionMatrix;

        projection.m02 +=
            offsetX;

        projection.m12 +=
            offsetY;

        targetCamera.projectionMatrix =
            projection;
    }

    private void HandleEndCameraRendering(
        ScriptableRenderContext context,
        Camera camera)
    {
        if (camera != targetCamera)
            return;

        targetCamera
            .ResetProjectionMatrix();
    }

    private void OnValidate()
    {
        jitterSpeed =
            Mathf.Max(
                0f,
                jitterSpeed
            );
    }
}