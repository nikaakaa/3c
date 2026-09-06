using System.Collections.Generic;
using UnityEngine;

namespace ZZZ.Rendering.Restored
{
    [ExecuteAlways]
    public sealed class CorinRestoredRenderEntity : MonoBehaviour
    {
        static readonly List<CorinRestoredRenderEntity> Active = new();
        static readonly int PackedParams1 = Shader.PropertyToID("_PackedParams1");
        static readonly int MiddlePointPosition = Shader.PropertyToID("_MiddlePointPosition");
        static readonly int AmbientGradientShape = Shader.PropertyToID("_AmbientGradientShape");
        static readonly int EntityInfo = Shader.PropertyToID("_EntityInfo");
        static readonly int HeadMatrix0 = Shader.PropertyToID("_HeadMatrixWS2OS0");
        static readonly int HeadMatrix1 = Shader.PropertyToID("_HeadMatrixWS2OS1");
        static readonly int HeadMatrix2 = Shader.PropertyToID("_HeadMatrixWS2OS2");

        [SerializeField] Transform face;
        [SerializeField] SkinnedMeshRenderer[] renderers;

        MaterialPropertyBlock properties;

        public Vector3 FacePosition => face.position;
        public Vector3 FaceForward => face.localToWorldMatrix.MultiplyVector(Vector3.up).normalized;
        public Matrix4x4 WorldToObject => transform.worldToLocalMatrix;

        public Vector3 Position
        {
            get
            {
                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                    bounds.Encapsulate(renderers[index].bounds);
                return bounds.center;
            }
        }

        public void Initialize(Transform faceTransform, SkinnedMeshRenderer[] entityRenderers)
        {
            face = faceTransform;
            renderers = entityRenderers;
        }

        public static void CollectActive(List<CorinRestoredRenderEntity> target)
        {
            target.Clear();
            foreach (var entity in Active)
                if (entity != null && entity.isActiveAndEnabled && entity.gameObject.scene.IsValid())
                    target.Add(entity);
        }

        public void ApplyEntityIndex(int index, Vector4 mainLightPosition)
        {
            properties ??= new MaterialPropertyBlock();
            var middlePoint = Position;
            var lightDirection = new Vector3(mainLightPosition.x, mainLightPosition.y, mainLightPosition.z).normalized;
            var gradientAxis = lightDirection * -0.5f;
            var gradientOffset = Vector3.Dot(gradientAxis, middlePoint) - 0.5f;
            var headMatrix = face.worldToLocalMatrix;
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetVector(PackedParams1, new Vector4(0f, 0f, index, 0f));
                properties.SetVector(MiddlePointPosition,
                    new Vector4(middlePoint.x, middlePoint.y, middlePoint.z, 0f));
                properties.SetVector(AmbientGradientShape,
                    new Vector4(gradientAxis.x, gradientAxis.y, gradientAxis.z, gradientOffset));
                properties.SetVector(EntityInfo, new Vector4(0f, 1f, 0f, 0f));
                properties.SetVector(HeadMatrix0, headMatrix.GetRow(0));
                properties.SetVector(HeadMatrix1, headMatrix.GetRow(1));
                properties.SetVector(HeadMatrix2, headMatrix.GetRow(2));
                renderer.SetPropertyBlock(properties);
            }
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }
    }
}
