using System.IO;
using System.Text;
using UnityEngine.Networking;

namespace ThirdPerson.Development.Gm
{
    sealed class GmBoundedDownloadHandler : DownloadHandlerScript
    {
        readonly MemoryStream m_Content = new MemoryStream();
        readonly int m_MaximumBytes;

        public GmBoundedDownloadHandler(int maximumBytes) : base(new byte[4096]) => m_MaximumBytes = maximumBytes;
        public bool CapacityExceeded { get; private set; }
        public string Text => Encoding.UTF8.GetString(m_Content.GetBuffer(), 0, (int)m_Content.Length);

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (m_Content.Length + dataLength > m_MaximumBytes)
            {
                CapacityExceeded = true;
                return false;
            }
            if (dataLength > 0)
                m_Content.Write(data, 0, dataLength);
            return true;
        }

        public override void Dispose()
        {
            m_Content.Dispose();
            base.Dispose();
        }
    }
}
