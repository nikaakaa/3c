## MODIFIED Requirements

### Requirement: Character Gameplay Effect Adapter 必须保持薄翻译边界

`CharacterGameplayEffectAdapter` MUST 只编排 `CharacterGameplayEffectInputMapper`、`GameplayEffectRuntime`、窄端口委托以及 Fact、Cue、Trace Projector。Effect/Attribute/Tag 规则、叠层、周期、聚合和 prediction journal MUST 位于 Gameplay 模块；ServerAuthoritative policy、packet、history 和 endpoint MUST 位于模型模块。Adapter MUST NOT 按 EffectId 编写业务 switch，MUST NOT 直接修改 Runtime Container，MUST NOT 使用全局事件总线或 ServiceLocator，MUST NOT 保存一个脱离当前 Tick 消费链的 `LastChangeSet`。

#### Scenario: 收到权威 Effect 输入

- **WHEN** Character semantic input 包含一条 Confirmed `GameplayEffectLifecycleFact`
- **THEN** `CharacterGameplayEffectInputMapper` MUST 把它转换为通用 authority input 并交给 Runtime
- **AND** Adapter MUST NOT 自己创建 ActiveEffect、Modifier 或 Tag source

#### Scenario: GE 产生状态变化

- **WHEN** GameplayEffectRuntime 返回当前 Tick ChangeSet
- **THEN** Adapter MUST 只 drain 一次并在同一 Commit 调用中交给 Fact、Cue 和 Trace Projector
- **AND** GameplayEffectRuntime MUST NOT 直接访问 CharacterPipelineOutput、Presentation 或 Diagnostics
- **AND** Adapter MUST NOT 把 ChangeSet 缓存到下一 Tick 等待未知消费者

### Requirement: BTSMTL 必须通过 CharacterGraphContext 使用 Gameplay Effect

`CharacterGraphContext` MUST 只暴露不可变 Gameplay Effect graph ports，由其提供只读 Tag Reader、Attribute Reader 和受控 Effect Command Sink。Condition、Decision 和 Value 节点 MUST 保持只读；Apply、Remove 等命令节点 MAY 提交以显式 EffectId、target 和 context 为输入的同步 mutation。系统 MUST NOT 让节点持有 CharacterGameplayEffectAdapter、GameplayEffectRuntime、AuthorityInputSink、active effect collection 或 prediction journal。

#### Scenario: Transition 读取硬直标签

- **WHEN** Condition Rule Graph 使用 `HasTag(State.HitReact.Stagger)`
- **THEN** 节点 MUST 通过 graph ports 的 `IGameplayTagReader` 从当前角色统一 GE 状态读取
- **AND** 节点 MUST NOT 从 Blackboard 或 ActionRuntime 私有集合读取另一份状态

#### Scenario: Graph 对自身应用消耗效果

- **WHEN** 命令节点提交一个以自身为 target 的 Instant stamina cost effect
- **THEN** 节点 MUST 只调用 graph ports 的 `IGameplayEffectCommandSink`
- **AND** 后续同 Tick Graph 读取 MUST 看到已提交的属性结果
- **AND** 节点 MUST NOT 获得网络模型或 AuthorityInput 能力

### Requirement: Gameplay Effect 输出必须进入统一事实与表现边界

GameplayEffectRuntime MUST 把 effect apply/remove/inhibit、attribute change、tag change 和 cue request 写入唯一 `GameplayEffectChangeSet`。Character 接入层 MUST 由 `CharacterGameplayEffectFactProjector`、`CharacterGameplayCueProjector` 和 `CharacterGameplayEffectTraceProjector` 分别把同一 ChangeSet 投影为 `CharacterPipelineOutput` 正式事实、`GameplayCueFact` 和 diagnostics。模型层事实 MUST 使用 model-neutral 类型；系统 MUST 删除 `ActionCueEvent` 的专用类型和链路，不得同时保留 action cue 与 gameplay cue 两套事实。

#### Scenario: 属性发生正式变化

- **WHEN** active effect 使 Health current value 变化
- **THEN** FactProjector MUST 生成携带 AttributeId、old/new value、ValueRevision、context、cause EffectId/BehaviorId 和 logic tick 的属性事实
- **AND** NetworkSend 与 diagnostics MUST 消费同一事实来源

#### Scenario: 效果触发命中特效

- **WHEN** effect component 产生一个需要表现的 cue
- **THEN** CueProjector MUST 生成正式 `GameplayCueFact`
- **AND** PresentationStage MUST 从该事实消费表现
- **AND** effect runtime MUST NOT 直接调用 Animancer、VFX、Audio 或场景组件
