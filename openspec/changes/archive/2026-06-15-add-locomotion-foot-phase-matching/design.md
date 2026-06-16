## Context
当前项目已经有统一状态机、动画播放进度事实、运行时黑板、TurnBack 状态、TurnBack motion policy 和 tick sampled motion 相关 proposal。现有缺口集中在动画表现相位：TurnBack 退出时只有 normalized time，没有语义脚相位；RunLoop 入场时也没有按脚相位选择 normalized start time。

## Goals / Non-Goals
- Goals:
  - 用纯数据 profile 表达 locomotion clip 的脚相位 marker。
  - 从当前播放进度采样脚相位事实，并在 TurnBack 退出时保留退出脚相位。
  - 让 RunLoop 新进入时消费脚相位匹配结果，只设置一次起播 normalized time。
  - 保持黑板 snapshot/restore 和预测回放可恢复。
  - 为配置缺失提供明确校验或诊断，不做静默 fallback。
- Non-Goals:
  - 不实现 IK foot lock。
  - 不实现 Motion Matching。
  - 不拆 TurnBack_Left / TurnBack_Right 或 RunLoop_Left / RunLoop_Right 变体。
  - 不让 Movement executor 理解左右脚。
  - 不改变 TurnBack 的进入、位移、旋转和 motion source 权威。

## Decisions
- Decision: 新能力命名为 `Locomotion Foot Phase Matching`。
  - Reason: 它描述的是动画相位匹配，不是位移系统、状态机系统或 IK 系统。

- Decision: 脚相位 profile 是运行时纯数据配置，至少支持 `Unknown / LeftPlant / RightPlant / LeftPass / RightPass`。
  - Reason: 第一版只需要支撑脚对齐，保留通过 pass 相位扩展的空间。

- Decision: 缺失或无效 foot phase profile 不自动使用 `0`、`0.5` 或 TransitionAsset 的 `_NormalizedStartTime` 作为相位匹配结果。
  - Reason: 项目约束要求正式配置，不做 fallback 配置。普通动画播放可以保持现状，但相位匹配能力必须显式失效并诊断。

- Decision: 状态机不计算脚相位，只消费黑板或输出帧中的纯数据相位事实。
  - Reason: 状态机保持逻辑权威，脚相位属于动画语义。

- Decision: Presenter 只在“播放 key 发生变化并新进入 RunLoop”时应用 start normalized time override。
  - Reason: 每帧重设 normalized time 会卡住或破坏动画播放。

## Data Flow
1. `BasicLocomotionAnimancerPresenter` 暴露当前 locomotion 播放进度。
2. 动画 facts adapter 使用当前 alias、phase、normalized time 和 `LocomotionFootPhaseProfile` 采样 `CurrentLocomotionFootPhase`。
3. 当当前 phase 为 `TurnBack` 且即将退出到 `MoveLoop + Run` 时，系统记录 `LastLocomotionExitFootPhase`。
4. 构建下一帧 `MovementAnimationContext` 时携带 `DesiredEntryFootPhase` 或已解析的 `StartNormalizedTimeOverride`。
5. Presenter 播放 `RunLoop` 时按匹配结果设置一次 `AnimancerState.NormalizedTime`。
6. 黑板 snapshot/restore 保存当前相位和退出相位，回放后得到一致入场决策。

## Boundaries
- `FootPhaseProfile` 和 sampler 不读取 Animancer runtime、AnimationClip、Transform、CharacterController 或 InputAction。
- `BasicLocomotionAnimancerPresenter` 可以设置播放起始时间，但不能决定逻辑状态，也不能写黑板。
- `PlayerLocomotionController` 或等价 adapter 只负责组装上下文和应用 animation facts，不直接计算脚相位曲线。
- Movement executor 不读取脚相位，不根据左右脚改变运动命令。
- 任何需要通过 IK 修脚、添加动画变体或新增 Motion Matching 的方案必须另走 proposal。

## Risks / Trade-offs
- Risk: 手工 marker 配错会导致相位匹配更差。
  - Mitigation: 增加配置校验、EditMode 测试和手动验证日志。
- Risk: 全局修改 RunLoop 起播会影响非 TurnBack 进入 RunLoop。
  - Mitigation: override 只对携带匹配请求的新进入 RunLoop 生效。
- Risk: active changes 中已有 TurnBack motion source 和 EntryLocal 改动。
  - Mitigation: 本变更只扩展脚相位事实链，不重定义 motion source 或 TurnBack 进入路径。

## Open Questions
- Corin 当前 Generic 和 Humanoid 的 TurnBack/RunLoop 是否都要第一版配置 marker，还是第一版只配置当前 Sandbox 使用的 Generic 路径？
- TurnBack 退出 marker 初始建议值是否采用 `0.92` 作为第一轮调参基线？

