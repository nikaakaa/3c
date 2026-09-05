using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterAclHash
    {
        public static string Compute(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }

        public static string ComputeStrings(IEnumerable<string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            string content = string.Join("\n", values.Select(value => value ?? string.Empty));
            return Compute(Encoding.UTF8.GetBytes(content));
        }

        public static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9' && c < 'a' || c > 'f')
                    return false;
            }
            return true;
        }
    }

    public static class CharacterAclAnimationIdentity
    {
        public static string ComputeRigReferencePoseIdentity(CharacterAnimationRigPayload rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            var boneIds = new string[rig.PoseBoneCount];
            var parentIndices = new int[rig.PoseBoneCount];
            var poses = new AnimationLocalBonePose[rig.PoseBoneCount];
            for (int i = 0; i < rig.PoseBoneCount; i++)
            {
                boneIds[i] = GetPoseBoneId(rig, i);
                parentIndices[i] = rig.GetPoseParentIndex(i);
                poses[i] = rig.GetReferenceLocalPose(i);
            }
            return ComputeRigReferencePoseIdentity(
                rig.RigId,
                rig.RigRevision,
                boneIds,
                parentIndices,
                poses);
        }

        public static string ComputeRigReferencePoseIdentity(
            string rigId,
            string rigRevision,
            IReadOnlyList<string> boneIds,
            IReadOnlyList<int> parentIndices,
            IReadOnlyList<AnimationLocalBonePose> poses)
        {
            if (string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision))
                throw new ArgumentException("Animation Rig identity is required.");
            if (boneIds == null || parentIndices == null || poses == null ||
                boneIds.Count != parentIndices.Count || boneIds.Count != poses.Count ||
                boneIds.Count == 0)
                throw new ArgumentException("Animation Rig reference pose input is invalid.");
            var values = new List<string>(checked(boneIds.Count * 9 + 4))
            {
                "acl-rig-reference/v1",
                rigId,
                rigRevision,
                boneIds.Count.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < boneIds.Count; i++)
            {
                if (string.IsNullOrEmpty(boneIds[i]) || !poses[i].IsValid)
                    throw new ArgumentException("Animation Rig reference pose input is invalid.");
                values.Add(i.ToString(CultureInfo.InvariantCulture));
                values.Add(boneIds[i]);
                values.Add(parentIndices[i].ToString(CultureInfo.InvariantCulture));
                AnimationLocalBonePose pose = poses[i];
                AddPose(values, in pose);
            }
            return CharacterAclHash.ComputeStrings(values);
        }

        public static string ComputePoseBoneReferenceIdentity(
            CharacterAnimationRigPayload rig,
            int poseBoneIndex)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if ((uint)poseBoneIndex >= (uint)rig.PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            return ComputePoseBoneReferenceIdentity(
                rig.RigId,
                rig.RigRevision,
                poseBoneIndex,
                GetPoseBoneId(rig, poseBoneIndex),
                rig.GetPoseParentIndex(poseBoneIndex),
                rig.GetReferenceLocalPose(poseBoneIndex));
        }

        public static string ComputePoseBoneReferenceIdentity(
            string rigId,
            string rigRevision,
            int poseBoneIndex,
            string boneId,
            int parentIndex,
            AnimationLocalBonePose referenceLocalPose)
        {
            if (string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision) ||
                poseBoneIndex < 0 || string.IsNullOrWhiteSpace(boneId) ||
                parentIndex < -1 || !referenceLocalPose.IsValid)
                throw new ArgumentException("Animation Pose Bone reference input is invalid.");
            var values = new List<string>(10)
            {
                "acl-pose-bone-reference/v1",
                rigId,
                rigRevision,
                poseBoneIndex.ToString(CultureInfo.InvariantCulture),
                boneId,
                parentIndex.ToString(CultureInfo.InvariantCulture)
            };
            AddPose(values, in referenceLocalPose);
            return CharacterAclHash.ComputeStrings(values);
        }

        public static string ComputeTransformBindingHash(
            CharacterAnimationRigPayload rig,
            IReadOnlyList<CharacterAclTransformTrackBinding> bindings)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            return ComputeTransformBindingHash(
                rig.RigId,
                rig.RigRevision,
                bindings);
        }

        public static string ComputeTransformBindingHash(
            string rigId,
            string rigRevision,
            IReadOnlyList<CharacterAclTransformTrackBinding> bindings)
        {
            if (string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision))
                throw new ArgumentException("Animation Rig identity is required.");
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            var ordered = bindings
                .OrderBy(value => value?.PoseBoneIndex ?? -1)
                .ToArray();
            var values = new List<string>(checked(ordered.Length * 6 + 4))
            {
                "acl-transform-binding/v1",
                rigId,
                rigRevision,
                ordered.Length.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < ordered.Length; i++)
            {
                CharacterAclTransformTrackBinding binding = ordered[i] ??
                    throw new ArgumentException("ACL transform binding is missing.", nameof(bindings));
                values.Add(binding.PoseBoneIndex.ToString(CultureInfo.InvariantCulture));
                values.Add(binding.TrackIndex.ToString(CultureInfo.InvariantCulture));
                values.Add(binding.BoneId);
                values.Add(binding.ReferenceIdentity);
            }
            return CharacterAclHash.ComputeStrings(values);
        }

        public static string GetPoseBoneId(CharacterAnimationRigPayload rig, int poseBoneIndex)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if ((uint)poseBoneIndex >= (uint)rig.PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            return poseBoneIndex < rig.PhysicalBoneCount
                ? rig.PhysicalBones[poseBoneIndex].BoneId.Value
                : rig.VirtualBones[poseBoneIndex - rig.PhysicalBoneCount].VirtualBoneId.Value;
        }

        static void AddPose(List<string> values, in AnimationLocalBonePose pose)
        {
            AddVector(values, pose.Position);
            AddQuaternion(values, pose.Rotation);
            AddVector(values, pose.Scale);
        }

        static void AddVector(List<string> values, Vector3 value)
        {
            values.Add(value.x.ToString("R", CultureInfo.InvariantCulture));
            values.Add(value.y.ToString("R", CultureInfo.InvariantCulture));
            values.Add(value.z.ToString("R", CultureInfo.InvariantCulture));
        }

        static void AddQuaternion(List<string> values, Quaternion value)
        {
            values.Add(value.x.ToString("R", CultureInfo.InvariantCulture));
            values.Add(value.y.ToString("R", CultureInfo.InvariantCulture));
            values.Add(value.z.ToString("R", CultureInfo.InvariantCulture));
            values.Add(value.w.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
