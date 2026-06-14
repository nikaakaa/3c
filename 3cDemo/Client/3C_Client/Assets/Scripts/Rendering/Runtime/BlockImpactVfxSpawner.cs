using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonRendering
{
    public sealed class BlockImpactVfxSpawner : MonoBehaviour
    {
        [SerializeField] BlockImpactVfxController prefab;
        [SerializeField] int maxActiveInstances = 8;

        readonly List<BlockImpactVfxController> instances = new List<BlockImpactVfxController>();

        public int MaxActiveInstances => Mathf.Max(1, maxActiveInstances);
        public int InstanceCount => instances.Count;

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
            for (int i = 0; i < instances.Count; i++)
            {
                if (!instances[i].IsPlaying)
                    return instances[i];
            }

            if (instances.Count >= MaxActiveInstances)
                return instances[0];

            BlockImpactVfxController instance = Instantiate(prefab, transform);
            instance.PlayOnEnable = false;
            instances.Add(instance);
            return instance;
        }
    }
}
