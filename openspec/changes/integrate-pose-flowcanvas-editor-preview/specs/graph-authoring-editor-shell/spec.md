## MODIFIED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

工作区 MUST提供Toolbar、Navigator、唯一图编辑区域、Details与可折叠观察区域，领域内容通过显式adapter装配。Pose的画布、端口、选择及通用编辑交互 MUST由原生图编辑器负责，其它区域只组合领域信息，不保存第二份node、edge或selection集合。未迁移领域 MUST继续使用其正式Shell；共享Shell不按具体业务节点类型构造领域UI。

#### Scenario: 打开BTSMTL RootTree
- **WHEN** 作者通过仍有效的RootTree正式入口打开图
- **THEN** 工作区 MUST装配本领域Data Catalog、正式画布和Details
- **AND** MUST不创建Pose Navigator或动画字段

#### Scenario: 打开Character Pose Graph
- **WHEN** 作者从显式Profile上下文打开Pose图
- **THEN** 同一原生窗口 MUST装配Pose Navigator、图、Details和运行观察
- **AND** MUST不创建旧BaseGraph副本或第二套GraphView

## ADDED Requirements

### Requirement: 图窗口的运行观察不得拥有预览生命周期

Pose窗口 MUST在Play状态和有效目标变化时绑定或释放运行观察，MUST不负责加载场景、创建角色、启动独立图、控制私有时钟或提供窗口级播放／单步。构建继续由明确作者命令触发；打开窗口与切换目标不得Build或自动修复过期产物。

#### Scenario: 未进入Play时打开窗口
- **WHEN** 作者仅打开Pose编辑器
- **THEN** 工作区 MUST显示未播放并允许普通作者编辑，不自行进入Play或创建场景
