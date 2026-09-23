using System;
using UnityEngine;

namespace ThirdPersonRendering
{
    public sealed class BlockImpactVfxSpawner : MonoBehaviour
    {
        [SerializeField] BlockImpactVfxController prefab;
        [SerializeField] int maxActiveInstances = 8;

        BlockImpactVfxController[] instances = Array.Empty<BlockImpactVfxController>();

        public int MaxActiveInstances => Mathf.Max(1, maxActiveInstances);
        public int InstanceCount => instances.Length;

        void Awake()
        {
            if (prefab == null)
                throw new InvalidOperationException("BlockImpactVfxSpawner requires BlockImpactVfx prefab.");

            maxActiveInstances = Mathf.Max(1, maxActiveInstances);
            instances = new BlockImpactVfxController[maxActiveInstances];
            for (int i = 0; i < instances.Length; i++)
            {
                BlockImpactVfxController instance = Instantiate(prefab, transform);
                instance.PlayOnEnable = false;
                instance.gameObject.SetActive(false);
                instances[i] = instance;
            }
        }

        public BlockImpactVfxController Spawn(BlockImpactVfxRequest request)
        {
            if (prefab == null)
            {
                Debug.LogError("BlockImpactVfxSpawner 缺少 BlockImpactVfx prefab", this);
                return null;
            }

            BlockImpactVfxController instance = GetInstance();
            instance.transform.position = request.WorldHitPoint;
            instance.gameObject.SetActive(true);
            instance.Play(request);
            return instance;
        }

        BlockImpactVfxController GetInstance()
        {
            for (int i = 0; i < instances.Length; i++)
            {
                if (!instances[i].IsPlaying)
                    return instances[i];
            }
            return instances[0];
        }
    }
}
