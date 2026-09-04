using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CharacterCameraSequenceEvaluator
    {
        readonly CharacterCameraFramePlanner m_FramePlanner;
        readonly CharacterCameraSequenceTransition m_Transition;
        bool m_Initialized;

        public CharacterCameraSequenceEvaluator(CharacterCameraProjectionPayload projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireValid();
            m_FramePlanner = new CharacterCameraFramePlanner(projection);
            m_Transition = new CharacterCameraSequenceTransition(projection, m_FramePlanner);
        }

        public void Reset()
        {
            m_FramePlanner.Reset();
            m_Transition.Reset();
            m_Initialized = false;
        }

        public void Retire(string sourceId, ulong generation, float blendOutSeconds)
        {
            if (!m_Initialized)
                return;
            m_Transition.Retire(sourceId, generation, blendOutSeconds);
        }

        public CameraFramePlan Evaluate(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            in CameraResponseRequest response)
        {
            if (input.ResetHistory || !m_Initialized)
            {
                m_FramePlanner.Reset();
                m_Transition.Reset();
                m_Initialized = true;
            }
            Vector2 look = m_FramePlanner.ResolveLook(input.LookInput, in response);
            return m_Transition.Evaluate(in input, in request, look);
        }
    }
}
