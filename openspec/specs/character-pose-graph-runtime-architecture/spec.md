# character-pose-graph-runtime-architecture Specification

## Purpose

定义原生 FlowCanvas Pose Graph 在正式 Character Presentation 中的运行边界、帧事务、资源生命周期、Constraint 与最终姿态发布。Pose Runtime 不再依赖 Pose Program Image、整角色 Projection 或第二套 Preview Runtime。

## Requirements

### Requirement: Pose Runtime 必须是 Actor-local 原生图组合

每个 Actor MUST 装配一份原生 FlowCanvas Graph instance、PoseState、Source Module、Constraint Module、Final Publication 和 Diagnostics Runtime。共享 Graph/资源只保存作者数据和稳定 identity，不保存 Actor 状态。Pose Runtime MUST 不创建 Pose IR、Program Image、旧 Operation Executor 或加载期 Compiler。

#### Scenario: 创建 Actor Pose Runtime

- **WHEN** CharacterPipelineHost 注册一个 Actor
- **THEN** Presentation Host MUST 按正式 Profile、Rig、Graph 和资源 binding 创建该 Actor 的原生图实例
- **AND** MUST 不从 CharacterSimulationProgram 或旧 Projection 生成 Pose 执行链

### Requirement: 根表现帧必须只有一条提交事务

根 Pose Frame MUST 统一管理 Prepare、Source Demand、Graph Evaluate、Constraint、Final Publication、Seal 和 Post-Commit。所有模块 MUST 通过同一 frame lineage 交换 Pending/Committed 结果；任何阶段失败都不得部分提交。

#### Scenario: Frame 在 Barrier 前失败

- **WHEN** Source、Graph 或资源准备失败
- **THEN** 当前 Frame MUST 丢弃所有 Pending 结果
- **AND** 上一份 Committed Pose MUST 保持不变

#### Scenario: Frame 在 Constraint 后失败

- **WHEN** Foot、Goal、FBBIK 或 Final Writer 失败
- **THEN** 当前 Frame MUST 阻止完整 Final Publication
- **AND** MUST 不写入部分 Physical Bone 或图外 Transform

### Requirement: 各模块只拥有自己的状态

PoseState 只拥有 Pose 状态和转换历史；Source Module 只拥有 Clip、Blend Space、Motion Matching source 的准备、采样和释放；Constraint Module 只拥有 Foot、Goal、Solver 状态；Final Publication 只拥有最终 Pose 页和 Physical binding。模块 MUST 不复制其它 owner 的可写状态或重新仲裁其它阶段。

#### Scenario: Source 释放

- **WHEN** PoseState 已不再需要某个 Source
- **THEN** Source Module MUST 按 generation 和 release completion 释放资源
- **AND** PoseState、Constraint、UI 和 Diagnostics MUST 不直接清理 Source 内部状态

### Requirement: 预览必须走 ScenePlay 正式 Actor

Pose Preview MUST 只作为 ScenePlay 对正式 Actor 的观察和输入入口。Pose Graph 页面 MUST 不创建独立 evaluator、Fact Fixture、Query Fixture、seek 时钟、临时 PlayableGraph、简化 Executor 或默认 Foot/Goal 结果。Live Debug MUST 只读取正式 Committed Result。

#### Scenario: 页面切换

- **WHEN** 作者从 Pose Graph 页面切换到 Timeline 或其它窗口
- **THEN** ScenePlay Session/Actor MUST 继续由 ScenePlay 协调器管理
- **AND** 页面只撤销自己的观察 interest，不得替换、暂停或销毁 Actor Runtime

### Requirement: Tuning 必须按 Actor 原子采用

运行时调参 MUST 先在 Actor-local Pending Tuning Candidate 中完成范围、identity、容量和 owner 校验，再由根 Frame 一次提升。Graph、Profile、Projection 或共享作者资产 MUST 不被 Runtime 直接改写；失败时所有 owner 保持旧值。

#### Scenario: 一个 Constraint 参数非法

