using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TankHealthBar : MonoBehaviour
{
    [SerializeField] private TankHealth health;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text healthText;

    [Header("Extra")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Color fullColor = new Color(0.25f, 0.85f, 0.3f);
    [SerializeField] private Color halfColor = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.15f);
    [SerializeField, Min(0f)] private float smoothDuration = 0.25f;
    [SerializeField] private bool hideWhenFull;
    [SerializeField] private GameObject barRoot;

    private Coroutine smoothRoutine;

    private void OnEnable()
    {
        if (health == null)
            return;

        health.HealthChanged += UpdateView;
        SetSliderInstant(health.CurrentHealth, health.MaxHealth);
        UpdateView(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        if (health != null)
            health.HealthChanged -= UpdateView;
    }

    private void UpdateView(int currentHealth, int maxHealth)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxHealth;

            if (smoothRoutine != null)
                StopCoroutine(smoothRoutine);

            if (smoothDuration > 0f && isActiveAndEnabled)
                smoothRoutine = StartCoroutine(SmoothSlider(currentHealth));
            else
                slider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text =
                currentHealth + " / " + maxHealth;
        }

        UpdateFillColor(currentHealth, maxHealth);

        if (hideWhenFull && barRoot != null)
            barRoot.SetActive(currentHealth < maxHealth);
    }

    private void SetSliderInstant(int currentHealth, int maxHealth)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = maxHealth;
        slider.value = currentHealth;
    }

    private IEnumerator SmoothSlider(float targetValue)
    {
        float startValue = slider.value;
        float elapsed = 0f;

        while (elapsed < smoothDuration)
        {
            elapsed += Time.deltaTime;
            slider.value = Mathf.Lerp(startValue, targetValue, elapsed / smoothDuration);
            yield return null;
        }

        slider.value = targetValue;
        smoothRoutine = null;
    }

    private void UpdateFillColor(int currentHealth, int maxHealth)
    {
        if (fillImage == null)
            return;

        float health01 = (float)currentHealth / maxHealth;

        fillImage.color = health01 > 0.5f
            ? Color.Lerp(halfColor, fullColor, (health01 - 0.5f) * 2f)
            : Color.Lerp(lowColor, halfColor, health01 * 2f);
    }
}
