# Design: Corin Idle 脚底锁定

## Context

Corin 的表现链已经具备脚部分析、地面查询、Plant/Release 迟滞、移动 Surface anchor、Pelvis Reach Planner 和唯一 FullBodyIK。当前问题不是缺少算法，而是正式 Corin Profile 把锁脚策略设成了 `Unlocked`，导致 Foot Placement 只做当前接地修正，不建立持续脚锚点。

Idle 动画本身仍应允许胸腔、骨盆和武器产生轻微运动。脚锁只负责接触约束，不能把全身姿势冻结。实现边界固定为：Foot Placement 生产 typed FullBodyIK Goals，FullBodyIK 在同一 Component Pose 上完成唯一骨骼求解。

## Formal Chain

```text
Idle Pose Source
  -> Idle Foot Placement Weight
  -> Final Pose Foot Features
  -> PredictiveFootPlacement
       -> Current Support / Plant Confidence
       -> Free -> Locked -> Sliding 生命周期
       -> Surface-local Foot Anchor
       -> Pelvis Reach Plan
       -> Foot Goals
  -> FullBodyIK
  -> Output Pose
```

输入是同帧最终 Component Pose、Idle source 的 Foot Placement Weight、Foot Analysis 特征、Body Grounded 和正式 PhysicsScene 查询结果。

输出是双脚的 Component-space Goal、Pelvis pre-solve offset、约束状态和最终 FullBodyIK Pose。脚锁状态只存在 Presentation runtime，不进入 Gameplay State、World State、Snapshot、Hash 或网络包。

## Decisions

### Decision 1: 使用统一 `PivotAroundToe`

Corin 只有一个正式 Foot Placement Profile，当前 Profile 没有 source-local LockType。选择 `PivotAroundToe` 作为角色级策略，让脚尖在接触面上保持锚定，同时允许脚跟在 Idle 呼吸、斜坡和起停动作中做受限旋转。这样使用现有配置边界，不增加第二份 source binding 数据。

`LockRotation` 不作为首选，因为它会把 Idle 的骨盆运动转成更明显的膝盖和踝关节拉扯；对走停和斜坡也更容易产生僵硬感。若后续业务要求鞋底整面绝对不动，应另起内容调校 change，而不是在本 change 中增加状态名特判。

### Decision 2: Idle 的脚锁由 Foot Analysis 负责进入，不由状态名强制

Idle binding 保持全程 `Foot Placement Weight = 1`，Plant Confidence 继续由 Foot Analysis 表达动画接触意图。Runtime 只根据最终脚速、Surface distance、Body Grounded 和迟滞阈值进入或释放锁定，不读取 `Idle` 字符串，不创建第二个 Blackboard foot flag，也不把 Gameplay Timeline 重新变成脚相位权威。

这样 Idle、Walk、Run 和有限 Action 共用同一个生命周期；业务差异只由 source-local Weight、Foot Features 和当前地面事实表达。

### Decision 3: 内容验证在显式 Character Build 内执行

Corin 内容验证只检查正式 Build 输入：Profile 锁脚模式、Idle binding、Foot Analysis identity、权重曲线、Rig/Calibration/Projection revision。编辑器选择、Inspector 重绘、`OnValidate`、Preview 和普通 dirty 操作只更新 revision 或 Stale 状态，不运行分析、编译、Physics 查询或 Program Build。

Build 先执行轻量输入验证，再执行需要显式请求的 Projection/Program 编译；所有输出通过现有原子发布组提交。任何 identity、容量或目标链错误都在重操作前失败。

### Decision 4: 不重新生成无依赖变化的 Foot Analysis

本 change 只改变 Foot Placement Profile 的锁脚策略，Foot Analysis 的 Clip、Rig、Calibration、算法版本和 sampling identity 不变，因此不重新生成 Foot Analysis artifact。只有这些输入发生变化时，才按现有 Foot Analysis Build 入口重新分析，并让后续 Character Build 消费新的 exact artifact。

这样可以减少 Editor 重操作，也避免把“锁脚策略改变”和“动画分析重做”混成一个不可审查的按钮。

## Tradeoffs

### 统一 `PivotAroundToe`

优点是改动小、复用同一正式 Profile、不会产生 Idle 专用 runtime 分支，适合求职 Demo 中既展示 Idle 稳定又保留动作感。代价是脚跟可能存在受控转动，不能承诺鞋底每个点都绝对固定。

### 统一 `LockRotation`

优点是静态地面上的脚最稳定，截图和近景观察最直接。代价是全角色所有启用 Foot Placement 的 source 都会更硬，双脚同时 Plant 时骨盆和膝盖的可解空间更小。本 change 不选择它。

### 增加 source-local LockType

优点是 Idle 可以使用 `LockRotation`、Locomotion 使用 `PivotAroundToe`、Action 使用单独策略。代价是需要扩展 Animation Presentation binding、Projection ABI、Runtime tuning owner 和迁移规则，增加作者选择，也会让脚锁策略再次分裂。当前没有证据表明统一 `PivotAroundToe` 不够，因此不纳入本 change。

### 只清理 Idle 动画脚轨迹

优点是运行时求解压力较小。代价是无法处理斜坡、台阶、移动平台、Root/Body 插值和动画重定向造成的世界接触误差，不能替代正式 Foot Placement。它作为后续动画内容优化，不作为本 change 的解决链。

## Failure And Publication Rules

- Profile 为 `Unlocked`、Idle binding 缺失、Idle Weight 不是完整 `1` 曲线、Foot Analysis identity 不匹配或旧 Pose/IK contract 未收口时，显式 Build 直接失败。
- Stale Projection 不得被 Preview 或 Runtime 消费；不得从旧 Projection、旧 LegIK、Animator Transform 或旧 Profile 推断值。
- Build 失败只保留旧发布组，不写半套 Projection、Program、tuning layout 或 wrapper。
- Runtime 中 Surface 无效、Body 离地、脚不可达、超出 Replant 或发生 Reset 时，继续使用现有 `Free/Locked/Sliding` 释放规则。

## Non-goals

- 不复制 Unreal、FinalIK 或 Animation Rigging 的实现代码。
- 不把 GASP 的 Leg IK 作为项目第二条执行路径。
- 不在 `OnInspectorGUI`、`OnValidate`、selection 或 preview 中执行重操作。
- 不改 Simulation、KCC、Root Motion、Motion Matching、Timeline 或 Network contract。
