## MODIFIED Requirements
### Requirement: Play Mode 延迟调试入口
系统 MUST 提供 Play Mode MonoBehaviour 组件，允许在 Unity Editor 中配置延迟参数、选择预测策略，并按键触发延迟 reconciliation 测试。该组件 MUST 装配在独立 `RollbackDebugRig` prefab 的场景实例上，并通过显式引用连接目标角色、input history、snapshot history 和 replay adapter；它 MUST NOT 要求挂载在正式角色对象上才能工作。

#### Scenario: 配置延迟参数
- **GIVEN** `LocalLatencyReconciliationDebugRunner` 装配在独立 `RollbackDebugRig` prefab 实例上
- **AND** Debug Rig 已显式引用目标角色 replay adapter、input recorder 和 snapshot recorder
- **WHEN** Inspector 中设置 `LatencyTicks = 3`
- **THEN** 远端输入 MUST 延迟 3 tick 到达
- **AND** 目标角色对象 MUST NOT 因此新增 latency debug runner 组件

#### Scenario: 按键触发 reconciliation
- **GIVEN** 延迟模拟器和 reconciliation runner 已配置
- **WHEN** 用户按下配置的触发键（默认 F7）
- **THEN** 系统 MUST 执行一次完整的 reconciliation 检查
- **AND** Console MUST 输出 PASS/FAIL 和诊断信息

#### Scenario: 安全探针语义
- **GIVEN** reconciliation 未启用"应用结果到场景"
- **WHEN** 触发键按下并完成 reconciliation
- **THEN** 角色 MUST 恢复到触发前的最新现场快照
- **AND** 角色状态 MUST NOT 因 reconciliation 而永久改变

#### Scenario: 可见 correction 模式
- **GIVEN** 启用了"应用结果到场景"
- **AND** 配置了 `PresentationTransformInterpolator`
- **WHEN** reconciliation 完成后 position 或 yaw 发生校正
- **THEN** 表现根 MUST 从触发前 visual pose 插值追到校正后逻辑根

#### Scenario: 缺失 Debug Rig 引用时失败
- **GIVEN** `LocalLatencyReconciliationDebugRunner` 缺少 input recorder、snapshot recorder 或 replay adapter 引用
- **WHEN** 用户触发 F7 或等价 latency reconciliation
- **THEN** runner MUST 返回诊断失败
- **AND** MUST NOT 从目标角色层级扫描第一个匹配 MonoBehaviour 作为正式 fallback 绑定
