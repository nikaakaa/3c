using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public sealed class CharacterDeviceInputFocus
    {
        bool m_Captured;
        int m_ReleaseFrame = -1;

        public bool IsGameplaySuppressed => m_Captured || Time.frameCount == m_ReleaseFrame;

        public IDisposable Acquire()
        {
            if (m_Captured)
                throw new InvalidOperationException("角色设备输入焦点已经被占用。");
            m_Captured = true;
            return new Capture(this);
        }

        sealed class Capture : IDisposable
        {
            CharacterDeviceInputFocus m_Owner;
            public Capture(CharacterDeviceInputFocus owner) => m_Owner = owner;

            public void Dispose()
            {
                if (m_Owner == null)
                    return;
                m_Owner.m_Captured = false;
                m_Owner.m_ReleaseFrame = Time.frameCount;
                m_Owner = null;
            }
        }
    }
}
