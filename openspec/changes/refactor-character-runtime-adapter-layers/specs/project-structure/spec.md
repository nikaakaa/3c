## ADDED Requirements
### Requirement: Character Runtime Adapter 代码分层
系统 MUST 将 Character 运行时 adapter 的代码按 `Runtime / Model / Solver / Diagnostics / Contracts` 分层组织。Runtime Adapter MAY 持有 Unity 场景引用并执行正式外围调用；Model、Solver 和 Diagnostics MUST 保持可测试边界，不得把 Unity runtime、状态机权威、运动执行权威或动画播放权威混入同一文件。

#### Scenario: Runtime Adapter 只做外围装配
- **WHEN** 新增或调整 Character runtime adapter
- **THEN** MonoBehaviour、Unity 生命周期、场景引用解析和 prefab 装配 MUST 放在 `Runtime/`
- **AND** Runtime Adapter MUST 委托纯数据规则给 `Solver/`
- **AND** Runtime Adapter MUST 委托日志格式和提交给 `Diagnostics/`

#### Scenario: Model 和 Solver 保持纯数据边界
- **WHEN** 新增或迁移 Character `Model/` 或 `Solver/` 代码
- **THEN** 这些模块 MUST NOT 持有 `MonoBehaviour`、`Transform`、`CharacterController`、`InputAction`、Animancer runtime 对象或 `UnityEngine.Object` 场景实例引用
- **AND** Solver MUST 通过纯数据输入输出表达规则结果

#### Scenario: Contracts 只表达真实 seam
- **WHEN** 拆分 Runtime Adapter 内部 helper
- **THEN** 实现 MUST NOT 为只有一个实现且没有真实替换点的 helper 新增 public Contract
- **AND** 已存在的跨 Runtime Adapter seam MAY 保留在 `Contracts/`
- **AND** 新增 Contract MUST 有明确调用方和 adapter 变化点

#### Scenario: Active change 顺序
- **WHEN** 拆分范围与 Locomotion、animation playback rollback 或 animation motion source active change 重叠
- **THEN** 实现 MUST 优先遵守对应 active change 的权威语义
- **AND** 本分层变更 MUST NOT 通过新目录或 helper 恢复被那些变更禁止的分裂路径

### Requirement: 胖 Runtime Adapter 拆分验收
系统 MUST 以职责归属、静态边界和行为保持作为 Runtime Adapter 拆分验收，而不是以固定文件行数作为唯一目标。拆出的 Module MUST 提升 Interface Depth 和 Locality；仅把长方法搬到同等复杂的 public helper 不满足本要求。

#### Scenario: 拆分后的 Module 有明确职责
- **WHEN** 从 Runtime Adapter 拆出新的 Module
- **THEN** 该 Module MUST 有单一职责说明和明确输入输出
- **AND** 调用方 MUST 不需要知道该 Module 内部的 Unity 引用解析、日志格式或中间步骤

#### Scenario: 行为不因拆分改变
- **WHEN** Runtime Adapter 被拆分为 Model、Solver、Diagnostics 或 helper Module
- **THEN** Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Dodge 的既有运行行为 MUST 保持不变
- **AND** 拆分 MUST 有自动测试或 characterization 测试证明关键输出一致

#### Scenario: 不新增 fallback 配置
- **WHEN** 拆分引用解析、配置解析或 factory helper
- **THEN** 实现 MUST NOT 新增 `Resources.Load`、全局单例配置读取、硬编码默认配置或旧字段 fallback
- **AND** 缺失正式配置 MUST 继续走明确诊断路径
