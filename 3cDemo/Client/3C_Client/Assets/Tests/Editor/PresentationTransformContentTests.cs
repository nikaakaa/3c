using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Animancer;
using NUnit.Framework;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonPresentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonPresentation.Tests
{
    public sealed class PresentationTransformContentTests
    {
        const string CharacterPrefabPath = "Assets/Prefabs/Character/可琳.prefab";
        const string CameraRigPrefabPath = "Assets/Prefabs/Camera/Third Person Camera Rig.prefab";

        [Test]
        public void CharacterPrefabSeparatesSimulationRootAndVisualRoot()
        {
            GameObject character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            Assert.NotNull(character);

            Transform visualRoot = character.transform.Find("CharacterVisualRoot");
            Assert.NotNull(visualRoot);

            PresentationTransformInterpolator interpolator = visualRoot.GetComponent<PresentationTransformInterpolator>();
            Assert.NotNull(interpolator);
            Assert.AreSame(character.transform, interpolator.Source);
            Assert.AreSame(visualRoot, interpolator.VisualTarget);

            Assert.NotNull(character.GetComponent<CharacterController>());
            Assert.Null(character.GetComponent<Animator>());
            Assert.Null(character.GetComponent<AnimancerComponent>());
            Assert.Null(character.GetComponent<BasicLocomotionAnimancerPresenter>());

            Assert.NotNull(visualRoot.GetComponent<Animator>());
            Assert.NotNull(visualRoot.GetComponent<AnimancerComponent>());
            Assert.NotNull(visualRoot.GetComponent<BasicLocomotionAnimancerPresenter>());
        }

        [Test]
        public void CameraRigPrefabKeepsGenericPresentationAnchor()
        {
            GameObject cameraRig = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefabPath);
            Assert.NotNull(cameraRig);

            ThirdPersonCameraController controller = cameraRig.GetComponent<ThirdPersonCameraController>();
            Assert.NotNull(controller);

            Transform presentationAnchor = cameraRig.transform.Find("PresentationAnchor");
            Assert.NotNull(presentationAnchor);
            Assert.NotNull(presentationAnchor.GetComponent<PresentationTransformInterpolator>());
            Assert.AreSame(presentationAnchor, controller.FollowAnchorSource);
        }

        [Test]
        public void SandboxCameraFollowsCharacterVisualRoot()
        {
            string sceneYaml = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes", "Sandbox.unity"), Encoding.UTF8);
            string characterGuid = AssetDatabase.AssetPathToGUID(CharacterPrefabPath);
            string visualRootId = FindCharacterVisualRootSceneId(sceneYaml, characterGuid);

            Assert.NotNull(visualRootId);
            StringAssert.Contains($"objectReference: {{fileID: {visualRootId}}}", sceneYaml);
        }

        static string FindCharacterVisualRootSceneId(string sceneYaml, string characterGuid)
        {
            string escapedGuid = Regex.Escape(characterGuid);
            Match match = Regex.Match(
                sceneYaml,
                $@"--- !u!4 &(?<id>\d+) stripped\s+Transform:\s+m_CorrespondingSourceObject: {{fileID: 7123456789012345679, guid: {escapedGuid},\s+type: 3}}",
                RegexOptions.Multiline);

            return match.Success ? match.Groups["id"].Value : null;
        }
    }
}
