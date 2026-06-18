## ADDED Requirements
### Requirement: Committed Action Branch Authoring Model
系统 MUST 提供通用 Committed Action branch authoring model，作为 `CharacterActionDefinitionSO` 或批准等价 action definition 的子模块。该 model MUST 能表达 branch id、root node、稳定节点 id、selector / condition / timeline 节点、稳定 child 顺序、默认 body claim、timeline authoring 数据和 editor layout。该 model MUST 能编译为现有 `CommittedActionBranchDefinition` 或批准等价纯 runtime model，且 runtime model MUST NOT 持有 Unity Editor、GraphView、ScriptableObject、scene object、Animancer runtime object 或 Ref/Taco runner。

#### Scenario: Action Definition 保存 Branch 节点树
- **WHEN** 设计者在正式 action definition 中配置 Committed Action branch
- **THEN** 该 action definition MUST 保存 branch id、root node id、节点列表和稳定 child 顺序
- **AND** TimelineNode MUST 保存正式 timeline authoring track、clip 和 payload
- **AND** authoring 数据 MUST 不依赖 editor view index 作为 runtime 选择顺序

#### Scenario: Branch Authoring 编译为 Runtime Definition
- **GIVEN** branch authoring 包含 selector、condition 和 timeline node
- **WHEN** action definition compiler 使用固定 tick compile context 编译该 action definition
- **THEN** compiler MUST 输出 `CommittedActionBranchDefinition`
- **AND** 输出 MUST 保留 selector child 顺序
- **AND** TimelineNode authoring seconds MUST 被编译为 runtime local tick 数据
- **AND** runtime evaluator MUST 能消费该 branch definition

### Requirement: Committed Action Branch Editor
系统 MUST 提供 Editor-only Committed Action Branch Editor 或批准等价入口，用于编辑正式 `CharacterActionDefinitionSO` 内的 Committed Action branch authoring。Branch Editor MUST 展示 selector、condition 和 timeline node，并通过 serialized adapter 写回 action definition。Branch Editor MUST NOT 编辑 `CharacterFramePipeline` phase、behavior source graph、motion executor、animation presenter、blackboard writer 或 Unity scene object binding。

#### Scenario: 打开正式 Dodge Branch
- **WHEN** 设计者打开 Committed Action Branch Editor
- **THEN** 编辑器 MUST 默认定位正式 Corin Dodge action definition
- **AND** ObjectField MUST 限定为 `CharacterActionDefinitionSO`
- **AND** 图中 MUST 展示 Dodge selector、Directional condition、Backstep condition、Directional timeline 和 Backstep timeline 或等价通用节点树

#### Scenario: 编辑节点后保存回 Action Definition
- **GIVEN** 设计者新增、删除、重排或修改一个 branch node
- **WHEN** 设计者保存
- **THEN** 修改 MUST 写回所选 `CharacterActionDefinitionSO`
- **AND** 保存后 `CharacterActionDefinitionSO.ToDefinition()` MUST 看到同一份 branch 修改
- **AND** 行为图 authoring asset MUST 不保存该 branch 数据

### Requirement: TimelineNode Panel
Branch Editor MUST 将 Timeline Editor 能力收敛为选中 TimelineNode 的 timeline panel 或批准等价 adapter。timeline panel MUST 读写该 TimelineNode 内的正式 timeline authoring 数据，MUST 继续使用 seconds authoring 和 fixed tick compile context 进行 preview / validation，MUST NOT 创建第二套 selector 或 timeline 数据权威。

#### Scenario: 选择 TimelineNode 后编辑 Timeline
- **GIVEN** Branch Editor 中选中了一个 TimelineNode
- **WHEN** 设计者新增 track、移动 clip 或编辑 payload
- **THEN** 修改 MUST 写回该 TimelineNode 的 timeline authoring 数据
- **AND** 其它 TimelineNode 的 track、clip 和 payload MUST 不被修改
- **AND** 保存后 runtime evaluator MUST 只从选中路径输出该 timeline 的结果

#### Scenario: Timeline 快捷入口不成为第二权威
- **WHEN** 设计者通过保留的 Timeline Editor 菜单打开工具
- **THEN** 工具 MAY 自动打开 Branch Editor 并选中默认 TimelineNode
- **AND** MUST 读写同一份 branch authoring 数据
- **AND** MUST NOT 使用独立 Directional / Backstep 特例字段作为正式保存目标

### Requirement: Toolchain Validation
Committed Action authoring toolchain MUST 提供自动测试和静态边界验证，证明 editor adapter、serialized asset、compiler、runtime evaluator 和 preview 使用同一份正式数据。测试 MUST 覆盖合法 branch、非法 branch、Dodge 迁移等价性、TimelineNode 写回、无 fallback 和 runtime 边界。

#### Scenario: 自动测试覆盖完整数据闭环
- **WHEN** 运行 Committed Action authoring toolchain EditMode 测试
- **THEN** 测试 MUST 覆盖 branch editor adapter 写回 action definition
- **AND** MUST 覆盖保存后重新加载并编译为 `CommittedActionBranchDefinition`
- **AND** MUST 覆盖 evaluator 对 Directional / Backstep 或等价 timeline path 的选择输出
- **AND** MUST 覆盖非法 branch 不生成可被正式 runtime 消费的半成品

#### Scenario: 静态边界验证
- **WHEN** 运行静态边界测试
- **THEN** runtime 源码 MUST 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner 或 branch editor 类型
- **AND** branch editor 源码 MUST 不直接调用 motion executor、animation presenter、blackboard writer 或 `CharacterController.Move`
- **AND** 测试 MUST 确认不存在从 legacy Dodge branch、Resources、sample asset 或代码默认值补齐缺失 branch 的正式路径
