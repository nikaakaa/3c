## MODIFIED Requirements

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST 区分 logic root 和 visual root。正式 Logic Pose Port MUST 表达碰撞、判定、网络预测和 motion correction 使用的逻辑真值；具体实现 MAY 包装 Unity `CharacterController`、纯 CSharp body 或外部权威 pose。表现插值 MUST 只应用到显式配置的 visual root / model root。PresentationFrame MUST NOT 调用 Motion Executor，MUST NOT 通过 Logic Pose Port 反写 logic root，MUST NOT 修改 `MotionResult` 或 `MotionCorrectionApplicationResult`。Presentation MUST 从正式 correction application result 获取 application extent，MUST NOT 从 motion debug snapshot 获取运行决策。

#### Scenario: 本地 motion 插值

- **WHEN** previous logic sample 和 current logic sample 都有有效 logic pose
- **THEN** PresentationFrame MUST 使用 interpolation alpha 计算 visual position 和 visual rotation
- **AND** 计算结果 MUST 应用到 visual root
- **AND** logic root MUST 保持 MotionStage 通过正式 executor/pose port 结算出的状态

#### Scenario: 网络校正后表现贴合

- **WHEN** logic tick 收到 motion correction 并产生 MotionCorrectionApplicationResult
- **THEN** correction MUST 仍由 MotionStage 的 correction phase 处理
- **AND** Presentation MAY 对部分应用使用普通 logic sample interpolation，对完整应用维持当前立即贴合行为
- **AND** 表现层 MUST NOT 把 correction 当作新的 motion contribution
- **AND** diagnostics 开关 MUST NOT 改变表现结果

### Requirement: Visual root 必须是正式配置

系统 MUST 让 `CharacterPipelineHost` 或等价 Unity 装配点显式持有 visual root / model root 绑定，并独立持有正式 Logic Pose Adapter 绑定。缺少当前模式所需绑定时，系统 MUST 报告正式配置错误。系统 MUST NOT 自动使用 `CharacterController.transform`、Logic Pose Adapter 所在 transform、Animancer 所在 transform、子节点搜索、同名对象搜索或 prefab 目录扫描作为 fallback。

#### Scenario: Host 配置 visual root

- **WHEN** 角色 Host 创建 `CharacterPipeline`
- **THEN** Host MUST 将正式 visual root 绑定传入表现层
- **AND** 表现层 MUST 只通过该绑定应用 visual pose
- **AND** visual root MUST 不等同于 Logic Pose Port 的隐式默认目标

#### Scenario: 缺少 visual root

- **WHEN** 角色需要表现插值但 Host 没有配置 visual root
- **THEN** 系统 MUST 报告配置错误
- **AND** 系统 MUST NOT 静默把 logic root 当成 visual root 使用
