## MODIFIED Requirements

### Requirement: Graph Data Catalog 必须编辑 Blackboard fact projection

唯一 Graph Data Catalog MUST在本地 Blackboard declaration 的展开详情中提供可选 fact projection 编辑。Projection 默认 MUST为 None；ActionWindow projection MUST只保存 WindowType、WindowId 和 Digest，并 MUST清楚显示其要求的 Bool、Frame/Frame、SyncFact 与显式 Action Context provenance 约束。继承 declaration MUST只读显示 projection 并 MAY提供定位 owner；系统 MUST NOT新增独立 Window panel、Window asset editor、TreeClip 内的第二份 projection 配置或 active-window registry。完整网络策略 MUST由当前 Network Model profile 按稳定 ActionId 解析，Catalog、Blackboard declaration 和 ActionProfile MUST NOT保存或编辑该策略。

#### Scenario: 配置攻击命中窗口变量

- **WHEN**作者展开 owner-local `HitWindow` declaration 并选择 ActionWindow projection
- **THEN** Catalog MUST允许编辑稳定 WindowType、WindowId 和 Digest
- **AND**完整网络策略 MUST保持位于当前 Network Model profile

#### Scenario: 配置动作恢复窗口

- **WHEN**作者展开 inline Timeline owner 下的 `RecoveryOpen` declaration
- **THEN** Catalog MUST显示其 ActionWindow projection、Action Context provenance 要求与真实 owner
- **AND**作者 MUST能从该条目定位使用同一 WindowType 的纯条件查询

#### Scenario: 配置普通本地状态门

- **WHEN**作者展开不属于动作窗口的 owner-local Bool Frame declaration
- **THEN**作者 MUST能保持 Projection=None
- **AND**该 variable 的 true 值 MUST NOT产生 `ActionWindowFact` 或被 `ActionWindowActiveInfoNode` 匹配

#### Scenario: 非法 projection

- **WHEN**作者为非 Bool、非 Frame/Frame、非 SyncFact 或缺失 Action Context provenance 的 declaration 选择 ActionWindow projection
- **THEN** Catalog 与 Validator MUST报告非法组合
- **AND**系统 MUST NOT静默修改 scope、lifetime、类型、projection 或 owner
