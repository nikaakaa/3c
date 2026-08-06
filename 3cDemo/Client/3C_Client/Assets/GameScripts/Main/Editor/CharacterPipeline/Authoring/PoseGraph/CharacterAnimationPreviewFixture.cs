using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CreateAssetMenu(
        fileName = "CharacterAnimationPreviewFixture",
        menuName = "3C/Character/Animation Preview Fixture")]
    public sealed class CharacterAnimationPreviewFixture : ScriptableObject
    {
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] GameObject m_PreviewRigPrefab;
        [SerializeField] GameObject m_EnvironmentPrefab;

        public CharacterPipelineDefinition Definition => m_Definition;
        public CharacterAnimationPresentationProfile Profile => m_Profile;
        public GameObject PreviewRigPrefab => m_PreviewRigPrefab;
        public GameObject EnvironmentPrefab => m_EnvironmentPrefab;

        public void RequireValid()
        {
            if (!m_Definition || !m_Profile || !m_PreviewRigPrefab)
                throw new InvalidOperationException("Animation Preview Fixture requires Definition, Presentation Profile and Preview Rig Prefab.");
            if (m_Definition.AnimationPresentationProfile != m_Profile)
                throw new InvalidOperationException("Animation Preview Fixture Definition and Profile do not match.");
            CharacterPipelineHost host = m_PreviewRigPrefab.GetComponentInChildren<CharacterPipelineHost>(true);
            if (!host || host.Definition != m_Definition || host.Definition.AnimationPresentationProfile != m_Profile)
                throw new InvalidOperationException("Animation Preview Fixture Rig Prefab does not contain the exact Definition and Presentation Profile.");
        }
    }

    internal static class CharacterAnimationPreviewFixtureCatalog
    {
        internal static IReadOnlyList<CharacterAnimationPreviewFixture> Load()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationPreviewFixture");
            var fixtures = new List<CharacterAnimationPreviewFixture>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterAnimationPreviewFixture fixture = AssetDatabase.LoadAssetAtPath<CharacterAnimationPreviewFixture>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (fixture)
                    fixtures.Add(fixture);
            }
            fixtures.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return fixtures;
        }
    }

    internal sealed class CharacterAnimationPreviewFixtureSession : IDisposable
    {
        readonly Scene m_Scene;
        readonly GameObject m_RigInstance;
        readonly GameObject m_EnvironmentInstance;
        readonly GameObject m_KeyLightInstance;
        readonly Camera m_PreviewCamera;
        RenderTexture m_PreviewTexture;
        readonly Vector3 m_DefaultFocus;
        readonly float m_DefaultDistance;
        Vector3 m_Focus;
        float m_Distance;
        float m_Yaw;
        float m_Pitch;
        bool m_Disposed;

        CharacterAnimationPreviewFixtureSession(
            Scene scene,
            GameObject rigInstance,
            GameObject environmentInstance,
            GameObject keyLightInstance,
            Camera previewCamera,
            RenderTexture previewTexture,
            Vector3 focus,
            float distance)
        {
            m_Scene = scene;
            m_RigInstance = rigInstance;
            m_EnvironmentInstance = environmentInstance;
            m_KeyLightInstance = keyLightInstance;
            m_PreviewCamera = previewCamera;
            m_PreviewTexture = previewTexture;
            m_DefaultFocus = focus;
            m_DefaultDistance = distance;
            m_Focus = focus;
            m_Distance = distance;
            Vector3 euler = previewCamera.transform.eulerAngles;
            m_Yaw = euler.y;
            m_Pitch = NormalizeAngle(euler.x);
        }

        internal CharacterPipelineHost Target =>
            m_RigInstance ? m_RigInstance.GetComponentInChildren<CharacterPipelineHost>(true) : null;
        internal Scene PreviewScene => m_Scene;
        internal bool HasEnvironment => m_EnvironmentInstance;
        internal RenderTexture PreviewTexture => m_PreviewTexture;

        internal static CharacterAnimationPreviewFixtureSession Create(
            CharacterAnimationPreviewFixture fixture)
        {
            if (!fixture)
                throw new ArgumentNullException(nameof(fixture));
            fixture.RequireValid();
            Scene scene = SceneManager.CreateScene(
                $"CharacterAnimationPreview/{fixture.name}/{Guid.NewGuid():N}",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            GameObject rigInstance = null;
            GameObject environmentInstance = null;
            GameObject keyLightInstance = null;
            GameObject previewCameraObject = null;
            RenderTexture previewTexture = null;
            try
            {
                rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                    fixture.PreviewRigPrefab,
                    scene);
                if (fixture.EnvironmentPrefab)
                    environmentInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                        fixture.EnvironmentPrefab,
                        scene);
                ulong sceneCullingMask =
                    EditorSceneManager.CalculateAvailableSceneCullingMask();
                if (sceneCullingMask == 0UL)
                    throw new InvalidOperationException(
                        "Animation Preview cannot allocate an isolated Scene culling mask.");
                EditorSceneManager.SetSceneCullingMask(
                    scene,
                    sceneCullingMask);
                CharacterPipelineHost target = rigInstance
                    ? rigInstance.GetComponentInChildren<CharacterPipelineHost>(true)
                    : null;
                if (!target)
                    throw new InvalidOperationException("Animation Preview Fixture Rig Prefab produced no CharacterPipelineHost.");
                if (!target.VisualRoot)
                    throw new InvalidOperationException("Animation Preview Fixture Rig Prefab has no formal VisualRoot.");
                previewCameraObject = new GameObject("Preview Camera")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                SceneManager.MoveGameObjectToScene(previewCameraObject, scene);
                Camera previewCamera = previewCameraObject.AddComponent<Camera>();
                previewCamera.cameraType = CameraType.Preview;
                previewCamera.enabled = false;
                previewCamera.forceIntoRenderTexture = true;
                previewCamera.clearFlags = CameraClearFlags.Color;
                previewCamera.backgroundColor = new Color(0.055f, 0.055f, 0.065f, 1f);
                previewCamera.allowHDR = false;
                previewCamera.allowMSAA = false;
                previewCamera.overrideSceneCullingMask =
                    sceneCullingMask;
                keyLightInstance = new GameObject("Preview Key Light")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                SceneManager.MoveGameObjectToScene(
                    keyLightInstance,
                    scene);
                Light keyLight = keyLightInstance.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 1.2f;
                keyLight.color = new Color(1f, 0.96f, 0.9f);
                keyLight.shadows = LightShadows.Soft;
                keyLight.transform.rotation = Quaternion.Euler(
                    48f,
                    -32f,
                    0f);
                previewTexture = new RenderTexture(512, 512, 24)
                {
                    name = $"{fixture.name} Preview Render Texture",
                    hideFlags = HideFlags.HideAndDontSave
                };
                previewTexture.Create();
                previewCamera.targetTexture = previewTexture;
                ConfigurePreviewCamera(
                    previewCamera,
                    target.VisualRoot,
                    out Vector3 focus,
                    out float distance);
                var session = new CharacterAnimationPreviewFixtureSession(
                    scene,
                    rigInstance,
                    environmentInstance,
                    keyLightInstance,
                    previewCamera,
                    previewTexture,
                    focus,
                    distance);
                if (!session.Target)
                    throw new InvalidOperationException("Animation Preview Fixture Rig Prefab produced no CharacterPipelineHost.");
                return session;
            }
            catch
            {
                if (previewCameraObject)
                    UnityEngine.Object.DestroyImmediate(previewCameraObject);
                if (keyLightInstance)
                    UnityEngine.Object.DestroyImmediate(keyLightInstance);
                if (previewTexture)
                {
                    previewTexture.Release();
                    UnityEngine.Object.DestroyImmediate(previewTexture);
                }
                if (environmentInstance)
                    UnityEngine.Object.DestroyImmediate(environmentInstance);
                if (rigInstance)
                    UnityEngine.Object.DestroyImmediate(rigInstance);
                if (scene.IsValid() && scene.isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
                throw;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_PreviewCamera)
                m_PreviewCamera.targetTexture = null;
            if (m_PreviewCamera)
                UnityEngine.Object.DestroyImmediate(m_PreviewCamera.gameObject);
            if (m_KeyLightInstance)
                UnityEngine.Object.DestroyImmediate(m_KeyLightInstance);
            if (m_PreviewTexture)
            {
                m_PreviewTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_PreviewTexture);
            }
            if (m_EnvironmentInstance)
                UnityEngine.Object.DestroyImmediate(m_EnvironmentInstance);
            if (m_RigInstance)
                UnityEngine.Object.DestroyImmediate(m_RigInstance);
            if (m_Scene.IsValid() && m_Scene.isLoaded)
                SceneManager.UnloadSceneAsync(m_Scene);
        }

        internal void RenderPreview()
        {
            if (!m_PreviewCamera || !m_PreviewTexture || !m_PreviewTexture.IsCreated())
                return;
            m_PreviewCamera.Render();
        }

        internal void Resize(int width, int height)
        {
            width = Mathf.Clamp(width, 256, 2048);
            height = Mathf.Clamp(height, 256, 2048);
            if (!m_PreviewCamera ||
                m_PreviewTexture &&
                m_PreviewTexture.width == width &&
                m_PreviewTexture.height == height)
                return;
            if (m_PreviewTexture)
            {
                m_PreviewCamera.targetTexture = null;
                m_PreviewTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_PreviewTexture);
            }
            m_PreviewTexture = new RenderTexture(width, height, 24)
            {
                name = "Pose Graph Preview Render Texture",
                hideFlags = HideFlags.HideAndDontSave
            };
            m_PreviewTexture.Create();
            m_PreviewCamera.targetTexture = m_PreviewTexture;
        }

        internal void Orbit(Vector2 delta)
        {
            m_Yaw += delta.x * 0.35f;
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * 0.3f, -80f, 80f);
            ApplyCamera();
        }

        internal void Pan(Vector2 delta)
        {
            float scale = Mathf.Max(0.0005f, m_Distance * 0.0015f);
            m_Focus += (-m_PreviewCamera.transform.right * delta.x +
                        m_PreviewCamera.transform.up * delta.y) * scale;
            ApplyCamera();
        }

        internal void Zoom(float delta)
        {
            m_Distance = Mathf.Clamp(
                m_Distance * Mathf.Exp(delta * 0.08f),
                m_DefaultDistance * 0.15f,
                m_DefaultDistance * 6f);
            ApplyCamera();
        }

        internal void Focus()
        {
            m_Focus = m_DefaultFocus;
            m_Distance = m_DefaultDistance;
            ApplyCamera();
        }

        void ApplyCamera()
        {
            if (!m_PreviewCamera)
                return;
            Quaternion rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            m_PreviewCamera.transform.SetPositionAndRotation(
                m_Focus + rotation * Vector3.back * m_Distance,
                rotation);
        }

        static float NormalizeAngle(float angle) =>
            angle > 180f ? angle - 360f : angle;

        static void ConfigurePreviewCamera(
            Camera camera,
            Transform visualRoot,
            out Vector3 focus,
            out float distance)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(visualRoot.position + Vector3.up, Vector3.one);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer)
                    continue;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            focus = hasBounds
                ? bounds.center
                : visualRoot.position + Vector3.up;
            float radius = hasBounds
                ? Mathf.Max(bounds.extents.magnitude, 0.5f)
                : 0.75f;
            distance = Mathf.Max(radius * 2.6f, 2.4f);
            Vector3 viewDirection = new Vector3(0.32f, 0.18f, -1f).normalized;
            camera.transform.position = focus - viewDirection * distance;
            camera.transform.LookAt(focus);
            camera.fieldOfView = 28f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = Mathf.Max(50f, distance + radius * 4f);
        }
    }
}
