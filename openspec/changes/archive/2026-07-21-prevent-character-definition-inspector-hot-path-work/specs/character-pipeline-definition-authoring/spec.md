## MODIFIED Requirements

### Requirement: Definition Inspector 必须分离作者配置与生成产物

Definition Inspector MUST以紧凑 Config References 作为默认作者界面。Program/Projection 引用、identity、Hash、capability 与 compiler report MUST属于 Generated Artifacts/Diagnostics 区域。Inspector selection、`OnEnable`、Layout、Repaint 和 foldout 切换 MUST只读取 serialized reference、轻量发布 Header 或当前 Inspector 会话缓存，MUST不运行 Compiler、完整 ProgramId/SourceRevision/ProjectionRevision/Target expectation 计算、Program decode、producer topology projection 或 `IsStale`。轻量发布 Header检查 MUST不加载 Program，也不得遍历 authoring dependency graph。

默认产物状态 MUST为 `Missing`、`Invalid` 或 `Unchecked`。`Unchecked` MUST明确表示产物已发布但当前 authoring source 尚未在本次 Inspector 会话中比较；Inspector MUST不把 `Unchecked` 显示为 `Ready`。只有作者显式执行 `Refresh Status` 后，Inspector MAY调用唯一正式 stale 检查并显示 `Ready` 或 `Stale`。Definition字段修改后 MUST显示 `Needs Compile`；Compile成功后 MAY直接显示 `Ready`。检查结果 MUST只属于Inspector会话，不得写入Definition、Profile、Program或Projection资产。

#### Scenario: 选择 Definition

- **WHEN** 作者选择或重新选择 CharacterPipelineDefinition
- **THEN** Inspector MUST只根据 serialized reference 与轻量发布 Header 显示 `Missing`、`Invalid` 或 `Unchecked`
- **AND** MUST不计算当前 SourceRevision、解码 Program 或重算 ProjectionRevision

#### Scenario: 重绘 Inspector

- **WHEN** Unity 对已打开的 Definition Inspector 执行 Layout、Repaint 或 foldout 切换
- **THEN** Inspector MUST只绘制当前会话状态
- **AND** MUST不调用 `IsStale`、Compiler、Program decode 或任何完整 dependency hash 入口

#### Scenario: 显式刷新产物状态

- **WHEN** 作者点击 `Refresh Status`
- **THEN** Inspector MUST执行一次正式完整 stale 检查
- **AND** MUST将结果缓存为 `Ready` 或 `Stale`，后续 Repaint MUST不重复该检查

#### Scenario: 修改 Definition

- **WHEN** 作者通过当前 Inspector 修改任一 Definition authoring 字段
- **THEN** Inspector MUST立即显示 `Needs Compile`
- **AND** MUST不为更新状态自动运行 Compiler 或 stale 检查

#### Scenario: 编译产物

- **WHEN** 作者点击 Compile 且正式 Build 成功
- **THEN** Inspector MUST显示 `Ready`
- **AND** Build失败时 MUST不显示虚假的 `Ready`

#### Scenario: 查看生成产物详情

- **WHEN** 作者显式展开 Generated Artifacts 或运行 Compiler Diagnostics
- **THEN** Inspector MAY显示 Program/Projection identity、Hash、capability 与 report
- **AND** foldout绘制本身 MUST不触发完整 stale 检查
- **AND** Compiler Diagnostics MAY按显式命令运行完整 dry-run
