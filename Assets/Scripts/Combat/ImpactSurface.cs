using UnityEngine;

// Позначає матеріал поверхні, щоб снаряд створював відповідний ефект влучання.
public class ImpactSurface : MonoBehaviour
{
    public enum SurfaceType
    {
        Ground,
        Metal
    }

    [SerializeField] private SurfaceType surfaceType = SurfaceType.Metal;

    public SurfaceType Type => surfaceType;
}
