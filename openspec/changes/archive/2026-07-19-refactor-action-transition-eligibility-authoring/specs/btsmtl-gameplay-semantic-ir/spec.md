## ADDED Requirements

### Requirement: 动作切换纯查询 operation 必须保持 Numeric Target 对等

Gameplay Semantic IR MUST以 numeric-neutral operation 表达 `ActionWindowActive` 与 `CanActivateAction`。每个 operation MUST由唯一 Authoring Discovery/Emitter 定义，保存 typed WindowType 或稳定 ActionProfile identity、source identity 和 capability requirement。Float32 与 Fixed Target MUST从同一 IR lowering，并通过各自窄状态端口调用同一 staged window query 与 portable Action admission 语义；系统 MUST不创建 Float32/Fixed 专用 authoring node 或复制业务 evaluator。

#### Scenario: 编译 ActionWindowActive

- **WHEN** ConditionRuleGraph 包含 `ActionWindowActiveInfoNode(RecoveryEarly)`
- **THEN** Frontend MUST生成一个 numeric-neutral `ActionWindowActive` operation
- **AND** Float32 与 Fixed Program MUST保留相同 WindowType 与 source identity

#### Scenario: 编译 CanActivateAction

- **WHEN** ConditionRuleGraph 包含指向 Dodge ActionProfile 的 `CanActivateActionInfoNode`
- **THEN** Frontend MUST保存稳定 ActionProfile identity，而不是 Unity object 或 profile display name
- **AND**每个 Target MUST从自己的 compiled Action catalog 解析该 profile

#### Scenario: Operation set 破坏性升级

- **WHEN**两个新 operation 进入正式 Program ABI
- **THEN** OperationSetVersion MUST从 `/4` 升级到 `/5`
- **AND** `/4` Program MUST被判定 stale 并重新生成
- **AND**系统 MUST不提供 `/4` reader、fallback evaluator 或运行时 migrator
