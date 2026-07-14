## ADDED Requirements

### Requirement: ExternalPose 表现必须消费模型已解析的 visual pose

ServerAuthoritative binding MUST 按 server tick、正式 clock、4 tick interpolation delay 和有界 snapshot buffer 计算 `ExternalPresentationPose`。Character PresentationStage MUST 只消费该 model-neutral resolved pose并更新 visual root；它 MUST NOT 读取 MotionSnapshot packet、server clock、endpoint 或 Fantasy message。

#### Scenario: 两个快照之间渲染

- **WHEN** buffer 中存在包围目标 presentation server time 的前后 snapshot
- **THEN** model sampler MUST 插值 position 和 rotation
- **AND** PresentationFrame MUST 只把结果应用到 visual root

#### Scenario: 只有一个合法快照

- **WHEN** buffer 尚不足两个 snapshot
- **THEN** model sampler MUST 保持最新确认 pose
- **AND** MUST 不创建无限外推或 LocalSolver fallback

#### Scenario: 快照过期

- **WHEN** 最新 snapshot age 超过 30 个 server tick
- **THEN** visual root MUST 冻结在最新确认 pose
- **AND** model diagnostics MUST 标记 stale age 和 last server tick

### Requirement: ExternalPose sampling 必须复用同一 PresentationFrame

GameplayTickSystem MUST 允许现有 target hook 在同一 PresentationFrame 前提交 resolved external visual pose。Character visual pose、Timeline visual sampling 和 Animancer fade MUST 继续由同一 target `PresentationFrame` 推进。系统 MUST NOT 新增网络专用 Update、第二个 PresentationStage、Animator Controller 或 direct Animancer Play。

#### Scenario: 远端 Character 边移动边攻击

- **WHEN** external pose interpolation 正在推进且 ExternalActionActivation 启动 Attack Timeline
- **THEN** visual root MUST 继续使用 resolved external pose
- **AND** Attack animation MUST 继续通过 AnimationLayerSelection、Queue、Lifecycle 和 Animancer 播放
- **AND** Attack root motion MUST 不改变远端 logic root

### Requirement: Logic pose 与 external visual pose 必须保持分离

ExternalPose MotionStage MUST 在 logic tick 通过 Logic Pose Port 接受最新合法 external logic pose；PresentationStage MUST 在 render frame 使用 resolved external visual pose。表现采样 MUST 不反写 logic root、不调用 Motion Executor、不生成 SyncFacts，也不得参与 Owner correction。

#### Scenario: Owner correction

- **WHEN** LocalSolver Owner 应用 MotionCorrection
- **THEN** Presentation MUST 继续只消费既有 correction application result
- **AND** external snapshot sampler MUST 不参与 Owner pose
