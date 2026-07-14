## ADDED Requirements

### Requirement: Graph Data Catalog 必须编辑 Blackboard fact projection

唯一 Graph Data Catalog MUST 在本地 Blackboard declaration 的展开详情中提供可选 fact projection 编辑。Projection 默认 MUST 为 None；本 change 的 ActionWindow projection MUST 只保存 WindowType、WindowId 和 Digest，并 MUST 清楚显示其要求的 Bool、Frame/Frame 和 SyncFact 约束。继承 declaration MUST 只读显示 projection 并 MAY 提供定位 owner；系统 MUST NOT 新增独立 Window panel、Window asset editor 或 TreeClip 内的第二份 projection 配置。

#### Scenario: 配置攻击命中窗口变量

- **WHEN** 作者展开 `Attack1Hit` declaration 并选择 ActionWindow projection
- **THEN** Catalog MUST 允许编辑稳定 WindowType、WindowId 和 Digest
- **AND** MUST 保持完整网络 policy 位于 ActionProfile

#### Scenario: 配置本地状态门

- **WHEN** 作者展开 `CanDodgeMoveCancel` declaration
- **THEN** 作者 MUST 能保持 Projection=None
- **AND** 该 variable 的 true 值 MUST NOT 产生 ActionWindowSample

#### Scenario: 非法 projection

- **WHEN** 作者为非 Bool、非 Frame/Frame 或非 SyncFact declaration 选择 ActionWindow projection
- **THEN** Catalog 与 Validator MUST 报告非法组合
- **AND** 系统 MUST NOT 静默修改 scope、lifetime、类型或 projection

