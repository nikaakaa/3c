# Change: 新增独立动画过渡路由模块

## Why

当前项目已经分别拥有BlendStack CrossFade、Stored Pose和局部Inertialization数学实现，但还没有一个与Unity采样、Pose Graph资产和Corin配置解耦的过渡决策模块。直接在现有动画链中同时修改Blend Policy、Projection、Player、BlendStack、Inertialization、Preview和角色资产，影响面过大，无法先单独观察以下控制问题：

- 一次source-target切换究竟选择Standard Blend还是Inertialization。
- Inertialization request如何获得稳定identity并进入明确生命周期。
- 新target尚未准备、capture尚未完成、连续请求、reset和seek时怎样保持原子状态。
- 哪些状态属于通用路由模块，哪些状态必须继续由Pose Runtime拥有。

本change先建立一个独立、可显式驱动的Transition Routing模块。它只处理规则编译、transition决策、typed request生命周期和诊断，不读取现有Profile、Projection或Pose Graph，不采样动画、不计算骨骼Pose，也不修改Corin和当前运行链。

模块通过正式Editor Fixture形成自己的闭环：

```text
Fixture Definition
  -> 显式Compile
  -> Compiled Transition Routing Plan
  -> 显式Frame Step
  -> Transition Decision / Request / Completion
  -> Snapshot与事件时间线
```

后续`refactor-animation-control-boundaries`只能复用该模块的正式合同和状态机，把PoseState transition、AnimationSlot与Inertialization接入；不得复制另一份路由算法。旧`integrate-animation-transition-routing-pipeline`因依赖BaseLocomotion Selection旧拓扑已被该架构change吸收并删除。

## What Changes

- 新增target-neutral Transition Routing模块，输入只使用稳定node、source、target、rule、generation和frame identity。
- 新增`AnimationTransitionBlendLogic`：
  - `StandardBlend`
  - `Inertialization`
- 硬切只表示为`StandardBlend + Duration = 0`。
- `Custom`不进入enum或编译产物。
- Stored Pose不进入Blend Logic；模块只允许外部Stack在完成结果中报告历史压缩事实。
- 新增不可变Transition Rule与完整exact source-target matrix编译。
- 新增typed `PoseInertializationRequest`，但payload不保存Pose、速度、播放器或Unity对象。
- 新增request生命周期：
  - `Idle`
  - `AwaitingTarget`
  - `Prepared`
  - `AwaitingCaptureCompletion`
  - `Committed`
  - `Invalid`
- 新增模块帧输入，显式报告target readiness、capture readiness、capture completion、release completion、reset与generation替换。
- 新增模块帧输出，把路由决策与completion outcome分开，显式报告Standard Blend命令、request准备、capture提交、release完成、等待、许可和结构化错误。
- 新增连续打断状态机：
  - Standard到Standard替换当前普通混合命令。
  - Standard到Inertialization等待capture完成后提交。
  - Inertialization到Inertialization提升request generation并要求consumer rebase。
  - Inertialization期间收到Standard只发布新的上游普通混合命令，不伪造第二个request。
- 新增结构化snapshot与有界事件时间线。
- 新增独立Editor Fixture工作区：
  - 使用模块专属Definition，不引用角色Profile、Pose Graph或Corin资产。
  - 只通过明确按钮Compile、Reset、Step和Run Sequence。
  - 选择资产、打开窗口、修改字段和domain reload均不得自动编译或运行。
  - Fixture显示规则解析、request状态、generation、completion与错误，但不伪造骨骼Pose效果。

## Capabilities

### New Capabilities

- `character-animation-transition-routing-module`：定义独立Transition Routing模块的术语、输入输出、规则编译、request生命周期、中断、Fixture与诊断合同。

### Modified Capabilities

- 无。该change不得修改任何current capability。

## Dependencies And Sequencing

- 可以复用项目既有stable identity、canonical hash和结构化诊断基础类型。
- 不依赖Corin、Animation Presentation Profile、Pose Graph、Projection、Animancer、PlayableGraph或Gameplay Program。
- 不修改`character-animation-selection-runtime`、`character-animation-layer-runtime`、`character-animation-pipeline`等current capability。
- 本change提供`refactor-animation-control-boundaries`直接复用的底层算法与生命周期模块。后续change MAY在本模块实现完成后立即把它接入PoseStateMachine transition和AnimationSlot，不要求用户先把独立Fixture作为单独产品验收。Fixture继续只用于隔离诊断；最终归档时先安装本模块capability，再归档依赖它的动画职责重构delta。
- 后续接入change必须删除现有重复的direct Player pair decision，不得长期保留旧路由和新模块双写。

## Current Spec Comparison

- current specs只定义现有唯一动画执行链，没有独立Transition Routing模块。本change新增孤立能力，不宣称现有Runtime已经消费它。
- current `character-animation-selection-runtime`仍保持直接Player、局部Inertialization或CrossFade-only BlendStack语义；本change期间该口径不变。
- current `character-animation-layer-runtime`仍让BlendStack与Inertialization分别执行现有算法；本change不改变其输入输出或source release。
- current `character-animation-presentation-authoring`的现有Policy schema不变；Fixture使用模块专属Definition，不借用角色正式资产。
- active `add-character-presentation-blend-space`、`add-character-motion-matching-pose-source`和`add-character-animation-virtual-bones`不需要引用本模块，避免并行实施相互阻塞。

## Business Tradeoffs

### 先闭环控制模块

- 收益：可以单独观察规则解析、请求生命周期、连续打断和原子completion，不需要同时排查动画采样、骨骼数学、Graph编译和角色资产。
- 代价：第一阶段只能证明控制合同闭环，不能证明Attack到Hit的最终视觉质量。

### 不在第一阶段实现第二套Pose Runtime

- 收益：不会复制BlendStack、Stored Pose、Quaternion residual或PlayableGraph，后续仍只有一套正式数学实现。
- 代价：Fixture只能显示结构化状态和时间线，不能输出SkinnedMesh姿势。

### 使用正式Fixture而非测试或临时脚本

- 收益：后续可以保留为模块诊断入口，且所有重操作必须由用户明确触发。
- 代价：需要维护一个小型Editor工作区及其专属Definition schema。

## Non-Goals

- 不修改Corin Profile、Pose Graph、Blend Policy、Inertialization Policy或generated Projection。
- 不连接SelectedPosePlayer、BlendSpacePlayer、BlendStack或Inertialization runtime。
- 不创建PlayableGraph、Animancer state、AnimationClip、Avatar、Rig或SkinnedMesh。
- 不计算Pose、速度、Quaternion residual、Per-Bone weight、Stored Pose或Foot Feature。
- 不修改Timeline Preview、Pose Watch或正式Runtime snapshot。
- 不建立全局request bus、运行时节点搜索或隐藏consumer。
- 不新增自动Build、自动Compile、自动Run或选中资产触发的重操作。
- 不新增测试任务或手工验证任务。
