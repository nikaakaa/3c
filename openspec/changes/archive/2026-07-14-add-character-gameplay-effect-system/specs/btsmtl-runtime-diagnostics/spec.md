## MODIFIED Requirements

### Requirement: Trace channel 必须控制调试采集成本

系统 MUST 至少提供 Graph、StateMachine、Timeline、Blackboard、Animation、Motion 和 GameplayEffect channel。未被 Live interest 或显式 Capture 请求的 channel MUST 阻止其非必要 payload 构造、source handle 解析和 diagnostics 写入，并且 MUST NOT 改变 runtime 执行结果。

#### Scenario: 关闭 Animation channel

- **WHEN** 当前 Debug Session 未启用 Animation channel
- **THEN** runtime MUST NOT 构建 Animation trace payload
- **AND** CharacterAnimationPlaybackCommandQueue、AnimationPlaybackLifecycle 和 AnimancerPlaybackAdapter MUST 继续产生相同正式结果

#### Scenario: 记录 Blackboard 值

- **WHEN** Blackboard channel 启用且变量发生正式写入或清理
- **THEN** Trace MUST 使用受限结构化 debug value snapshot
- **AND** Trace MUST NOT 持有任意 gameplay object reference 或调用未知对象逻辑作为序列化 fallback

#### Scenario: 关闭 GameplayEffect channel

- **WHEN** 当前 Debug Session 未启用 GameplayEffect channel
- **THEN** runtime MUST NOT 构建 tag、attribute、effect lifecycle 或 prediction journal trace payload
- **AND** Gameplay Effect MUST 继续产生相同 tag、attribute、effect 和 sync fact 结果

#### Scenario: 记录 Effect 生命周期

- **WHEN** GameplayEffect channel 启用且 effect 被应用、叠层、抑制、到期或移除
- **THEN** Trace MUST 使用稳定 effect identity、instance identity、context、logic tick 和结构化结果
- **AND** Trace MUST NOT 持有 Effect asset、component asset 或 active runtime object reference
