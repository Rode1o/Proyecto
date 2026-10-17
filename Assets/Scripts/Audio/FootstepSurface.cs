using UnityEngine;

/// <summary>
/// Tipos de suelo. El valor numérico es el que recibe el parámetro de superficie del evento de FMOD.
/// </summary>
public enum SurfaceType
{
    Grass = 0,
    Dirt = 1,
    Stone = 2
}

/// <summary>
/// Marca un collider de suelo con su tipo de superficie para que PlayerFootsteps elija el sonido.
/// </summary>
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] SurfaceType surface = SurfaceType.Grass;

    public SurfaceType Surface
    {
        get => surface;
        set => surface = value;
    }
}
