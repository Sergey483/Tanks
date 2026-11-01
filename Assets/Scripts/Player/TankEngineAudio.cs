using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankEngineAudio : MonoBehaviour
{
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private float maxTrackedSpeed = 6f;
    [SerializeField] private float idleVolume = 0.15f;
    [SerializeField] private float movingVolume = 0.6f;
    [SerializeField] private float idlePitch = 0.8f;
    [SerializeField] private float movingPitch = 1.25f;
    [SerializeField] private float changeSpeed = 4f;

    private Rigidbody tankRigidbody;

    private void Awake()
    {
        tankRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (engineSource == null)
            return;

        float speed01 = Mathf.InverseLerp(
            0f,
            maxTrackedSpeed,
            tankRigidbody.linearVelocity.magnitude
        );

        float targetVolume = Mathf.Lerp(
            idleVolume,
            movingVolume,
            speed01
        );

        float targetPitch = Mathf.Lerp(
            idlePitch,
            movingPitch,
            speed01
        );

        engineSource.volume = Mathf.MoveTowards(
            engineSource.volume,
            targetVolume,
            changeSpeed * Time.deltaTime
        );

        engineSource.pitch = Mathf.MoveTowards(
            engineSource.pitch,
            targetPitch,
            changeSpeed * Time.deltaTime
        );
    }
}
