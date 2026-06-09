# Design: 基础移动动画参数可配置

## Context

当前基础移动链路已经把输入、移动意图、状态机、运动命令和动画外观层分开。`LocomotionAnimationSetSO` 负责将 `BasicMovementPhase + gait` 映射到 Animancer key，`BasicLocomotionStateMachine` 负责四阶段流转。当前不足在于动画参数不完整，尤其 `MoveStop` 回到 `Idle` 的时机不应长期依赖一个通用 `MoveStopMinTime`，而应能由 `WalkEnd / RunEnd` 的动画配置提供。

后续系统会扩展到多层动画、动作窗口、IK、预测回滚和编辑器。本次变更只建立“动画参数作为纯数据被逻辑层读取”的最小闭环。

## Goals

- 动画配置 entry 能表达 key、fade、speed、normalized start time、exit duration。
- 逻辑层读取解析后的停止退出时长，不读取 Animancer runtime。
- `MoveStop` 无输入时等待当前停止动画退出时长后回 `Idle`。
- `MoveStop` 中出现输入时立即切到 `MoveStart`。
- 保持状态机和 pipeline 不依赖 Animancer、Camera、CharacterController、KCC 或 Input System。
- 保持动画外观层不写位移、不切业务状态。

## Non-Goals

- 不引入完整动作状态系统。
- 不引入多层 layer 冲突规则。
- 不引入 IK 命令、动作 timeline 或编辑器。
- 不引入网络预测回滚。
- 不使用 Root Motion 接管基础位移。

## Proposed Shape

### Animation Entry

基础移动动画集从简单字段升级为 entry：

```text
LocomotionAnimationEntry
  Key
  FadeDuration
  Speed
  NormalizedStartTime
  ExitDurationMode
  ExitDurationOverride
```

第一版 `ExitDurationMode` 仅需要：

```text
Manual
FallbackToMovementConfig
```

如果后续要自动从 `AnimationClip.length` 推导，应单独引入 Clip 引用、AssetDatabase 编辑器解析或运行时 Transition 解析。当前不让逻辑层为了时长去碰 Animancer。

### Resolved Timing

配置层提供解析后的纯数据：

```text
LocomotionAnimationTiming
  ExitDuration
```

状态机只消费这个纯数据，不关心 key 是 `RunEnd` 还是 `WalkEnd`。

### Stop Duration Source

现有 `BasicMovementSettings.MoveStopMinTime` 作为兼容 fallback 保留。新增的停止动画退出时长进入状态机上下文，例如：

```text
BasicMovementSettings
  MoveStopMinTime
  MoveStopExitDuration
```

或等价方式：

```text
LocomotionStateGraphContext
  StopExitDuration
```

具体实现可按代码最小改动选择，但必须保持 `LocomotionStateGraphConditionEvaluator` 只读纯数据。

### RunEnd / WalkEnd 行为

```text
MoveLoop + NoMoveIntent
  -> MoveStop

MoveStop + NoMoveIntent + PhaseTime < StopExitDuration
  -> 保持 MoveStop

MoveStop + NoMoveIntent + PhaseTime >= StopExitDuration
  -> Idle

MoveStop + HasMoveIntent
  -> MoveStart
```

`MoveStop -> MoveStart` 继续高优先级，不能被 stop exit duration 阻塞。

## Alternatives Considered

### 直接查询 Animancer 当前播放状态

不采用。这样会让状态机依赖动画 runtime，测试、预测回滚和后续服务器侧模拟都会变困难。

### 用 AnimationClip.length 自动推导

暂不作为第一版要求。自动推导需要 Clip 引用或编辑器烘焙，可能扩大资产迁移范围。本次先支持手动时长和 fallback，后续可以加自动校验/烘焙。

### 把 RunEnd 做成逻辑状态

不采用。`RunEnd` 是动画 key，不是基础移动逻辑阶段。逻辑层仍保持 `MoveStop`，具体播放 `WalkEnd / RunEnd` 是动画映射结果。

## Validation Strategy

- EditMode 测试覆盖动画 entry 默认解析。
- EditMode 测试覆盖 `RunEnd` stop exit duration 使 `MoveStop` 到时回 `Idle`。
- EditMode 测试覆盖 `WalkEnd` stop exit duration。
- EditMode 测试覆盖 `MoveStop` 中有输入立即切到 `MoveStart`。
- EditMode 测试覆盖动画外观层不因相同 entry 每帧重播。
- 静态搜索验证状态机不引用 Animancer、CharacterController、KCC、Camera、Input System。
- 手动验证当前演示角色急停完整进入 Idle，急停中重新输入立即起步。

## Future Extensions

- 为 entry 增加 Clip 引用和自动 `Clip.length / Speed` 推导。
- 将 entry 数据推广到动作状态配置。
- 引入 FullBody / UpperBody / LowerBody / Additive / Weapon 层。
- 引入 cancel、hitbox、motion、IK、VFX、SFX 等窗口。
- 引入可同步动画状态快照和事件去重。
- 在数据稳定后实现轻量窗口编辑器和 Timeline 编辑器。
