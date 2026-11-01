using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private float lifetime = 2f;

    private void Start()
    {
        if (particles != null)
            particles.Play();

        if (audioSource != null && impactClip != null)
            audioSource.PlayOneShot(impactClip);

        Destroy(gameObject, lifetime);
    }
}
