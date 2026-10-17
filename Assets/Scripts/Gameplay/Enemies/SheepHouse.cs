using UnityEngine;

/// <summary>
/// Casa de la oveja en una isla y encuentro de esa isla. La oveja se muda a la casa de la isla
/// donde está el jugador: entra por la puerta hasta el interior (oculta por las paredes),
/// se teletransporta al interior de la otra casa y sale por su puerta.
/// </summary>
public class SheepHouse : MonoBehaviour
{
    [Tooltip("Collider del suelo de la isla a la que pertenece esta casa.")]
    [SerializeField] Collider island;
    [Tooltip("Punto sobre el NavMesh delante de la puerta.")]
    [SerializeField] Transform door;
    [Tooltip("Punto dentro de la casa, tapado por las paredes.")]
    [SerializeField] Transform interior;
    [Tooltip("Adonde camina el jugador si acepta la propuesta.")]
    [SerializeField] Transform playerSpot;

    [Header("Encuentro de esta isla")]
    [SerializeField] string decisionId = "oveja_isla";
    [SerializeField] DialogContent offer = new()
    {
        speaker = "Oveja",
        text = "¡Hola! Te veo muy cansado. Ven a mi casa y yo me encargo de todo tu trabajo. ¿Vienes?",
        options = new[] { "Sí, ayúdame", "No, gracias" }
    };
    [SerializeField, TextArea] string rejectReply = "¡Bah! Tú te lo pierdes...";

    public Collider Island => island;
    public Transform Door => door;
    public Transform Interior => interior;
    public Transform PlayerSpot => playerSpot != null ? playerSpot : door;
    public string DecisionId => decisionId;
    public DialogContent Offer => offer;
    public string RejectReply => rejectReply;
}
