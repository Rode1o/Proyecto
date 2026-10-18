using UnityEngine;
using UnityEngine.AI;

// Drives only visual playback; the original NavMeshAgent remains authoritative.
public sealed class ConejoLocomotionBehaviour : StateMachineBehaviour
{
    static readonly int MoveBlend = Animator.StringToHash("MoveBlend");
    static readonly int WalkRate = Animator.StringToHash("WalkRate");
    NavMeshAgent agent;

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (agent == null) agent = animator.GetComponentInParent<NavMeshAgent>();
        float speed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            speed = velocity.magnitude;
        }

        // Hysteresis keeps tiny avoidance corrections from alternating idle/walk.
        bool wasMoving = animator.GetFloat(MoveBlend) > 0.5f;
        bool moving = speed > (wasMoving ? 0.05f : 0.12f);
        animator.SetFloat(MoveBlend, moving ? 1f : 0f);
        float normalizedSpeed = agent == null ? 0f : Mathf.Clamp01(speed / Mathf.Max(agent.speed, 0.01f));
        float rate = Mathf.Lerp(0.65f, 1.6f, normalizedSpeed);
        animator.SetFloat(WalkRate, rate, 0.12f, Time.deltaTime);
    }
}
