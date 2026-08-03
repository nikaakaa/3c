# 动画职责边界最终对账

## 正式代码链

输入：

- Gameplay Program只提交有限Action的`AnimationChannelId`、完整`AnimationPlaybackId`和committed raw sample。
- committed Body与Intent生成只读`CharacterPresentationFactFrame`。

处理：

- `CharacterActionPlaybackRuntime`只拥有有限Action command inbox、逐Playback registry、committed sample history、usage、retirement permission与backend release completion。
- `CharacterAnimationPresentationRuntime`只编排表现帧事务，依次处理Fact、PoseStateMachine、state-local provider、Action sample projection、AnimationSlot、Transition Routing、Pose Plan、source release和最终发布。
- 持续Pose source与有限Action使用不同identity、binding index、workspace和release协议。
- Timeline Preview、Pose Graph Fact Preview与Motion Matching Query Fixture只通过统一`AnimationPreviewRuntime`提交各自typed adapter输入。

输出：

- 每个表现帧只发布一次`FinalAnimationPoseFrame`。
- Presentation Projection保存PoseStateMachine、state-local source、AnimationSlot、Transition Routing、Rig v3和ordered staged Pose Plan。
- Float32与Fixed Program共享同一Presentation Projection，但Pose workspace、Graph内存和runtime state不进入Rollback snapshot或网络协议。

删除的旧路径：

- Gameplay `BaseLocomotion` AnimationChannel、producer、Selection Input、binding、lifecycle、retention与diagnostics。
- `ActionOverride`与旧ownership Blackboard数据。
- `CharacterAnimationPlaybackRuntime`共享总管、旧BindingIndex、Pose Runtime按channel反查Action playback和旧Preview分支。
- 角色运行时即时编译Transition Routing、隐藏Player/Stack/Inertialization、第二PlayableGraph与Animator direct play。

## Delta对账

- Selection Runtime：持续Pose source与有限Action ABI、readiness、usage与release已经分离。
- Layer Runtime：基础Pose只来自PoseStateMachine的state-local source；有限Action只经AnimationSlot插入。
- Pose Graph：完整PoseStateMachine、Slot、Routing、Rig v3、IK、FootPlacement与Output在同一Pose Plan。
- Presentation Authoring：Profile、source、Action producer、marker、curve、Policy、Rig与Navigator保持唯一owner。
- Animation Pipeline：表现协调器与Action Playback Runtime职责、帧事务和一次final publication已明确。
- Corin State Timeline：Gameplay Locomotion只保留影响Simulation的状态、Motion与时序；表现Timeline数据已迁入Pose source。
- Action ownership：Action Timeline继续拥有有限Action权威logic time与committed raw sample。
- Motion semantics：PoseState time不成为Gameplay Motion真相，MotionWarp仍只处理正式Action motion owner。
- Pipeline Definition/Runtime：精确Profile、Program、Projection装配与启动readiness已经闭合。
- Presentation Interpolation：Action表现采样不写回Timeline、Window、Motion、Cue或Action lifecycle。
- State Interruption：Gameplay打断与动画fade/release分离。
- Foot Analysis：Build消费Editor-only artifact，Runtime只读Projection。
- Timeline Preview：统一Preview Runtime，不恢复Gameplay或第二播放链。
- Agent Character Controller：Document v3只通过共享Capability与Presentation Mutation编辑正式Presentation owner。
- Transition Routing：独立模块只提供编译期route decision与capture/release协议，不拥有PoseState、Slot或final Pose。
- Equipment Presentation：有限Action继续进入Action Playback Runtime与AnimationSlot。
- Foot Placement：只消费同帧最终Pose输入，不重新仲裁source或采样Projection。

## Active change关系

- `refactor-pose-graph-to-btsmtl-authoring-domain`拥有共享作者UI、Document v3、Presentation Mutation、Pose IR与Corin正式事务。
- `add-action-animation-authoring-workspace`只组合既有owner，不拥有新数据或第二Preview链。
- `complete-composable-pose-graph-editor-workflow`已经把Rig升级为v3，并把TwoBoneIK与FootPlacement收进同一空间化Pose Plan。
- Blend Space与Motion Matching剩余独立内容继续放在Rollback闭环后的队列，不修改Corin Rollback装配。
- 旧`integrate-animation-transition-routing-pipeline`已经由本change吸收并删除，不再是后续依赖。

## 表现帧事务实施纠正

- 本change任务34建立了表现帧事务的业务边界，但当时的实现仍通过完整`FrameState`、Module state与Physical Bone before-image完成失败恢复，不能作为真实staged transaction的最终证据。
- 本change任务37.23当时未清除上述每帧状态和骨骼复制，因此“已清除Presentation每帧分配或重复工作”的完成结论不成立。
- `refactor-animation-presentation-frame-transaction`已破坏性删除旧快照恢复链，改为预分配Committed/Pending双页、固定mutation journal、prepared/deferred source lifecycle、唯一Animancer Evaluate Barrier与Actor Fault边界；任务34与37.23的最终实现证据以该change为准。
