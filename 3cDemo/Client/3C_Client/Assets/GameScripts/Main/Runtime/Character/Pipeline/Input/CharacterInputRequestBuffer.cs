using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public sealed class CharacterInputRequestBuffer
    {
        readonly List<CharacterInputRequest> m_Requests = new List<CharacterInputRequest>();

        public IReadOnlyList<CharacterInputRequest> Requests => m_Requests;

        public void Add(CharacterInputRequest request)
        {
            m_Requests.Add(request);
        }

        public bool HasRequest(string requestId, ulong localLogicTick)
        {
            return TryGetRequest(requestId, localLogicTick, out _);
        }

        public bool TryGetRequest(string requestId, ulong localLogicTick, out CharacterInputRequest request)
        {
            int index = FindBestIndex(requestId, localLogicTick);
            if (index < 0)
            {
                request = default;
                return false;
            }

            request = m_Requests[index];
            return true;
        }

        public bool TryConsumeRequest(string requestId, ulong localLogicTick, out CharacterInputRequest request)
        {
            int index = FindBestIndex(requestId, localLogicTick);
            if (index < 0)
            {
                request = default;
                return false;
            }

            request = m_Requests[index];
            request.MarkConsumed();
            m_Requests[index] = request;
            return true;
        }

        public void CleanupExpired(ulong localLogicTick)
        {
            for (int i = m_Requests.Count - 1; i >= 0; i--)
            {
                if (m_Requests[i].IsExpired(localLogicTick))
                    m_Requests.RemoveAt(i);
            }
        }

        public void Clear()
        {
            m_Requests.Clear();
        }

        int FindBestIndex(string requestId, ulong localLogicTick)
        {
            int bestIndex = -1;
            for (int i = 0; i < m_Requests.Count; i++)
            {
                CharacterInputRequest candidate = m_Requests[i];
                if (string.IsNullOrEmpty(candidate.RequestId) ||
                    candidate.RequestId != requestId ||
                    !candidate.IsAvailable(localLogicTick))
                    continue;

                if (bestIndex < 0)
                {
                    bestIndex = i;
                    continue;
                }

                CharacterInputRequest best = m_Requests[bestIndex];
                if (candidate.Priority > best.Priority ||
                    candidate.Priority == best.Priority && candidate.CreatedLocalLogicTick < best.CreatedLocalLogicTick)
                    bestIndex = i;
            }

            return bestIndex;
        }
    }
}
