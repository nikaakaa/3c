# Change: 将动画过渡路由模块接入唯一Pose Plan

## Why

`add-animation-transition-routing-module`先独立闭环Blend Logic、exact rule、typed request、generation、capture/release握手、reset与诊断，但它明确不连接现有动画系统。模块归档后，项目仍然维持当前正式链：

```text
Selection
  -> 直接Player + 可选局部Inertialization
  或CrossFade-only BlendStack
  -> Pose composition
```

该拓扑仍要求作者在“直接Player惯性化”和“BlendStack普通混合”之间二选一，FullBodyAction的BlendStack也不能像UE对应工作流一样为每个source-target pair选择Standard Blend或请求下游Inertialization。

本change只负责第二阶段接入：把已归档Transition Routing模块安装到现有唯一Pose Plan，迁移正式Policy、Projection、Player、BlendStack、Inertialization、Preview和Corin资产，并删除旧的直接Player pair decision。它不重新实现路由状态机，也不保留新旧双写。

## What Changes

- `CharacterAnimationBlendTransitionRule`接入模块的`AnimationTransitionBlendLogic`：
  - `StandardBlend`由BlendStack执行现有CrossFade。
  - `Inertialization`由Player或BlendStack构造Frame Facts，模块发布typed request，下游显式Inertialization执行现有残差数学。
  - Hard Cut统一为`StandardBlend + Duration = 0`。
- Blend Policy成为source-target业务过渡选择的唯一真相。
- Inertialization Policy删除直接Player source-target matrix，只保留consumer数学配置、参数过滤与reset配置。
- Stored Pose继续只属于BlendStack容量和历史压缩，不进入Blend Logic。
- Pose Graph Compiler为每个兼容producer建立到唯一Inertialization consumer的静态route。
- 第一阶段兼容producer固定为：
  - `SelectedPosePlayer`
  - `BlendSpacePlayer`
  - `BlendStack`
- Runtime不得建立全局request bus、按名称寻找consumer或自动插入Inertialization。
- Player与BlendStack把正式source、target、readiness和generation降低为模块Frame Facts。
- Pose Plan completion把target sample、consumer capture和source release completion回报同一个模块workspace。
- BlendStack继续唯一拥有Standard Blend entry、Stored Pose、Per-Bone Blend Profile、capacity和source release。
- Inertialization继续唯一拥有completed Pose history、速度、residual、衰减和rebase。
- Standard Blend到Inertialization、Inertialization到Inertialization以及Inertialization期间上游Standard Blend全部复用模块已经归档的状态机决定。
- Pose Graph、Profile工作区、Timeline Preview、Pose Watch和Live Debug显示同一request route与lifecycle。
- Corin正式图迁移为：

```text
BaseLocomotion Selection
  -> MarkerSync
  -> BlendSpacePlayer
  -> Locomotion Inertialization

FullBodyAction Selection
  -> Action BlendStack
  -> Action Inertialization

Locomotion Pose + Action Pose
  -> Layered Blend Per Bone
  -> Pose Parameter Resolve
  -> Foot Placement
  -> Output Pose
```

- Corin Action Blend Policy按全部当前可达稳定producer identity物化完整Blend Logic matrix。
- 删除旧直接Player Inertialization pair rule、旧route descriptor、重复diagnostic和旧Projection字段。
- 旧资产和Projection不兼容读取，不保留fallback、converter或双路径。

## Capabilities

### New Capabilities

- `character-animation-transition-routing`：定义Transition Routing模块进入Pose Plan后的producer、consumer、静态route、capture、release、中断和Preview合同。

### Modified Capabilities

- `character-animation-selection-runtime`：让Player与BlendStack按exact Blend Logic执行Standard Blend或驱动Transition Routing模块。
- `character-animation-layer-runtime`：建立BlendStack、Routing模块、Inertialization与source backend之间的唯一运行链。
- `character-animation-presentation-authoring`：让Blend Policy唯一保存业务Blend Logic，让Inertialization Policy只保存consumer数学配置。
- `character-animation-pipeline`：把模块decision、request、capture和release纳入同一次Pose Plan completion。
- `character-presentation-pose-graph`：显示显式request route、consumer和UE对应术语。
- `btsmtl-timeline-editor-preview`：复用正式Routing模块和Pose Plan，不创建简化dispatcher。

## Dependencies And Sequencing

