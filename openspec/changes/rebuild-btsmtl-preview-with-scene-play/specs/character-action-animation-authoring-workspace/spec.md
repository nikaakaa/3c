## REMOVED Requirements

### Requirement: Workspace Preview必须只运行表现链

**Reason**：表现专用 Action/Base Pose fixture 无法表达本次要求的完整场景运行。
**Migration**：动作试验进入统一独立场景 Play，经正式输入与 Action admission 执行，工作区观察同一次真实运行。

## MODIFIED Requirements

### Requirement: Workspace Live Debug必须只读取正式Trace

Workspace 的场景预览观察与外部 Live Debug MUST从匹配正式运行身份的诊断源显示 ActionInstance、Action lifecycle、committed Timeline sample、projected presentation sample、Playback lifecycle、AnimationSlot route、Blend/Stored/Inertialization 状态与 Final Pose 贡献。观察区域 MUST只读，不得重新执行 Graph、Timeline 或 Pose，也不得显示 Action Phase relation。运行可调的作者字段 MUST位于明确的作者区域，通过同一 Mutation 与精确 Actor 参数入口修改，不得编辑 Trace 或推断已生效。

#### Scenario: Action被Hit打断

- **WHEN** 正式 Runtime 发生 Attack 到 Hit 的 Action replacement
- **THEN** 工作区 MUST显示旧 Action terminal、替换 command、Slot route、混合策略与最终 Pose 贡献
- **AND** 所有观察 MUST来自同一正式运行事实

#### Scenario: Trace过期

- **WHEN** Trace 与当前正式拓扑或 Projection 身份不匹配
- **THEN** 工作区 MUST显示过期并停止错误关联
- **AND** MUST不自动 Build、按显示名重建关系或用编辑游标补算

### Requirement: Workspace必须保持Numeric Target与显式Build边界

Workspace 作者文档 MUST不保存 NumericProfile 或 Float32/Fixed runtime state。场景配置 MUST明确选择正式 Numeric Target，工作区 MUST只读显示该次 Session 的选择；相同 Presentation Contract 的 Float32 与 Fixed MUST映射到同一 producer、AnimationSlot 和 Pose Plan。窗口打开、selection、mutation、普通开始预览、Live Debug 和 asset import MUST不自动 Build、重分析或选择替代产物；明确构建操作 MUST使用精确 Definition 和所选 Target。

#### Scenario: 查看Fixed Session动作

- **WHEN** 工作区连接匹配合同的 Fixed Session
- **THEN** 观察 MUST显示 Fixed Target identity 和 committed raw sample
- **AND** 表现 MUST复用对应的 target-neutral Projection，不切换 Float32

#### Scenario: 修改Timeline Clip

- **WHEN** 作者在 Edit Mode 完成 Timeline mutation
- **THEN** 正式 owner MUST进入原有 Undo 并显示产物状态
- **AND** 系统 MUST等待明确 Dry Run、Build 或构建并开始操作

## ADDED Requirements

### Requirement: 动作工作区必须通过场景真实角色试验动作

动作工作区 MUST保持精确 Definition、ActionProfile、Action call site、Timeline、producer 和 Slot 的关系解析，并通过统一场景预览操作连接真实角色。动作试验 MUST经明确输入映射和正式准入产生，Base Pose MUST来自同一角色实际运行事实；缺少映射、调用点、目标或产物时 MUST报告原因，不合成动画或 Gameplay 结果。

#### Scenario: 移动中试验攻击

- **WHEN** 作者使角色正式移动并通过合法输入请求攻击
- **THEN** 工作区 MUST观察同一角色的 Gameplay Action、当前 Base Pose、Slot 与最终表现
- **AND** MUST不另外配置 Base Pose fixture 或直接播放攻击资源
