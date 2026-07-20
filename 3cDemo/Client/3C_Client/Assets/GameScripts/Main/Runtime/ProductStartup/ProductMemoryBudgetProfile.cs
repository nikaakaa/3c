using UnityEngine;

namespace ThirdPerson.ProductStartup
{
    [CreateAssetMenu(fileName = "ProductMemoryBudgetProfile", menuName = "Third Person/Product Startup/Memory Budget")]
    public sealed class ProductMemoryBudgetProfile : ScriptableObject
    {
        [SerializeField, Min(0)] long m_HomeBytes;
        [SerializeField, Min(0)] long m_GameplayBytes;

        public long HomeBytes => m_HomeBytes;
        public long GameplayBytes => m_GameplayBytes;
    }
}