- 硬依赖`add-animation-transition-routing-module`已经由用户跑通并归档。只完成代码但未归档时不得开始本change。
- 依赖`refactor-animation-selection-pose-graph-boundary`、`refactor-inertial-blending-to-local-pose-node`、`refactor-animation-playback-to-blend-stack`和`add-character-presentation-pose-graph`已经建立唯一Pose Plan与职责分离。
- 复用`add-character-presentation-blend-space`的BlendSpacePlayer Pose Discontinuity和单Pose输出。
- `add-character-motion-matching-pose-source`继续只输出Selection identity，不拥有Blend Logic。
- `add-character-animation-virtual-bones`继续让完整Pose Bone page经过BlendStack、Stored Pose和Inertialization；本change不修改Pose Bone ABI。
- 实施顺序固定为：
  1. 接入已归档模块contract。
  2. 升级Policy和Projection schema。
  3. 编译静态request route。
  4. 接入Runtime completion。
  5. 更新Preview与Diagnostics。
  6. 原子迁移Corin资产。
  7. 删除旧字段、旧decision和旧文档口径。
- 不得在任何中间阶段让正式Runtime同时读取旧pair matrix和新Routing Plan。

## Current Spec Comparison

- current `character-animation-selection-runtime`规定Selection通过直接Player、可选局部Inertialization或CrossFade-only BlendStack降低为Pose。本change改为Player/BlendStack统一调用Routing模块，下游Inertialization消费typed request。
- current `character-animation-layer-runtime`已经分离BlendStack与Inertialization数学owner，但没有两者之间的正式控制协议。本change只增加模块接线，不移动数学所有权。
- current `character-animation-presentation-authoring`存在Blend Policy与直接Player Inertialization pair matrix两份业务选择。本change删除后者。
- current `character-presentation-pose-graph`没有显示request route和consumer。本change把编译路由加入画布、Details和Live状态。
- current `btsmtl-timeline-editor-preview`按直接硬切、局部Inertialization或BlendStack三选一执行。本change改为执行相同Routing Plan。
- active `add-character-presentation-blend-space`方向兼容，但其“Discontinuity直接触发下游惯性化”必须改成按exact Blend Logic提交Frame Facts。
- active `add-character-motion-matching-pose-source`中的CrossFade matrix与直接Player Inertialization matrix二选一口径必须同步迁移。
- active `add-character-animation-virtual-bones`不冲突；capture与release必须覆盖同一完整Pose Bone page。
- `openspec/project.md`的“BlendStack只拥有CrossFade/Stored/source release；Inertialization只拥有history/residual/rebase”继续成立，只补充二者通过已归档Routing模块协作。

## Business Tradeoffs

### 第二阶段才迁移正式板块

- 收益：Routing状态机已经在独立Fixture闭环，接入阶段只排查Pose facts、Projection和资源生命周期。
- 代价：必须等待前置change归档，整体交付被明确分成两个串行阶段。

### 一次性切换正式配置

- 收益：归档后只有Blend Policy、Routing Plan和下游consumer一条真相，不留下兼容债务。
- 代价：Policy schema、Projection ABI和Corin资产必须在同一迁移窗口完成，未完成时可以明确报错但不能继续旧链。

### 保留显式编译路由

- 收益：每个request的producer、consumer和作用分支在构建期可证明，避免上半身请求误作用全身。
- 代价：组合节点若要传播请求，必须正式声明透明性，不能依赖UE式隐藏全图消息。

## Breaking Changes

- Blend Policy schema升级并增加Blend Logic。
- Inertialization Policy删除source-target业务matrix。
- Projection增加Routing Plan、request route和module workspace descriptor。
- Pose Plan删除“Inertialization只能直接接单Player”的限制。
- Corin Action BlendStack后新增Action Inertialization。
- 旧Policy、旧Projection、旧direct route和旧diagnostic字段直接删除。

## Non-Goals

- 不修改前置Transition Routing模块的状态机语义。
- 不保留或扩展前置Editor Fixture为正式角色播放器。
- 不实现Custom Blend、Dead Blending、Pose Snapshot或Ragdoll Get-Up。
- 不实现Hit、Death、命中Solver或Gameplay打断判定。
- 不实现全局request bus、隐藏consumer或自动节点插入。
- 不修改Gameplay Body、Root Motion、WorldSolver、Network Model或Numeric Target ABI。
- 不新增自动Build、自动Projection发布或选中资产触发重编译。
- 不新增测试任务或手工验证任务。
