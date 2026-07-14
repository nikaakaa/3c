## MODIFIED Requirements

### Requirement: ActionProfile Inspector 必须是策略主编辑入口

`ActionProfile` Inspector MUST 是 gameplay 动作定义主入口，按 Identity、Tags、Block/Cancel、Target 和 Debug 分区展示。它 MUST 不编辑任何具体 Network Model 的 prediction、authority、replication、window/motion/cue/result 网络策略，也 MUST 不提供 packet preview。模型策略 MUST 由对应 model profile Inspector 编辑。

#### Scenario: 编辑 Attack ActionProfile

- **WHEN** 作者选中 Attack ActionProfile
- **THEN** UI MUST 展示动作身份与 gameplay 约束
- **AND** MUST 提供到已绑定 model policy 的只读导航或缺失提示，而不是内联编辑网络字段

### Requirement: 作者 UI 必须能从 ActionProfile 追到输出预览

作者 MUST 能从 ActionProfile、Graph request、TreeClip projection 和 Runtime Debug 追踪 ActionId/ActionInstanceId 与 gameplay outputs。网络 packet preview MUST 只出现在显式选择的 model profile/Debug 中，并通过稳定 ActionId 关联；ActionProfile MUST 不持有 expected packet 配置。

#### Scenario: 从 TreeClip 查看 HitWindow

- **WHEN** 作者查看 Attack HitWindow projection
- **THEN** UI MUST 显示 WindowType、WindowId 和 Action identity
- **AND** MAY 导航到 ServerAuthoritative model policy 的只读匹配结果
- **AND** MUST 不把该 model policy 复制到 TreeClip

### Requirement: 非 Timeline 输出必须共享同一套策略解析

Timeline 与非 Timeline 动作 MUST 继续产生相同 gameplay facts，并通过 ActionId/ActionInstanceId 关联。具体网络策略 MUST 由当前 model profile/resolver 统一解析；ActionProfile、Node 和 Blackboard declaration MUST 不成为第二 policy 来源。

#### Scenario: 非 Timeline GuardWindow

- **WHEN** 非 Timeline 动作产生 GuardWindow fact
- **THEN** fact MUST 使用正式 Action Context
- **AND** ServerAuthoritative adapter MUST 从 model Action policy 解析网络行为

### Requirement: TreeClip 与 Scope Variable 必须是 Timeline Window 唯一作者入口

Decision TreeClip 与 Bool Frame scope variable MUST 继续作为 Timeline Window 唯一时间作者入口。Projection MUST 只保存 WindowType、WindowId 和 Digest gameplay fact 声明；authority、history、replication 和 packet policy MUST 来自当前 Network Model profile，不得保存在 ActionProfile、TreeClip 或 declaration。

#### Scenario: Attack HitWindow

- **WHEN** TreeClip 在本 tick 写入 HitWindow declaration
- **THEN** projection MUST 生成对应 ActionWindow fact
- **AND** ServerAuthoritative model policy MUST 决定是否进入 history/packet

