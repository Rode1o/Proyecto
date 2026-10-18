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
/// Para un suelo de una sola malla con varias superficies pintadas en la textura (hierba con un
/// camino de tierra), se le puede dar una máscara: donde es blanca suena <see cref="maskedSurface"/>.
/// </summary>
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] SurfaceType surface = SurfaceType.Grass;

    [Header("Máscara (opcional)")]
    [Tooltip("Mismo mapeado UV que la textura del suelo; blanco = maskedSurface. Necesita Read/Write y un MeshCollider.")]
    [SerializeField] Texture2D mask;
    [SerializeField] SurfaceType maskedSurface = SurfaceType.Dirt;

    public SurfaceType Surface
    {
        get => surface;
        set => surface = value;
    }

    /// <summary>Superficie en el punto pisado (con máscara, según la textura en ese punto).</summary>
    public SurfaceType GetSurface(RaycastHit hit)
    {
        // textureCoord solo es válido contra un MeshCollider.
        if (mask != null && hit.collider is MeshCollider)
        {
            var uv = hit.textureCoord;
            if (mask.GetPixelBilinear(uv.x, uv.y).r > 0.5f)
                return maskedSurface;
        }
        return surface;
    }
}
