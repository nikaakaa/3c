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
