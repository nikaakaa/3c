using NUnit.Framework;
using System.Reflection;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace Tests.Editor.Character.Action.Timeline
{
    public sealed class ActionTimelineContractTests
    {
        [Test]
        public void ValidatorRejectsInvalidClipRange()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                8,
                                3,
                                ActionTimelineClipPayload.Motion(CreateMotionSpec()))
                        })
                });

            ActionTimelineValidationResult result = ActionTimelineValidator.Validate(timeline);

            Assert.True(result.HasErrors);
            CollectionAssert.Contains(result.Errors, "clip-range-invalid:0:0");
        }

        [Test]
        public void EmptyTimelineHasNoValidationErrorsAndNoOutcome()
        {
            ActionTimelineValidationResult validation = ActionTimelineValidator.Validate(ActionTimelineDefinition.Empty);
            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(ActionTimelineDefinition.Empty, 0, 1));

            Assert.False(validation.HasErrors);
            Assert.False(outcome.HasOutcome);
        }

        [Test]
        public void EvaluatorOutputsActiveTimelineDataForCurrentFrame()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                12,
                                ActionTimelineClipPayload.Animation(ActionAnimationKeys.DodgeDirectional))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                3,
                                8,
                                ActionTimelineClipPayload.Motion(CreateMotionSpec()))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Cancel,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.CancelWindow,
                                4,
                                6,
                                ActionTimelineClipPayload.Fact("cancel.dodge.after-start"))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Hitbox,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.HitboxWindow,
                                5,
                                7,
                                ActionTimelineClipPayload.Fact("hitbox.dodge.body"))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Cue,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Cue,
                                5,
                                5,
                                ActionTimelineClipPayload.Cue("cue.dodge.flash"))
                        })
                });

            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 5, 22));

            Assert.True(outcome.HasAnimation);
            Assert.True(outcome.HasMotion);
            Assert.True(outcome.HasCue);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, outcome.AnimationKey);
            CollectionAssert.Contains(outcome.ActiveWindowFactIds, "cancel.dodge.after-start");
            CollectionAssert.Contains(outcome.ActiveWindowFactIds, "hitbox.dodge.body");
            Assert.AreEqual("cue.dodge.flash", outcome.CueRequests[0].CueId);
        }

        [Test]
        public void ActionBranchTimelineNodeProducesCandidateWithoutWritingFacts()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                12,
                                ActionTimelineClipPayload.Animation(ActionAnimationKeys.DodgeDirectional))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                12,
                                ActionTimelineClipPayload.Motion(CreateMotionSpec()))
                        })
                });
            ActionBranchDefinition branch = ActionBranchDefinition.Define(
                "action",
                ActionStateIds.Dodge,
                ActionNodeDefinition.Timeline("dodge-timeline", timeline),
                BodyOccupancyClaim.FullBodyAction(30));

            ActionBranchOutcome outcome = ActionBranchEvaluator.Evaluate(
                new ActionBranchEvaluationInput(branch, 1, 30));

            Assert.True(outcome.HasOutcome);
            Assert.True(outcome.Candidate.HasMotionCandidate);
            Assert.True(outcome.Candidate.HasAnimationCandidate);
            Assert.True(outcome.BodyClaim.ClaimsFullBody);
        }

        [Test]
        public void EvaluatorUsesStartInclusiveEndExclusiveFrameBoundaries()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                8,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                2,
                                4,
                                ActionTimelineClipPayload.Animation(ActionAnimationKeys.DodgeDirectional))
                        })
                });

            Assert.False(ActionTimelineEvaluator.Evaluate(new ActionTimelineEvaluationInput(timeline, 1, 1)).HasAnimation);
            Assert.True(ActionTimelineEvaluator.Evaluate(new ActionTimelineEvaluationInput(timeline, 2, 1)).HasAnimation);
            Assert.True(ActionTimelineEvaluator.Evaluate(new ActionTimelineEvaluationInput(timeline, 3, 1)).HasAnimation);
            Assert.False(ActionTimelineEvaluator.Evaluate(new ActionTimelineEvaluationInput(timeline, 4, 1)).HasAnimation);
        }

        [Test]
        public void EvaluatorKeepsOverlappingWindowFactsAndIgnoresEmptyTracks()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                10,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new ActionTimelineClipDefinition[0]),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Hitbox,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.HitboxWindow,
                                2,
                                6,
                                ActionTimelineClipPayload.Fact("hitbox.primary")),
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.HitboxWindow,
                                4,
                                8,
                                ActionTimelineClipPayload.Fact("hitbox.secondary"))
                        })
                });

            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 5, 4));

            Assert.False(outcome.HasAnimation);
            CollectionAssert.Contains(outcome.ActiveWindowFactIds, "hitbox.primary");
            CollectionAssert.Contains(outcome.ActiveWindowFactIds, "hitbox.secondary");
        }

        [Test]
        public void EvaluationInputResolvesAuthorityFrameFromStateTimeAndTickInterval()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                5,
                                6,
                                ActionTimelineClipPayload.Animation(ActionAnimationKeys.DodgeDirectional))
                        })
                });

            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 0.1f, 0.02f, 19));

            Assert.AreEqual(5, outcome.CurrentFrame);
            Assert.True(outcome.HasAnimation);
            Assert.AreEqual(0, ActionTimelineEvaluationInput.ResolveFrame(-1f, 0.02f));
            Assert.AreEqual(0, ActionTimelineEvaluationInput.ResolveFrame(0.1f, 0f));
        }

        [Test]
        public void CueTriggersOnlyAtStartFrameAndEvaluatorKeepsNoStaticState()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                10,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Cue,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Cue,
                                3,
                                3,
                                ActionTimelineClipPayload.Cue("cue.dodge.flash"))
                        })
                });

            ActionTimelineOutcome before = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 2, 7));
            ActionTimelineOutcome atStart = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 3, 7));
            ActionTimelineOutcome after = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 4, 7));
            ActionTimelineOutcome replay = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 3, 7));

            Assert.False(before.HasCue);
            Assert.True(atStart.HasCue);
            Assert.False(after.HasCue);
            Assert.True(replay.HasCue);
            Assert.AreEqual(0, typeof(ActionTimelineEvaluator).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length);
        }

        static ActionMotionSpec CreateMotionSpec()
        {
            return new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                false,
                Vector3.forward,
                0f,
                0);
        }
    }
}
