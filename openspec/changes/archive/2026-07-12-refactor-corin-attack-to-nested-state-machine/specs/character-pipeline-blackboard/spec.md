## ADDED Requirements

### Requirement: 嵌套状态机必须按 declaration owner 解析 State activation frame

Pipeline Blackboard access context MUST 携带完整 StateMachine execution path。读取或写入 State scope declaration 时，resolver MUST 根据 declaration owner 和 Graph ownership 选择唯一对应 activation frame，而不是始终使用最内层 frame。找不到或找到多个候选 frame MUST 作为配置/runtime 错误，MUST NOT fallback 到 Character、Graph 或栈顶 State scope。

#### Scenario: 外层 Attack 状态变量跨连段保持

- **WHEN** declaration 归属外层 Attack State body
- **AND** 内层状态从 Attack1 切换到 Attack2
- **THEN** 该 declaration MUST 继续绑定外层 Attack activation bucket
- **AND** Attack1 exit MUST NOT 清理该值
- **AND** 外层 Attack exit MUST 清理该值

#### Scenario: Attack1 局部状态变量退出清理

- **WHEN** declaration 归属 Attack1 State body
- **AND** Attack1 退出到 Attack2
- **THEN** runtime MUST 只清理 Attack1 activation bucket
- **AND** 外层 Attack bucket 与 Attack2 bucket MUST 保持独立

#### Scenario: 内层引用外层 declaration

- **WHEN** Attack2 ConditionRuleGraph 显式引用外层 Attack body declaration
- **THEN** resolver MUST 使用 declaration owner 定位外层 Attack frame
- **AND** 系统 MUST NOT 复制同 key declaration 到 Attack2 graph
- **AND** 系统 MUST NOT 按最近 key 隐式 shadow

#### Scenario: declaration owner 不在 execution path

- **WHEN** State declaration reference 的 owner 不对应当前 execution path 中任何 frame
- **THEN** access MUST 失败并报告 owner/path 断裂
- **AND** Compare、And、Or 或 lifecycle 节点 MUST NOT 获得默认值继续执行
