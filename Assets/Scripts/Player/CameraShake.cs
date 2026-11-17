using UnityEngine;

// Короткий поштовх камери. Працює після CameraFollow, тому зміщення не накопичується.
[DefaultExecutionOrder(200)]
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float frequency = 25f;

    private float duration;
    private float timeLeft;
    private float strength;

    public void Shake(float shakeStrength, float shakeDuration)
    {
        strength = Mathf.Max(strength * (timeLeft / Mathf.Max(duration, 0.001f)), shakeStrength);
        duration = shakeDuration;
        timeLeft = shakeDuration;
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f)
            return;

        timeLeft -= Time.deltaTime;
        float fade = Mathf.Clamp01(timeLeft / duration);
        float time = Time.time * frequency;

        Vector3 offset = new Vector3(
            Mathf.PerlinNoise(time, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, time) - 0.5f,
            0f) * 2f * strength * fade;

        transform.position += transform.rotation * offset;
    }
}
