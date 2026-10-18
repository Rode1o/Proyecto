using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AmbienteManager : MonoBehaviour
{
    [SerializeField] private EventReference eventoAmbiente;

    private EventInstance ambiente;

    void Start()
    {
        ambiente = RuntimeManager.CreateInstance(eventoAmbiente);
        ambiente.start();
    }

    void OnDestroy()
    {
        ambiente.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        ambiente.release();
    }
}
