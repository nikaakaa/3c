using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CinemachineResolvedTargetAdapter
    {
        readonly Transform owner;
        readonly CinemachineFreeLook freeLook;
        Transform followTarget;
        Transform aimTarget;

        public CinemachineResolvedTargetAdapter(
            Transform owner,
            CinemachineFreeLook freeLook,
            Transform followTarget,
            Transform aimTarget)
        {
            this.owner = owner;
            this.freeLook = freeLook;
            this.followTarget = followTarget;
            this.aimTarget = aimTarget;
        }

        public Transform FollowTarget => followTarget;
        public Transform AimTarget => aimTarget;

        public bool IsOutputTarget(Transform target)
        {
            return target != null && (target == followTarget || target == aimTarget);
        }

        public void EnsureTargets()
        {
            if (followTarget == null)
                followTarget = FindChildTarget("CameraFollowTarget") ?? CreateTarget("CameraFollowTarget");

            if (aimTarget == null)
                aimTarget = FindChildTarget("CameraAimTarget") ?? CreateTarget("CameraAimTarget");
        }

        public void BindFreeLook()
        {
            if (freeLook == null)
                return;

            if (followTarget != null)
                freeLook.Follow = followTarget;

            if (aimTarget != null)
                freeLook.LookAt = aimTarget;
        }

        public void Apply(CameraResolveResult result)
        {
            if (followTarget != null)
                followTarget.position = result.AnchorPosition;

            if (aimTarget != null)
                aimTarget.position = result.AimPoint;
        }

        Transform FindChildTarget(string targetName)
        {
            if (owner == null)
                return null;

            Transform[] children = owner.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == targetName)
                    return child;
            }

            return null;
        }

        Transform CreateTarget(string targetName)
        {
            GameObject targetObject = new GameObject(targetName);
            Transform target = targetObject.transform;
            target.SetParent(owner, false);
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
            return target;
        }
    }
}
