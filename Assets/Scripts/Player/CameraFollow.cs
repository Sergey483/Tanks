using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform rotationTarget;
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float lookAtHeight = 0.8f;

    private float smoothedYaw;
    private float yawVelocity;
    private bool isInitialized;

    private void Start()
    {
        FindRotationTarget();
        InitializeCamera();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (rotationTarget == null)
            FindRotationTarget();

        if (!isInitialized)
            InitializeCamera();

        float targetYaw = rotationTarget != null
            ? rotationTarget.eulerAngles.y
            : target.eulerAngles.y;

        smoothedYaw = Mathf.SmoothDampAngle(
            smoothedYaw,
            targetYaw,
            ref yawVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        UpdateCameraTransform(target.position);
    }

    private void FindRotationTarget()
    {
        if (rotationTarget != null || target == null)
            return;

        TankController tankController = target.GetComponent<TankController>();

        rotationTarget = tankController != null
            ? tankController.TurretPivot
            : target;
    }

    private void InitializeCamera()
    {
        if (target == null)
            return;

        smoothedYaw = rotationTarget != null
            ? rotationTarget.eulerAngles.y
            : target.eulerAngles.y;

        yawVelocity = 0f;
        isInitialized = true;

        UpdateCameraTransform(target.position);
    }

    private void UpdateCameraTransform(Vector3 targetPosition)
    {
        Quaternion orbitRotation = Quaternion.Euler(0f, smoothedYaw, 0f);

        Vector3 cameraPosition =
            targetPosition + orbitRotation * offset;

        Vector3 lookPosition =
            targetPosition + Vector3.up * lookAtHeight;

        transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(lookPosition - cameraPosition, Vector3.up)
        );
    }
}
