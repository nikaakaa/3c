# Character Pipeline 当前收口指针

## 文档地位

本文档只保留当前尚未闭合的串行顺序。已完成change的proposal、design、tasks与delta已进入`openspec/changes/archive/`，历史Program Hash、Projection Revision、Collision Hash、旧Rig版本和旧阶段编号不再作为当前执行依据。

当前真相只来自`openspec/project.md`、`openspec/specs/`与下列active change。

## 已安装主链

```text
CharacterPipelineDefinition
  -> Agent Document v3 / shared Graph authoring
  -> Semantic IR
  -> requested Float32 or Fixed Program
  -> Presentation Projection
  -> PoseStateMachine / state-local source
  -> AnimationSlot
  -> Local Pose stages
  -> LocalToComponentPose
  -> Component Pose controls
  -> FootPlacement pelvis + component.biped-leg-targets
  -> LegIK
  -> ComponentToLocalPose
  -> OutputPose
  -> FinalAnimationPoseFrame
```

Rig v3、Virtual Bone、TwoBoneIK、Transition Routing、Blend/Inertialization、Action Workspace、显式Pose空间、FootPlacement/LegIK分离、Corin Character Build、Deterministic KCC与楼梯双表面基座均已归档。它们不得被active change重新实现或建立第二路径。

## 唯一串行顺序

### 1. Corin训练AI收口

完成`add-corin-training-ai-demo`剩余的AI Program与Document v3验证任务。Character Program、Projection、Foot Analysis与Pose Program已经是可消费前置；AI只能重新发布并校验精确身份，不得回绑旧Program、回退Neutral或新增Scene Bot旁路。

### 2. Local Fixed与Rollback产品再闭合

只通过`close-deterministic-rollback-character-pipeline`继续：

1. 使用当前Float32/Fixed Program、Presentation Projection、Native Pose Program、KCC与Collision Artifact精确身份准备Local Fixed。
2. 完成Local Fixed组合对账。
3. 由显式Product Build原子发布DeterministicRollback产品。
4. 通过正式Run入口对账Relay与Peer A/B。

Run只消费已发布manifest，不得临时build、修复资产、切换KCC或从历史产品推导当前身份。

## 独立队列

以下工作不得阻塞上述主链，也不得修改Corin Rollback装配：

- `add-character-presentation-blend-space`的独立演示内容。
- `add-character-motion-matching-pose-source`的独立正式内容配置。
- `add-local-deterministic-simulation-debugger`。
- `add-discrete-stair-presentation`。
- `support-authored-stair-traversal-policies`。

楼梯表现与作者策略只能消费已安装的Gameplay Traversal、FootPlacement Surface、KCC与Pose合同，不得把旧隐藏坡道、硬编码台阶或动画反推运动带回主链。

## 重操作边界

Compile、Foot Analysis、Character Build、Collision Bake、Product Build与Run必须由明确命令触发。Inspector、selection、窗口恢复、Preview、AssetDatabase refresh和Document apply不得自动执行任一重操作。

## 停止条件

只有出现必须绕过当前正式系统、缺少关键业务选择或会产生第二GraphView、第二Mutation、第二Compiler、双写、fallback或角色私有旁路时停止实施并要求决策。
