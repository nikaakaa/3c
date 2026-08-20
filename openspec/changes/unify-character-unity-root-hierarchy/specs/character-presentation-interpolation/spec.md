## MODIFIED Requirements

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST区分 World/Model Body真相、最外层LogicRoot单向投影、VisualRoot相对表现姿态与PoseRoot动画Component Pose。WorldSolve Pass、Pipeline Runtime或Model Egress唯一更新逻辑Body；成功Body commit projection MAY只把最终Body单向写入LogicRoot。`CharacterBodyPresentationRuntime` MUST只根据committed/predicted/selected BodyState samples与interpolation alpha计算唯一visible world pose，并把该结果转换为当前LogicRoot下的VisualRoot local pose；PresentationFrame MUST不写LogicRoot、不调用Solver、不申请restore、不修改World state或产生correction result。PoseRoot与其骨骼 MUST继续只由正式动画Pose Plan和Final Writer发布，Body Runtime与Foot Placement MUST不把世界位移写入PoseRoot。

#### Scenario: Local Motion 插值

- **WHEN** previous/current committed body samples有效且LogicRoot已经投影当前committed Body
- **THEN** PresentationFrame MUST计算唯一visible world pose
- **AND** MUST把visible pose转换为VisualRoot相对LogicRoot的local pose
- **AND** WorldSimulationState与LogicRoot MUST保持不变

#### Scenario: 后续模型执行 Hard Recovery

- **WHEN** Pipeline Runtime通过正式restore恢复World state并提交新Body anchor
- **THEN** Committer MUST按模型commit policy更新LogicRoot投影与visual sample history
- **AND** Body Runtime MUST从当前visible pose接管VisualRoot local correction
- **AND** Presentation MUST不自行改写逻辑Body或LogicRoot

### Requirement: Visual root 必须是正式配置

Character Host与Remote Presentation Template MUST显式引用唯一`CharacterRootHierarchyBinding`，该绑定 MUST声明互不相同的LogicRoot、VisualRoot与PoseRoot。LogicRoot MUST是角色实例最外层，VisualRoot MUST是LogicRoot直接子级，PoseRoot MUST是VisualRoot直接子级；Animator Transform MUST精确等于PoseRoot。Host MUST把同一LogicRoot交给WorldSolver binding或Body commit projection，把同一VisualRoot交给Body Presentation，并把同一PoseRoot交给Animation Rig。缺少当前composition所需绑定、父子关系错误、Animator归属错误或SelfColliderRoot不等于LogicRoot时创建 MUST失败。系统 MUST不自动使用CharacterController.transform、Animancer transform、子节点搜索、同名对象、prefab扫描或运行时补建作为fallback。

#### Scenario: Host 配置正式根层级

- **WHEN** Host创建Local、Fixed、Rollback或observed Corin
- **THEN** MUST从显式Root Hierarchy Binding取得LogicRoot、VisualRoot与PoseRoot
- **AND** MUST把LogicRoot绑定到唯一Body投影边界
- **AND** MUST把VisualRoot与PoseRoot分别绑定到Body Presentation和Animation Runtime

#### Scenario: 缺少正式根层级

- **WHEN** 任一角色需要表现但缺少三根之一、父子关系错误或Animator不位于PoseRoot
- **THEN** Host或Template创建 MUST报告配置错误
- **AND** 系统 MUST不搜索、补建或回退旧`AnimatorRoot`/VisualRoot-only结构

#### Scenario: 预制体外层表达逻辑位置

- **WHEN** 角色完成一个成功Body commit
- **THEN** 预制体最外层LogicRoot世界姿态 MUST等于该事务最终Body
- **AND** VisualRoot local姿态 MUST只表达visible pose相对该LogicRoot的表现差值
- **AND** PoseRoot MUST不承担第二份Body世界位移
