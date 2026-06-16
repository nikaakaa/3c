## ADDED Requirements
### Requirement: 条件集合保持受控扩展
Locomotion 和统一状态图配置 MUST 继续使用受控 condition key 集合表达 transition 条件。新增 condition key MUST 通过正式 evaluator adapter 和配置校验进入系统，不得允许任意运行时代码、场景对象或未审批 ScriptableObject 回调参与状态切换。

#### Scenario: 新 Locomotion 条件需要正式 adapter
- **WHEN** 后续新增 Sprint、JumpStart 或 TurnBack 变体条件
- **THEN** 条件 MUST 以稳定 key 和纯数据参数写入状态图配置
- **AND** MUST 有对应 evaluator adapter
- **AND** 配置 MUST 在缺少 adapter 时校验失败

#### Scenario: 配置不引用运行时对象
- **WHEN** 设计者检查状态图 transition condition
- **THEN** condition MUST NOT 持有 MonoBehaviour、Transform、Animator、InputAction 或任意场景实例引用
- **AND** condition MUST NOT 持有可执行回调对象
