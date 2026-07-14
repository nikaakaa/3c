# Change: 删除 Root Motion 曲线的隐式默认求值模式

## Why

`RootMotionCurveEvaluationMode` 当前以 `FullLocalDelta = 0` 作为枚举零值，`RootMotionCurveAsset.evaluationMode` 也以它初始化。旧资产缺少字段、字段为零值或字段损坏时，求值器会把它们静默解释为完整本地位移。

这不是无害的展示默认值。求值模式决定动画派生数据是保留本地 XYZ 轨迹，还是只使用前向距离和 yaw；错误解释会让攻击、闪避或转身产生不可追溯的逻辑位移。

原提案的资产基线不正确。当前项目实际存在七个 `RootMotionCurveAsset`：

- `Corin/Pipeline/Motion/Curves` 下的 Attack1、Attack2、DodgeBack、DodgeForward、MovingTurn 五个曲线资产均为 `evaluationMode: 0`，其累计 XYZ 数据表达的是完整本地位移。
- `BakedAnimation` 下两个 TurnBack 输出缺少 `evaluationMode` 字段，且没有任何非 `.meta` 序列化引用。

同时，当前 Corin 的正式运行时位移来自 RootTree 内联 `MotionCurveClip`；`MotionCurveTrack` 不引用 `RootMotionCurveAsset`，`RootMotionCurveEvaluator` 也没有运行时调用者。独立曲线资产是离线烘焙和检查用的 authoring 数据，不是 Timeline 的第二条运行时位移来源。

## What Changes

- 将 `RootMotionCurveEvaluationMode` 的零值定义为无效的 `Unspecified`；`FullLocalDelta` 与 `ForwardDistanceYaw` 使用显式非零序列化值。
- 在 `RootMotionCurveAsset`、正式 Baker 和求值器中统一校验模式。未指定、缺失或未知模式都是配置错误，不能产生 sample 或 motion delta。
- 将五个 Corin `Pipeline/Motion/Curves` 曲线资产一次性显式迁移为 `FullLocalDelta`，保留其现有本地 XYZ 与 yaw 语义。
- 删除两个无引用、缺少模式字段的 `BakedAnimation` TurnBack 曲线输出及其 meta，不为它们保留迁移、读取或自动升级路径。
- Root Motion Baker 的初始模式改为未指定；作者必须选择有效模式后才能烘焙，Baker 不再通过默认分支写入完整本地位移。
- `RootMotionCurveEvaluator` 只通过显式模式分支求值，删除“非 `ForwardDistanceYaw` 即 `FullLocalDelta`”语义。
- 明确数据边界：`RootMotionCurveAsset` 不会自动进入 Timeline；Timeline 仍只从自己的内联 `MotionCurveClip` 提交 motion contribution。本 change 不新增资产到 Timeline 的导入器、隐式复制、运行时引用或 fallback。
- 删除 current spec 中“旧曲线资产默认按完整本地位移解释”的兼容 requirement，并补充显式模式与 Timeline 边界合同。

## Impact

- 影响 `RootMotionCurveAsset`、`RootMotionCurveEvaluator` 和 Root Motion Baker。
- 影响七个已存在的曲线资产：保留五个正式 Corin authoring 输出，删除两个失效且无引用的旧输出。
- 不改变 Corin 当前 Timeline 的内联 `MotionCurveClip` 数据，也不让这五个资产在运行时突然接管角色位移。
- 影响 `character-root-motion-curves` current spec。
- 不新增 fallback、legacy reader、自动写回、一次性迁移工具、命名查找或并行曲线格式。

## Current Spec Comparison

current `character-root-motion-curves` 已在实施阶段同步为本 change 的目标语义：旧零值兼容 requirement 已删除，`Unspecified` 已明确为配置错误，并已加入 `RootMotionCurveAsset` 与 Timeline 内联 `MotionCurveClip` 的单向边界。归档时应保留本 delta 作为历史来源，但不再向 current spec 重复应用。

原 delta 还假定 `Timeline MotionCurveTrack` 会直接引用 `RootMotionCurveAsset`。现有 `MotionCurveClip` 没有这种引用，当前 Corin Runtime 也只采样内联曲线。因此本 change 删除这项不符合现状的断言；若后续需要“烘焙资产一键转写 Timeline 内联曲线”，必须单独规划一条明确的 authoring 导入链，而不能由运行时推断或双写。
