## REMOVED Requirements

### Requirement: Query Fixture Preview必须复用正式MM Module与唯一Pose链

**Reason**：独立 Query Fixture 不再承担完整角色预览，编辑器不应绕过正式角色事实选择完整表现结果。
**Migration**：在独立场景 Play 中通过正式输入运行角色，观察其真实 MM 查询、选择和 Pose 输出；保留正式 Database、Admission、Search、Plan、State 和完成合同。

## ADDED Requirements

### Requirement: MM场景预览必须观察真实角色查询与结果

MM 完整角色预览 MUST使用独立场景中真实 Actor 的正式 Definition、MM Pose source、Projection 和 Database。查询输入 MUST由正式角色运行产生，作者页面 MUST只观察实际 Admission、Search、Selection、历史与 Pose 结果，不创建自己的 MM 实例、查询 fixture 或完整角色播放器。需要世界上下文的阶段 MUST使用该真实 Actor 的正式上下文。

#### Scenario: 预览移动中的MM选择

- **WHEN** 作者通过合法输入改变角色移动方向
- **THEN** 页面 MUST显示正式 MM 的实际查询、选择和同一 Pose Plan 的可用完成结果
- **AND** MUST不通过编辑器手写 query 替代真实角色输入

#### Scenario: Database身份已过期

- **WHEN** 所选 Actor 的 Projection、Database 或分析产物与正式 Definition 不匹配
- **THEN** 正式准备 MUST拒绝运行并显示精确不匹配项
- **AND** MUST不自动重建、迁移旧 query 或选择其它 Definition
