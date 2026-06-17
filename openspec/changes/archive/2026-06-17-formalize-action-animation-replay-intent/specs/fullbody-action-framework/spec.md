## ADDED Requirements

### Requirement: Action 生命周期输出播放实例身份
FullBody Action 生命周期 MUST 为每个 accepted Action 实例提供纯数据播放实例身份，并将其传递到 Action 动画请求。该身份 MUST 在同一个 active Action 生命周期内保持稳定，在新的 accepted Action 实例进入时变化。播放实例身份 MUST NOT 依赖 Unity scene object、Animancer runtime object、AnimationClip、TransitionAsset、Animator 或当前视觉播放状态。

#### Scenario: 新 accepted Action 创建新播放身份
- **GIVEN** 当前 active action 为 `Action.Dodge`
- **AND** 新的 Dodge 请求通过 Action 仲裁并被 accepted
- **WHEN** Action lifecycle 应用该 accepted action
- **THEN** lifecycle MUST 产生不同于上一段 active action 的播放实例身份
- **AND** 即使两段动作使用同一个 `ActionAnimationKey`，输出的 Action 动画请求也 MUST 能表达新播放意图

#### Scenario: Active Action 后续帧复用播放身份
- **GIVEN** 当前 active action 已经拥有播放实例身份 `A`
- **AND** 本帧没有新的 accepted Action 覆盖它
- **WHEN** Action lifecycle 输出本帧 animation request
- **THEN** 输出 MUST 继续使用播放实例身份 `A`
- **AND** MUST NOT 只因为当前 frame source step 变化而生成新播放身份

#### Scenario: Restore 后播放身份保持可重建
- **GIVEN** snapshot restore 后仍处于某个 active Action
- **WHEN** restore 后下一帧输出 Action animation request
- **THEN** 输出的播放实例身份 MUST 与恢复的 active Action 对应
- **AND** 同一次播放的 restore resume MUST 不被误判为新 accepted Action

#### Scenario: 播放身份保持纯数据边界
- **WHEN** 开发者检查 Action lifecycle restore state、animation request 和 output request
- **THEN** 播放实例身份 MUST 是值类型或等价纯数据
- **AND** MUST NOT 保存 `MonoBehaviour`、`AnimancerState`、`Animator`、`AnimationClip`、`Transform` 或输入系统 runtime object

### Requirement: Action 输出阶段只转交播放意图
FullBody output runtime MUST 只把 Action lifecycle 已解析出的 animation key 和播放实例身份转交给统一 animation presenter。Output runtime MUST NOT 自行重新判断 Dodge 是否连续、重新生成播放实例身份、调用 Action 仲裁或创建备用播放路径。

#### Scenario: Output runtime 不重新生成身份
- **GIVEN** Action lifecycle frame 包含 animation key 和播放实例身份
- **WHEN** FullBody output runtime 执行动画 presentation 阶段
- **THEN** runtime MUST 将该请求原样转交给正式 animation presenter 接口
- **AND** MUST NOT 基于当前 Presenter key、normalized time 或 Unity frame count 重新生成播放身份

#### Scenario: 没有第二条动画出口
- **WHEN** 同 key 连续 Dodge 需要重播动画
- **THEN** 系统 MUST 通过现有 Character frame output 和正式 Presenter 接口完成播放
- **AND** MUST NOT 新增 Dodge 专用 presenter、fallback presenter、直接 Animancer 调用或绕过 output runtime 的播放路径
