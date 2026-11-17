using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    public Transform TurretPivot => turretPivot;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField, Range(0.1f, 1f)] private float reverseSpeedMultiplier = 0.6f;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float deceleration = 5f;
    [SerializeField] private float turnAcceleration = 6f;

    [Header("Turret")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] private float turretKeyboardRotationSpeed = 75f;
    [SerializeField, Min(0f)] private float turretMouseSensitivity = 0.15f;

    private Rigidbody tankRigidbody;
    private float targetMoveInput;
    private float targetTurnInput;
    private float currentMoveInput;
    private float currentTurnInput;
    private Vector3 previousMousePosition;
    private bool isMoving;

    private void Awake()
    {
        tankRigidbody = GetComponent<Rigidbody>();
        tankRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        previousMousePosition = Input.mousePosition;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            previousMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        ReadMovementInput();
        RotateTurret();
    }

    private void FixedUpdate()
    {
        SmoothInput();
        MoveAndRotate();
        ReportMovementChange();
    }

    private void ReportMovementChange()
    {
        bool movingNow = Mathf.Abs(currentMoveInput) > 0.01f || Mathf.Abs(currentTurnInput) > 0.01f;
        if (movingNow == isMoving)
            return;

        isMoving = movingNow;
        Debug.Log(isMoving ? "Танк почав рух" : "Танк зупинився");
    }

    private void SmoothInput()
    {
        float moveChangeSpeed =
            targetMoveInput == 0f ? deceleration : acceleration;

        currentMoveInput = Mathf.MoveTowards(
            currentMoveInput,
            targetMoveInput,
            moveChangeSpeed * Time.fixedDeltaTime
        );

        currentTurnInput = Mathf.MoveTowards(
            currentTurnInput,
            targetTurnInput,
            turnAcceleration * Time.fixedDeltaTime
        );
    }

    private void MoveAndRotate()
    {
        float turn =
            currentTurnInput *
            rotationSpeed *
            Time.fixedDeltaTime;

        Quaternion rotationDelta =
            Quaternion.Euler(0f, turn, 0f);

        Quaternion nextRotation =
            tankRigidbody.rotation * rotationDelta;

        Vector3 moveDirection =
            nextRotation * Vector3.forward;

        // Назад танк їде повільніше, ніж уперед.
        float speed = currentMoveInput < 0f
            ? moveSpeed * reverseSpeedMultiplier
            : moveSpeed;

        Vector3 nextPosition =
            tankRigidbody.position +
            moveDirection *
            currentMoveInput *
            speed *
            Time.fixedDeltaTime;

        tankRigidbody.MoveRotation(nextRotation);
        tankRigidbody.MovePosition(nextPosition);
    }

    private void ReadMovementInput()
    {
        // Стрілки або W/S/A/D.
        targetMoveInput = Mathf.Clamp(
            GetButtonAxis(KeyCode.DownArrow, KeyCode.UpArrow) + GetButtonAxis(KeyCode.S, KeyCode.W), -1f, 1f);
        targetTurnInput = Mathf.Clamp(
            GetButtonAxis(KeyCode.LeftArrow, KeyCode.RightArrow) + GetButtonAxis(KeyCode.A, KeyCode.D), -1f, 1f);
    }

    private static float GetButtonAxis(KeyCode negativeKey, KeyCode positiveKey)
    {
        float negative = Input.GetKey(negativeKey) ? 1f : 0f;
        float positive = Input.GetKey(positiveKey) ? 1f : 0f;
        return positive - negative;
    }

    private void RotateTurret()
    {
        if (turretPivot == null)
            return;

        float keyboardInput = GetButtonAxis(KeyCode.Z, KeyCode.X);

        Vector3 mousePosition = Input.mousePosition;
        float mouseDelta = mousePosition.x - previousMousePosition.x;
        previousMousePosition = mousePosition;

        float rotationAmount =
            keyboardInput * turretKeyboardRotationSpeed * Time.deltaTime +
            mouseDelta * turretMouseSensitivity;

        turretPivot.Rotate(0f, rotationAmount, 0f, Space.Self);
    }
}
