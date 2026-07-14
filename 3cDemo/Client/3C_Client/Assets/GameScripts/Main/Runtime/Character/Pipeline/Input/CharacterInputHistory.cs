using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public sealed class CharacterInputHistory
    {
        readonly List<CharacterInputFrame> m_Frames = new List<CharacterInputFrame>();
        int m_Capacity = 1;

        public IReadOnlyList<CharacterInputFrame> Frames => m_Frames;
        public int Capacity => m_Capacity;

        public void SetCapacity(int capacity)
        {
            m_Capacity = capacity < 1 ? 1 : capacity;
            Trim();
        }

        public void Record(CharacterInputFrame frame)
        {
            if (frame == null)
                return;

            m_Frames.Add(frame.Clone());
            Trim();
        }

        public bool TryGetByLocalLogicTick(ulong localLogicTick, out CharacterInputFrame frame)
        {
            for (int i = m_Frames.Count - 1; i >= 0; i--)
            {
                if (m_Frames[i].LocalLogicTick == localLogicTick)
                {
                    frame = m_Frames[i];
                    return true;
                }
            }

            frame = null;
            return false;
        }

        public bool TryGetByInputSequence(ulong inputSequence, out CharacterInputFrame frame)
        {
            for (int i = m_Frames.Count - 1; i >= 0; i--)
            {
                if (m_Frames[i].InputSequence == inputSequence)
                {
                    frame = m_Frames[i];
                    return true;
                }
            }

            frame = null;
            return false;
        }

        public void Clear()
        {
            m_Frames.Clear();
        }

        void Trim()
        {
            while (m_Frames.Count > m_Capacity)
                m_Frames.RemoveAt(0);
        }
    }
}
