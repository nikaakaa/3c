using UnityEngine;

namespace ThirdPersonRendering
{
    public sealed class BlockImpactVfxPreview : MonoBehaviour
    {
        [SerializeField] BlockImpactVfxController vfx;
        [SerializeField] Vector3 localHitOffset = new Vector3(0f, 1.35f, 0f);
        [SerializeField] Vector3 attackDirection = Vector3.forward;
        [SerializeField] float intensity = 1f;
        [SerializeField] float duration = 0.28f;
        [SerializeField] bool autoRepeat;
        [SerializeField] float repeatInterval = 1.2f;

        float repeatTimer;

        void Update()
        {
            if (!autoRepeat)
                return;

            repeatTimer += Time.deltaTime;
            if (repeatTimer < Mathf.Max(0.1f, repeatInterval))
                return;

            repeatTimer = 0f;
            PlayPreview();
        }

        [ContextMenu("Play Block Impact Preview")]
        public void PlayPreview()
        {
            if (vfx == null)
            {
                Debug.LogError("BlockImpactVfxPreview 缺少 BlockImpactVfxController", this);
                return;
            }

            Vector3 worldHitPoint = transform.TransformPoint(localHitOffset);
            Camera camera = Camera.main;
            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            if (camera != null)
            {
                Vector3 viewportPoint = camera.WorldToViewportPoint(worldHitPoint);
                screenCenter = new Vector2(viewportPoint.x, viewportPoint.y);
            }

            BlockImpactVfxRequest request = new BlockImpactVfxRequest(
                worldHitPoint,
                transform.TransformDirection(attackDirection),
                screenCenter,
                intensity,
                duration,
                0,
                true,
                true,
                true,
                true,
                true);

            vfx.Play(request);
        }
    }
}
