# 移动 TurnBack Root Motion 任务

## 1. 删除旧转身运行链路
- [x] 1.1 删除 `TurnInPlace` 状态 ID、默认节点和 transition
- [x] 1.2 删除 `HasTurnInPlacePlan` 和 `TurnInPlaceCanExit` 条件
- [x] 1.3 删除状态机 frame/restore 中的 `TurnInPlacePlan`
- [x] 1.4 删除 runtime blackboard 中的 `TurnInPlaceFacts` 和 `TurnInPlacePlan`
- [x] 1.5 删除 `BasicLocomotionFrame` 中的 TurnInPlace/MovingPivot plan
- [x] 1.6 删除 `MovementAnimationContext` 中的 TurnInPlace/MovingPivot plan
- [x] 1.7 删除 `PlayerLocomotionController` 中的 MovingPivot plan 生命周期
- [x] 1.8 删除 `ResolveMovingPivotTurnMotionFacts`
- [x] 1.9 删除 Presenter 中的 TurnInPlace/MovingPivot alias 分支
- [x] 1.10 删除 rollback state 中的 MovingPivot 私有状态

## 2. 删除旧类型、工具和资源
- [x] 2.1 删除 `TurnInPlaceTypes.cs`
- [x] 2.2 删除 `MovingPivotTurnTypes.cs`
- [x] 2.3 删除 TurnInPlace animation config/entry/selector/context
- [x] 2.4 删除 MovingPivot animation config/entry/selector
- [x] 2.5 删除旧 `MovingPivotTurnTests`
- [x] 2.6 删除旧 TurnInPlace setup editor utility
- [x] 2.7 删除旧 TurnBack bake trigger
- [x] 2.8 删除旧 TurnInPlace/MovingPivot config asset
- [x] 2.9 保留 TurnBack animation clip 和 TransitionAsset 资源

## 3. 修正测试和诊断
- [x] 3.1 更新默认状态机测试为不包含 `FullBody/Locomotion/TurnInPlace`
- [x] 3.2 删除旧 TurnInPlace selector/state/setup 测试
- [x] 3.3 更新 character config root 测试
- [x] 3.4 更新 rollback runtime state 构造调用
- [x] 3.5 将旧 moving-pivot executor 诊断改为通用 animation-motion 诊断
- [x] 3.6 运行 Unity refresh 编译
- [x] 3.7 运行受影响 EditMode 测试
- [x] 3.8 读取 Console 确认 error 为 0

## 4. 后续 Root Motion TurnBack
- [x] 4.1 参考 `Ref/zzzdemo-source-code` 的 Sprinting/ReturnRun 状态边界
- [x] 4.2 新增移动 TurnBack 逻辑状态
- [x] 4.3 在 TurnBack 状态播放 `Locomotion.Turn.Back`
- [x] 4.4 TurnBack 窗口内禁止普通输入旋转
- [x] 4.5 TurnBack 窗口内禁止普通输入平面位移
- [x] 4.6 收集 Animator/Animancer root motion delta
- [x] 4.7 通过统一 motion executor 应用 root motion delta
- [x] 4.8 增加 TurnBack 状态和 root motion focused tests
- [x] 4.9 将真实 Corin Animancer `Locomotion.Turn.Back` transition 绑定到 root motion clip
- [x] 4.10 增加真实 TransitionLibrary 资源级测试，确认 TurnBack alias 指向 root motion clip
- [x] 4.11 运行受影响 EditMode 测试
- [x] 4.12 读取 Console 确认 error 为 0
- [x] 4.13 给用户 Sandbox 验证步骤
