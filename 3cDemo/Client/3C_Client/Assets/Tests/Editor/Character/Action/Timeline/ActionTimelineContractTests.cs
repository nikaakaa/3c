using NUnit.Framework;
using System;
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
            CollectionAssert.Contains(result.Errors, "clip-tick-range-invalid:0:0");
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
        public void EvaluatorOutputsActiveTimelineDataForCurrentTick()
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
        public void CommittedActionBranchTimelineNodeProducesCandidateWithoutWritingFacts()
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
            CommittedActionBranchDefinition branch = CommittedActionBranchDefinition.Define(
                "action",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Timeline("dodge-timeline", timeline),
                BodyOccupancyClaim.CommittedActionFullBody(30));

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 1, 30));

            Assert.True(outcome.HasOutcome);
            Assert.True(outcome.Candidate.HasMotionCandidate);
            Assert.True(outcome.Candidate.HasAnimationCandidate);
            Assert.True(outcome.BodyClaim.ClaimsFullBody);
        }

        [Test]
        public void EvaluatorUsesStartInclusiveEndExclusiveTickBoundaries()
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
        public void QuantizerCompilesSecondsToAuthorityTicks()
        {
            ActionTimelineCompileContext context = new ActionTimelineCompileContext(1f / 60f);

            Assert.AreEqual(21, ActionTimelineQuantizer.QuantizeSecondsToTick(0.35f, in context));
            Assert.AreEqual(3, ActionTimelineQuantizer.QuantizeSecondsToTick(0.05f, in context));
            Assert.AreEqual(5, ActionTimelineQuantizer.QuantizeSecondsToTick(0.08f, in context));
            Assert.AreEqual(12, ActionTimelineQuantizer.LegacyFrameToTick(12, in context));
            Assert.AreEqual(0.2f, ActionTimelineQuantizer.LegacyFrameToSeconds(12, in context), 0.0001f);
            Assert.Throws<ArgumentOutOfRangeException>(() => ActionTimelineQuantizer.QuantizeSecondsToTick(-0.01f, in context));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActionTimelineCompileContext(0f));
        }

        [Test]
        public void AuthoringSecondsCompileToRuntimeTicks()
        {
            ActionTimelineCompileContext context = new ActionTimelineCompileContext(1f / 60f);
            CommittedActionBranchTimelineAuthoring authoring = new CommittedActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                0.35f,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                4f / 60f,
                                13f / 60f,
                                ActionTimelineClipPayloadAuthoring.Animation(ActionAnimationKeys.DodgeDirectional.Value))
                        }),
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Cue,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.Cue,
                                5f / 60f,
                                5f / 60f,
                                ActionTimelineClipPayloadAuthoring.Cue("cue.dodge.flash"))
                        })
                });

            ActionTimelineDefinition timeline = authoring
                .ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 17, in context)
                .RootNode
                .TimelineNode
                .Timeline;

            Assert.AreEqual(21, timeline.DurationTicks);
            Assert.AreEqual(4, timeline.Tracks[0].Clips[0].StartTick);
            Assert.AreEqual(13, timeline.Tracks[0].Clips[0].EndTick);
            Assert.AreEqual(5, timeline.Tracks[1].Clips[0].StartTick);
            Assert.AreEqual(5, timeline.Tracks[1].Clips[0].EndTick);
        }

        [Test]
        public void CompileDoesNotFallbackToLegacyFrameFieldsWhenSecondsAreMissing()
        {
            ActionTimelineCompileContext context = new ActionTimelineCompileContext(1f / 60f);
            ActionTimelineClipAuthoring clip = new ActionTimelineClipAuthoring(
                ActionTimelineClipKind.AnimationKey,
                0f,
                0f,
                ActionTimelineClipPayloadAuthoring.Animation(ActionAnimationKeys.DodgeDirectional.Value));
            SetStructField(ref clip, "legacyStartFrame", 4);
            SetStructField(ref clip, "legacyEndFrame", 13);
            CommittedActionBranchTimelineAuthoring authoring = new CommittedActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                0f,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[] { clip })
                });
            SetStructField(ref authoring, "legacyDurationFrames", 21);

            ActionTimelineDefinition timeline = authoring
                .ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 17, in context)
                .RootNode
                .TimelineNode
                .Timeline;

            Assert.AreEqual(0, timeline.DurationTicks);
            Assert.AreEqual(0, timeline.Tracks[0].Clips[0].StartTick);
            Assert.AreEqual(0, timeline.Tracks[0].Clips[0].EndTick);
            Assert.False(ActionTimelineEvaluator.Evaluate(new ActionTimelineEvaluationInput(timeline, 5, 17)).HasAnimation);
        }

        [Test]
        public void AuthoringValidatorRejectsInvalidSecondsBeforeTickCompilation()
        {
            ActionTimelineCompileContext context = new ActionTimelineCompileContext(1f / 60f);
            CommittedActionBranchTimelineAuthoring authoring = new CommittedActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                0.35f,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                0.2f,
                                0.1f,
                                ActionTimelineClipPayloadAuthoring.Animation(ActionAnimationKeys.DodgeDirectional.Value))
                        })
                });
            CharacterActionCatalogValidationResult result = new CharacterActionCatalogValidationResult();

            authoring.ValidateInto(result, "Dodge", ActionStateIds.Dodge, 17, in context);

            Assert.True(result.HasErrors);
            CollectionAssert.Contains(result.Errors, "Dodge clip seconds range is invalid:0:0.");
        }

        [Test]
        public void CueTriggersOnlyAtStartTickAndEvaluatorKeepsNoStaticState()
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

        static void SetStructField<T>(ref T value, string fieldName, object fieldValue) where T : struct
        {
            object boxed = value;
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(boxed, fieldValue);
            value = (T)boxed;
        }
    }
}
