using UnityEngine;

namespace ThirdPersonScene
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class InteractiveTallGrassInteractor : MonoBehaviour
    {
        static readonly int InteractionPositionId = Shader.PropertyToID("_InteractionPosition");
        static readonly int InteractionRadiusId = Shader.PropertyToID("_InteractionRadius");
        static readonly int BendStrengthId = Shader.PropertyToID("_BendStrength");

        [SerializeField] Transform interactionSource;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] InteractiveTallGrassProfile profile;

        MaterialPropertyBlock propertyBlock;

        public Transform InteractionSource
        {
            get => interactionSource;
            set => interactionSource = value;
        }

        public Renderer TargetRenderer
        {
            get => targetRenderer;
            set => targetRenderer = value;
        }

        public InteractiveTallGrassProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public Vector4 LastUploadedPosition { get; private set; } = new Vector4(0f, 0f, 0f, 0f);

        public void Apply()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null)
                return;

            InteractiveTallGrassSettings settings = profile != null
                ? profile.NormalizedSettings
                : InteractiveTallGrassSettings.Default;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);

            bool hasSource = interactionSource != null;
            Vector3 sourcePosition = hasSource ? interactionSource.position : Vector3.zero;
            LastUploadedPosition = new Vector4(sourcePosition.x, sourcePosition.y, sourcePosition.z, hasSource ? 1f : 0f);

            propertyBlock.SetVector(InteractionPositionId, LastUploadedPosition);
            propertyBlock.SetFloat(InteractionRadiusId, hasSource ? settings.InteractionRadius : 0f);
            propertyBlock.SetFloat(BendStrengthId, hasSource ? settings.BendStrength : 0f);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        void LateUpdate()
        {
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }
    }
}
