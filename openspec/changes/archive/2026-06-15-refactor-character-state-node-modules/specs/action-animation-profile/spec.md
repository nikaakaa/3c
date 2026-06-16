## ADDED Requirements
### Requirement: 动作动画模块只保存稳定语义 key
系统 MUST 允许动作状态节点通过动作动画模块保存稳定 animation key 或 timeline binding key，用于产出动作动画请求。具体 Clip、TransitionAsset、fade、speed、start time 和 Animancer runtime state MUST 继续归属 Action Animation Profile、Animancer TransitionLibrary 或等价表现配置入口。

#### Scenario: Dodge 变体输出动作动画 key
- **WHEN** `Dodge` 节点的 Directional 变体进入
- **THEN** 动作动画模块 MUST 产出 `Action.Dodge.Directional` 或等价稳定 key
- **AND** 动作动画 Presenter MUST 只消费该 key 对应的播放请求
- **AND** 状态节点 MUST NOT 保存具体 AnimationClip 或 TransitionAsset

#### Scenario: 连续 Dodge 仍重播同 key
- **WHEN** `Dodge -> Dodge` transition 进入同一动作动画 key
- **THEN** 动作动画模块 MUST 再次产出动作动画请求
- **AND** Presenter MUST 将其视为新的播放意图
- **AND** 该行为 MUST NOT 依赖新建第二播放路径
