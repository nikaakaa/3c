using UnityEngine;

namespace BTSMTL.Timeline
{
    [CreateAssetMenu(fileName = "Timeline", menuName = "BTSMTL/Timeline/Shared Timeline")]
    public sealed class TimelineAsset : ScriptableObject
    {
        [SerializeField]
        TimelineData m_Data = new TimelineData();

        public TimelineData Data => m_Data;

        public void SetData(TimelineData data)
        {
            m_Data = data ?? TimelineData.CreateDefault(name);
            BindData();
        }

        void OnEnable()
        {
            BindData();
        }

        void OnValidate()
        {
            BindData();
        }

        void BindData()
        {
            if (m_Data == null)
                m_Data = TimelineData.CreateDefault(name);
            m_Data.BindSerializedOwner(this, "m_Data");
            m_Data.Init();
        }
    }
}
