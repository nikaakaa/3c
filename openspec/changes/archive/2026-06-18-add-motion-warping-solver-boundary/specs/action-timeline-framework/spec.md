## ADDED Requirements

### Requirement: Motion Clip 可声明 Warp Payload
ActionTimeline Motion clip MUST 能以纯数据声明可选 Motion Warping payload。该 payload MAY 包含 warp policy id、target binding id、motion profile id、compiled tick duration 或 motion window binding、axis mask、rotation policy、攻击吸附开关、转向修正开关；MUST NOT 持有 Unity scene object、Animancer runtime object、Animator、AnimationClip、CharacterController 或 MonoBehaviour runner。

#### Scenario: Motion clip 携带 warp payload
- **GIVEN** ActionTimeline 中存在 Motion clip
- **AND** 该 clip 配置了 warp policy id 和 target binding id
- **WHEN** runtime 构建 `ActionTimelineDefinition`
- **THEN** definition MUST 保存这些 warp 字段的纯数据形式
- **AND** definition MUST NOT 保存场景目标对象或表现层对象

#### Scenario: 未配置 warp 时保持普通 motion
- **GIVEN** ActionTimeline Motion clip 只配置 duration、distance 和 rotateToDirection 或等价普通 motion payload
- **WHEN** evaluator 命中该 clip
- **THEN** outcome MUST 继续输出普通 motion intent
- **AND** 现有 Dodge Directional / Backstep 行为 MUST 不因 warp payload 支持而改变

### Requirement: Timeline Evaluator 不解析 Warp Target
`ActionTimelineEvaluator` MUST 只以 action-local tick 评估当前命中的 Motion clip 并输出 motion intent。它 MUST NOT 解析 warp target、运行 Motion Warping solver、调用 motion executor、读取场景对象或写 runtime blackboard。

#### Scenario: Evaluator 只输出 intent
- **GIVEN** 当前 action-local tick 命中带 warp payload 的 Motion clip
- **WHEN** `ActionTimelineEvaluator` 评估 timeline
- **THEN** outcome MUST 包含对应 motion intent 和 warp payload
- **AND** evaluator MUST NOT 输出已经应用到角色根的 delta
- **AND** evaluator MUST NOT 读取目标 `Transform`

#### Scenario: Target 解析在后续 motion resolve
- **GIVEN** outcome 包含带 target binding id 的 motion intent
- **WHEN** Action submitter 或后续 motion resolve 阶段处理该 outcome
- **THEN** target binding MUST 在 motion resolve 边界解析为纯数据 target snapshot
- **AND** Timeline evaluator MUST 不参与 target provider 调用

#### Scenario: 攻击吸附与转向修正只作为 intent
- **GIVEN** 当前 action-local tick 命中带攻击吸附和转向修正 payload 的 Motion clip
- **WHEN** `ActionTimelineEvaluator` 评估 timeline
- **THEN** outcome MUST 只表达攻击吸附和转向修正 intent
- **AND** evaluator MUST NOT 计算 planar delta
- **AND** MUST NOT 计算 yaw delta

### Requirement: Warp Payload 校验
系统 MUST 对 ActionTimeline Motion clip 的 Motion Warping payload 提供校验。缺失必需 policy、target binding、profile 或非法 motion window / tick 区间 MUST 报告配置错误，runtime MUST NOT 通过隐藏默认值继续执行 warped motion。

#### Scenario: 必需 target binding 缺失
- **GIVEN** Motion clip 的 warp policy 要求 target
- **AND** clip 未配置 target binding id
- **WHEN** 运行 ActionTimeline 校验
- **THEN** 校验结果 MUST 报告错误
- **AND** runtime MUST NOT 使用默认 target 继续执行

#### Scenario: 非法 payload 不进入 solver
- **GIVEN** Motion clip 的 warp payload 校验失败
- **WHEN** runtime 构建或评估该 timeline
- **THEN** 系统 MUST 阻止该 warped motion 被送入 Motion Warping solver
- **AND** MUST 输出明确诊断或校验错误
