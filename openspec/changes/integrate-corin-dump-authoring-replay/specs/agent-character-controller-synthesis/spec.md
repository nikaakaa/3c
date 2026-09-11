## ADDED Requirements

### Requirement: Corin Document必须携带来源和闭包身份

CharacterController v7 Document中的Corin editable与context目标 MUST 保留正式作者owner、来源manifest identity、稳定资产identity和闭包引用关系。Document MUST 不把外部Dump路径、生成Program或运行时Dump当作可写作者字段。

#### Scenario: Corin Document来源核对

- **WHEN** Agent checkout或apply Corin Definition
- **THEN** 系统 MUST 验证来源identity、owner和完整闭包与当前Definition一致
- **AND** 不能解析的来源或跨角色引用 MUST 拒绝Document事务
