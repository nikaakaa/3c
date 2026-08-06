# Design

## Context

运行重构后的正式链路是：

```text
Simulation Fixed Tick
  -> Action Context
  -> finite Action Timeline
  -> Window / Motion / Warp / Cue / lifecycle
  -> committed raw visual sample

Presentation Tick
  -> Action Playback time projection
  -> AnimationClip pose sampling
  -> AnimationSlot
  -> Blend / Stored Pose / Inertialization
  -> Pose Graph
  -> Final Pose
```

本change不修改该链，只建立作者与调试工作面：

```text
Action Animation Workspace
  -> ActionProfile正式owner
  -> Gameplay Graph正式owner
  -> Timeline正式owner
  -> Presentation Profile正式owner
  -> Pose Graph正式owner
  -> RuntimeDebugSession正式Trace
```

## Goals

- 在一个窗口理解和编辑有限Action动画闭环。
- 保持每个字段只有一个正式owner。
- 明确区分Simulation raw time与Presentation visual time。
- 复用唯一Preview Runtime和Runtime Debug数据。
- 不影响Float32/Fixed Numeric Target。
- 不增加自动Build、自动分析或自动选中资产。

## Non-Goals

- 不新增Montage或Action Sequence运行时资产。
- 不新增Section、Jump、Seek、Pause、Resume或SetRate。
- 不改变Action Timeline或Action Playback ABI。
- 不改变每Fixed Tick committed raw sample合同。
- 不让Preview执行Gameplay。
- 不迁移任何角色资产。
- 不新增测试任务。

## Decision 1: Workspace不拥有作者数据

Workspace typed session必须解析：

```text
CharacterPipelineDefinition
ActionProfile
Action Context call site
finite Action Timeline
Action Animation producer
Presentation producer binding
AnimationSlot consumer
Runtime Debug binding
```

正式mutation路由：

```text
Identity与Action策略 -> ActionProfile
Action流程与退出     -> Gameplay Graph
时间内容             -> Timeline
资源、Rig与Analysis  -> Presentation Profile
Slot与Blend          -> Pose Graph / Policy
```

Workspace只保存窗口级selection和折叠状态，不保存业务字段镜像。

## Decision 2: 三种时间必须分离

### Action Logic Time

来自Simulation Action Timeline，用于：

- Window。
- Motion。
- Warp。
- Cue。
- Action完成与打断。
- committed raw visual sample。

### Projected Presentation Time

由Action Playback在相邻committed raw sample之间按Presentation Tick插值或受限外推，只用于视觉采样。

### Marker Effective Time

由Marker Sync从projected time映射得到，只改变source AnimationClip的表现采样。

Workspace必须分别显示三者及来源，不提供可写的统一`Playback Position`。

## Decision 3: Preview只运行表现链

Workspace Preview输入：

```text
Action Playback fixture
Base Pose fixture
AnimationSlot plan
Transition Routing plan
Pose Plan
Rig
Presentation delta
```

Preview可以生成：

- Action Pose。
- Slot transition。
- Blend Stack。
- Stored Pose。
- Inertialization。
- Final Pose。

Preview不能生成：

- Action admission。
- Gameplay Window真相。
- Motion request。
- Warp target decision。
- Gameplay Cue事实。
- Action lifecycle事实。

Timeline Gameplay内容需要独立正式Timeline Preview adapter时，Workspace只导航或嵌入该只读结果，不复制执行器。

## Decision 4: Numeric Target只属于Session和Build身份

Workspace authoring与表现Preview均为target-neutral。Live Debug可以显示当前Session是Float32或Fixed，但相同Presentation Contract必须映射到同一producer、AnimationSlot和Pose Plan。

Workspace不得：

- 保存NumericProfile。
- 保存Float32或Fixed raw state。
- 转换活动Session state。
- 为两个Target创建两套表现配置。

## Decision 5: 窗口布局

```text
Top Bar
  Definition | Action | Timeline | Preview | Live

Center
  Timeline Core

Right Details
  Identity
  Gameplay
  Animation
  Slot / Blend
  References

Bottom Dock
  Preview
  Live
  Diagnostics
```

Timeline Core继续独立拥有Track、Clip、TreeClip、Marker和Curve编辑。Character Workspace通过typed session提供领域上下文和导航。

## Decision 6: Build保持显式

Workspace可以提供导航到已有Dry Run和Build操作，或调用同一正式显式命令，但不得建立新的自动触发策略。

以下行为不得Build：

- 打开Workspace。
- 切换Action。
- Timeline或Graph mutation。
- selection变化。
- Preview播放。
- Live Debug连接。
- asset import。

## Failure Model

- 缺少精确Definition：拒绝完整session。
- Action没有唯一Action Context call site：显示歧义。
- Action没有唯一有限Timeline：显示缺失或重复关系。
- producer没有Presentation binding：禁用Animation Preview并定位owner。
- producer没有AnimationSlot consumer：禁用Slot Preview并定位Pose Graph。
- Projection或Trace revision不匹配：显示stale并停止关联。
- Preview模块缺失：停止Preview，不建立临时播放器。

## Migration

本change没有业务资产迁移。所有现有Action继续保留原正式owner。Workspace通过identity解析已存在关系，不能解析的关系作为正式作者错误显示，不创建fallback binding。
