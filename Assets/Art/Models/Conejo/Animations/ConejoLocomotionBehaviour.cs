using UnityEngine;
using UnityEngine.AI;

// Drives only visual playback; the original NavMeshAgent remains authoritative.
public sealed class ConejoLocomotionBehaviour : StateMachineBehaviour
{
    static readonly int MoveBlend = Animator.StringToHash("MoveBlend");
    static readonly int WalkRate = Animator.StringToHash("WalkRate");
    NavMeshAgent agent;

    [SerializeField, HideInInspector] Quaternion[] referenceArmRotations;
    [SerializeField, HideInInspector] int referenceArmFrameCount;
    static readonly HumanBodyBones[] ReferenceArmBones = {
        HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
        HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
    };

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

    // Reproduce only the arm rotations authored in the walking FBX.
    public override void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!animator.isHuman || referenceArmFrameCount < 2 || referenceArmRotations == null ||
            referenceArmRotations.Length != referenceArmFrameCount * ReferenceArmBones.Length)
            return;

        float weight = 1f;
        if (animator.IsInTransition(layerIndex))
        {
            float transition = Mathf.Clamp01(animator.GetAnimatorTransitionInfo(layerIndex).normalizedTime);
            weight = animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash
                ? 1f - transition : transition;
        }

        float sample = Mathf.Repeat(stateInfo.normalizedTime, 1f) * (referenceArmFrameCount - 1);
        int first = Mathf.FloorToInt(sample);
        int second = Mathf.Min(first + 1, referenceArmFrameCount - 1);
        float fraction = sample - first;
        for (int i = 0; i < ReferenceArmBones.Length; i++)
        {
            Quaternion rotation = Quaternion.Slerp(
                referenceArmRotations[first * ReferenceArmBones.Length + i],
                referenceArmRotations[second * ReferenceArmBones.Length + i], fraction);
            Transform bone = animator.GetBoneTransform(ReferenceArmBones[i]);
            if (bone == null) continue;
            if (weight < 0.9999f) rotation = Quaternion.Slerp(bone.localRotation, rotation, weight);
            animator.SetBoneLocalRotation(ReferenceArmBones[i], rotation);
        }
    }
}
