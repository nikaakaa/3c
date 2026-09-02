using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramFramePages : IDisposable
    {
        sealed class Page
        {
            internal NativeArray<CharacterPoseStateMachineNativeControl>
                StateMachineControls;
            internal NativeArray<CharacterAnimationSlotNativeControl>
                AnimationSlotControls;
            internal NativeArray<CharacterRootOrientationWarpNativeControl>
                RootOrientationWarpControls;
            internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
                LinkedPoseCallControls;
            internal NativeArray<byte> LinkedPoseActiveFragments;
        }

        Page m_Committed;
        Page m_Pending;
        Page m_Active;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseProgramFramePages(
            int stateMachineCount,
            int animationSlotCount,
            int rootOrientationWarpCount,
            int linkedPoseCallCount,
            int linkedPoseFragmentCount)
        {
            if (stateMachineCount < 0 ||
                animationSlotCount < 0 ||
                rootOrientationWarpCount < 0 ||
                linkedPoseCallCount < 0 ||
                linkedPoseFragmentCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            try
            {
                m_Committed = AllocatePage(
                    stateMachineCount,
                    animationSlotCount,
                    rootOrientationWarpCount,
                    linkedPoseCallCount,
                    linkedPoseFragmentCount);
                m_Pending = AllocatePage(
                    stateMachineCount,
                    animationSlotCount,
                    rootOrientationWarpCount,
                    linkedPoseCallCount,
                    linkedPoseFragmentCount);
                m_Active = m_Committed;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal NativeArray<CharacterPoseStateMachineNativeControl>
            StateMachineControls => RequireActive().StateMachineControls;
        internal NativeArray<CharacterAnimationSlotNativeControl>
            AnimationSlotControls => RequireActive().AnimationSlotControls;
        internal NativeArray<CharacterRootOrientationWarpNativeControl>
            RootOrientationWarpControls =>
                RequireActive().RootOrientationWarpControls;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
            LinkedPoseCallControls => RequireActive().LinkedPoseCallControls;
        internal NativeArray<byte> LinkedPoseActiveFragments =>
            RequireActive().LinkedPoseActiveFragments;
        internal bool HasOpenFrame => m_FrameOpen;

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are already open.");
            }
            m_Active = m_Pending;
            ClearLinkedPose(m_Active);
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            Page previousCommitted = m_Committed;
            m_Committed = m_Pending;
            m_Pending = previousCommitted;
            m_Active = m_Committed;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            m_Active = m_Committed;
            m_FrameOpen = false;
        }

        internal void RequireValid()
        {
            RequireAlive();
            Page committed = m_Committed ??
                throw new InvalidOperationException(
                    "Character Pose Program committed frame page is missing.");
            RequirePage(
                committed,
                committed.StateMachineControls.Length,
                committed.AnimationSlotControls.Length,
                committed.RootOrientationWarpControls.Length,
                committed.LinkedPoseCallControls.Length,
                committed.LinkedPoseActiveFragments.Length);
            RequirePage(
                m_Pending,
                committed.StateMachineControls.Length,
                committed.AnimationSlotControls.Length,
                committed.RootOrientationWarpControls.Length,
                committed.LinkedPoseCallControls.Length,
                committed.LinkedPoseActiveFragments.Length);
            if (!ReferenceEquals(m_Active, m_Committed) &&
                !ReferenceEquals(m_Active, m_Pending))
            {
                throw new InvalidOperationException(
                    "Character Pose Program active frame page is detached.");
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            DisposePage(m_Pending);
            DisposePage(m_Committed);
            m_Active = null;
            m_Pending = null;
            m_Committed = null;
            m_FrameOpen = false;
        }

        Page RequireActive()
        {
            RequireAlive();
            return m_Active ??
                throw new InvalidOperationException(
                    "Character Pose Program active frame page is missing.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseProgramFramePages));
            }
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are not open.");
            }
        }

        static Page AllocatePage(
            int stateMachineCount,
            int animationSlotCount,
            int rootOrientationWarpCount,
            int linkedPoseCallCount,
            int linkedPoseFragmentCount)
        {
            var page = new Page();
            try
            {
                page.StateMachineControls = Allocate<
                    CharacterPoseStateMachineNativeControl>(
                    stateMachineCount);
                page.AnimationSlotControls = Allocate<
                    CharacterAnimationSlotNativeControl>(
                    animationSlotCount);
                page.RootOrientationWarpControls = AllocateClear<
                    CharacterRootOrientationWarpNativeControl>(
                    rootOrientationWarpCount);
                page.LinkedPoseCallControls = Allocate<
                    AnimationPoseGraphNativeLinkedPoseCallControl>(
                    linkedPoseCallCount);
                page.LinkedPoseActiveFragments = AllocateClear<byte>(
                    linkedPoseFragmentCount);
                ClearLinkedPose(page);
                return page;
            }
            catch
            {
                DisposePage(page);
                throw;
            }
        }

        static void ClearLinkedPose(Page page)
        {
            for (int i = 0; i < page.LinkedPoseCallControls.Length; i++)
            {
                page.LinkedPoseCallControls[i] =
                    AnimationPoseGraphNativeLinkedPoseCallControl.Inactive;
            }
            for (int i = 0; i < page.LinkedPoseActiveFragments.Length; i++)
                page.LinkedPoseActiveFragments[i] = 0;
        }

        static void RequirePage(
            Page page,
            int stateMachineCount,
            int animationSlotCount,
            int rootOrientationWarpCount,
            int linkedPoseCallCount,
            int linkedPoseFragmentCount)
        {
            if (page == null ||
                !page.StateMachineControls.IsCreated ||
                page.StateMachineControls.Length != stateMachineCount ||
                !page.AnimationSlotControls.IsCreated ||
                page.AnimationSlotControls.Length != animationSlotCount ||
                !page.RootOrientationWarpControls.IsCreated ||
                page.RootOrientationWarpControls.Length !=
                    rootOrientationWarpCount ||
                !page.LinkedPoseCallControls.IsCreated ||
                page.LinkedPoseCallControls.Length != linkedPoseCallCount ||
                !page.LinkedPoseActiveFragments.IsCreated ||
                page.LinkedPoseActiveFragments.Length !=
                    linkedPoseFragmentCount)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame page layout is invalid.");
            }
        }

        static NativeArray<T> Allocate<T>(int length)
            where T : struct =>
            new NativeArray<T>(
                length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

        static NativeArray<T> AllocateClear<T>(int length)
            where T : struct =>
            new NativeArray<T>(
                length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);

        static void DisposePage(Page page)
        {
            if (page == null)
                return;
            DisposeArray(ref page.LinkedPoseActiveFragments);
            DisposeArray(ref page.LinkedPoseCallControls);
            DisposeArray(ref page.RootOrientationWarpControls);
            DisposeArray(ref page.AnimationSlotControls);
            DisposeArray(ref page.StateMachineControls);
        }

        static void DisposeArray<T>(ref NativeArray<T> values)
            where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
