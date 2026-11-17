using System;
using UnityEngine;
using UnityEngine.Rendering;

// Перемикає атмосферу арени: день і захід сонця.
public class AtmosphereSwitcher : MonoBehaviour
{
    [Serializable]
    private class Atmosphere
    {
        public string name = "Day";
        public Vector3 sunRotation = new Vector3(50f, -30f, 0f);
        public Color sunColor = Color.white;
        public float sunIntensity = 1.2f;
        public Color ambientColor = new Color(0.55f, 0.6f, 0.7f);
        public Color fogColor = new Color(0.65f, 0.72f, 0.8f);
        public float fogDensity = 0.006f;
        public VolumeProfile volumeProfile;
    }

    [SerializeField] private Light sun;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private Atmosphere[] atmospheres;
    [SerializeField] private int startIndex;
    [SerializeField] private KeyCode switchKey = KeyCode.T;

    private int currentIndex;

    private void Start()
    {
        Apply(startIndex);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
            Apply(currentIndex + 1);
    }

    public void Apply(int index)
    {
        if (atmospheres == null || atmospheres.Length == 0)
            return;

        currentIndex = (index % atmospheres.Length + atmospheres.Length) % atmospheres.Length;
        Atmosphere atmosphere = atmospheres[currentIndex];

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(atmosphere.sunRotation);
            sun.color = atmosphere.sunColor;
            sun.intensity = atmosphere.sunIntensity;
        }

        RenderSettings.ambientLight = atmosphere.ambientColor;
        RenderSettings.fogColor = atmosphere.fogColor;
        RenderSettings.fogDensity = atmosphere.fogDensity;

        if (globalVolume != null && atmosphere.volumeProfile != null)
            globalVolume.sharedProfile = atmosphere.volumeProfile;
    }
}
