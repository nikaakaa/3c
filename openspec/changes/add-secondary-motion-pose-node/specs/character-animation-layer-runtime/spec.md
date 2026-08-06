# character-animation-layer-runtime Delta

## MODIFIED Requirements

### Requirement: 动画帧必须按固定职责顺序执行

每个PresentationFrame MUST按固定顺序读取committed Body/Intent与Program parameter、构造Fact、求值PoseStateMachine、提交target provider demand、解析readiness、采样state-local source、消费有限Action frame、执行Transition Routing与AnimationSlot、执行Local Pose composition与Virtual Bone派生、显式转换到Component Pose、执行Goal Source与其它Component Pose控制、由唯一FullBodyIK求解Physical全身链、显式转回Local Pose、应用可选SecondaryMotion，最后发布FinalAnimationPoseFrame。SecondaryMotion MUST在所有Actor Base Physical Pose完成后由同一global manual batch执行，并在post-secondary完整Rig capture后提供Final Pose；Action visual sampler MUST只生成有限Action sample，PoseState provider MUST只处理其state-local source。任一阶段 MUST不重新仲裁其它阶段的选择、写回Gameplay或通过独立LateUpdate修改已发布Pose。

#### Scenario: 攻击期间角色速度归零

- **WHEN** FullBodyAction Slot仍有完整权重但Body速度已经归零
- **THEN** PoseStateMachine MUST继续更新到Stop或Idle目标
- **AND** Action结束时Slot MUST回到当时的当前Source Pose

#### Scenario: IK后的裙摆处理

- **WHEN** FullBodyIK改变Corin骨盆和腿部Pose且SecondaryMotion节点相关
- **THEN** 裙摆模拟 MUST读取同帧IK后的Physical Base Pose
- **AND** FinalAnimationPoseFrame MUST只在裙摆修正完成并被完整捕获后发布

### Requirement: Float32与Fixed必须共享同一Presentation Projection

由同一SemanticHash和producer contract生成的Float32 Program与Fixed Program wrapper MUST引用同一套Presentation Projection、Pose source binding、Action binding、Pose Plan、Routing Plan、Secondary Motion Profile、dense Physical Bone/group/collider payload和Rig revision。Secondary Motion team state MUST只存在于Unity Presentation backend，不得进入任一Numeric Program、State、hash或snapshot。Runtime MUST不按ProgramHash复制、选择或降级Projection，也 MUST不从另一target Program反读或复制Magica runtime state。任一Program、Projection、Rig、Secondary Motion Profile或authoring revision不匹配 MUST在preparation阶段失败。

#### Scenario: Fixed角色运行Secondary Motion

- **WHEN** Fixed Gameplay Session提交与Float32相同的Corin Presentation Projection
- **THEN** Fixed角色 MUST使用同一SecondaryMotion节点、Profile和Magica visual backend
- **AND** Rollback snapshot MUST不包含team particle或collision state

#### Scenario: 构建Fixed wrapper

- **WHEN** Fixed Program由当前Definition和Float32 Program生成
- **THEN** wrapper MUST保留同一SemanticHash与Presentation contract
- **AND** MUST不生成第二套动画或Secondary Motion Projection