- **WHEN** Program、Source 或 Constraint 的任一调参分区非法
- **THEN** 整个 Tuning Candidate MUST 被拒绝
- **AND** 其它分区 MUST 不得部分生效

### Requirement: Diagnostics 只能读取已提交事实

Diagnostics MUST 读取带同一 lineage 的 Committed Pose、Source、Constraint 和 Final Publication 事实。Diagnostics、Pose Watch 和 UI MUST 不参与求值、不扫描节点重新计算、不从 Animancer weight 或 Transform 反推业务事实。

#### Scenario: 观察已提交Pose

- **WHEN** Live Debug 请求当前 Actor 的 Pose 状态
- **THEN** Diagnostics MUST 只读取同一 lineage 的 Committed Pose、Source、Constraint 和 Final Publication
- **AND** Pose Watch 与 UI MUST 不重新求值或修改 Actor-local runtime

### Requirement: Final Pose Publication必须原子拥有最终结果与Physical写入

`CharacterFinalPoseNativePublication` MUST 唯一拥有 Committed/Pending Final Pose 物理页、完整 Physical Bone binding、Final Writer binding、整 Rig 预验证、唯一 Apply 和 Publication Result；物理页 MUST 是 Actor-local 双缓冲页，不进入共享作者资产或共享 Host。Actor Runtime 创建时 MUST 由 Role Dependency Factory 构造该 Actor 唯一的 Final Publication，并把原生图 Output 绑定到当前 Actor 唯一 Pending 页。原生图求值通过 actor-local binding 写入 Output Pose 并发布只读结果，MUST 不在求值缓冲之外分配第二 Final Pose buffer。Final Publication MUST 在写任何 Physical Bone 前验证 Pose availability、Rig、continuity、原生图求值 completion、Constraint completion 和 Frame lineage；合法时一次写入完整 Pending Pose，非法时保持 Committed Pose 并返回正式失败。

原生图校验 MUST 证明唯一 OutputPose 与唯一 Final Publication 要求；具体 Final Publication 实例、Physical Bone binding 和 Writer 唯一性 MUST 由 Role Dependency Factory 与 Final Publication 构造验证。系统 MUST 保持一个最终写入流程，不建立 Writer Graph 节点、第二 Writer、第二 Final Pose 页、图外 Transform 写入或运行时 Writer 选择。

当角色声明动画属性时，同一 Final Publication MUST 一并拥有属性 Committed/Pending 结果、Renderer/Mesh binding、dense 目标索引及写入 completion。最终写入流程 MUST 保持现有骨骼数学，并在同一整体预验证通过后写入骨骼和 BlendShape；属性 MUST 不创建独立 Publication、独立 Seal 或绕过根 Frame Transaction。所有目标、参数值和 lineage 必须在第一笔可见写入前完成验证。Source Graph、decoder 和 Preview MUST 不直接发布管理中的 Renderer 属性。

#### Scenario: Pending Pose完整合法

- **WHEN** 当前原生图求值 Result、Constraint Result、Final Pose 及角色声明的属性结果全部匹配同一 lineage
- **THEN** Final Publication MUST 一次写入全部 Physical Bones 和所声明 BlendShape，并发布同一 completion 的 Result
- **AND** Writer 成功后 MUST 不再执行可能失败的动画业务计算

#### Scenario: 一个Physical binding无效

- **WHEN** 任一 Physical Bone binding 在 Apply 前无效
- **THEN** Final Publication MUST 不写入任何 Pending Physical Bone 或属性
- **AND** 当前 Frame MUST 遵守 Barrier 后的 Fault 政策而不得切换第二 Writer 或恢复后继续

#### Scenario: 一个属性binding无效

- **WHEN** 任一声明 Renderer、Mesh revision 或属性索引在整体预验证时无效
- **THEN** Final Publication MUST 阻止本次骨骼和属性的全部写入
- **AND** MUST 不先提交骨骼后丢弃属性，也不得读取旧 Renderer 数值补成当前帧
