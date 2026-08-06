## MODIFIED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

`GraphAuthoringEditorShell` MUST继续为BTSMTL Tree与AI domain提供现有Toolbar、Navigator、Graph Canvas、Details与可折叠Bottom Dock区域，并保持其Data Catalog、Inspector和Live Debug体验。Graph Authoring Framework MUST另外暴露唯一domain-neutral interaction core，包括`GraphAuthoringCanvasView`、selection、搜索、创建菜单、clipboard、Undo/Redo、breadcrumb、StateMachine表面和Details宿主。Character Presentation PoseGraph Workspace MAY围绕同一interaction core组合角色画面、图导航、selection Details、Preview typed输入和target控制，但 MUST不修改Tree/AI布局、创建第二GraphView、第二selection、第二Undo或第二Mutation链。所有domain仍 MUST通过显式adapter提供document、capability、port policy、mutation、presenter与diagnostics。

#### Scenario: 打开BTSMTL RootTree

- **WHEN** 作者通过正式入口打开RootTree
- **THEN** `GraphAuthoringEditorShell` MUST继续显示现有Graph Data Catalog、BTSMTL Canvas、Details和Live Debug表面
- **AND** MUST不创建Pose Preview、动画输入、Pose字段或Character target选择器

#### Scenario: 打开Character Pose Graph

- **WHEN** 作者从精确CharacterAnimationPresentationProfile上下文打开PoseGraph
- **THEN** 现有PoseGraph窗口 MUST装配同一个Graph interaction core和Pose domain adapters
- **AND** MAY在窗口中组合动画专属角色画面和输入
- **AND** MUST不要求BTSMTL Tree/AI采用相同工作区布局

### Requirement: 旧固定两栏布局必须原子迁移

BTSMTL Tree与AI正式窗口 MUST保持已经安装的Graph Authoring Editor Shell入口，不得恢复旧Data/Inspector互斥页签、固定左侧Inspector或重复selection。Character PoseGraph正式窗口 MUST原子删除旧手工Preview/Live/Diagnostics装配、重复Graph模式和窗口私有Graph交互，只保留一个基于共享interaction core的PoseGraph工作区。系统 MUST不保留旧PoseGraph UI、Action Animation PoseGraph入口或临时reparent兼容桥接。

#### Scenario: 迁移后打开现有BTSMTL资产

- **WHEN** 作者双击现有BaseTreeAsset
- **THEN** 正式入口 MUST继续打开现有BTSMTL Workspace体验
- **AND** PoseGraph工作区迁移 MUST不改变其Data Catalog、Inspector、StateMachine或Live Debug行为

#### Scenario: 迁移后打开现有PoseGraph

- **WHEN** 作者从Presentation Profile打开现有PoseGraph资产
- **THEN** 系统 MUST只打开收口后的`CharacterPresentationPoseGraphEditorWindow`
- **AND** MUST不同时创建旧Preview Dock、第二Graph窗口或Action Animation工作区

### Requirement: 旧BTSMTL窗口路径必须迁移而不是并存

现有BTSMTL Graph作者入口 MUST继续使用Graph Authoring Editor Shell和BTSMTL domain adapter，Shell已经接管的旧window/view交互、领域外公共静态入口和重复clipboard/Undo/Inspector路径 MUST保持删除。PoseGraph MUST通过现有`CharacterPresentationPoseGraphEditorWindow`和共享interaction core获得独立asset编辑体验；不得新增Workbench、复制GraphView框架或改变BTSMTL正式入口。Action Animation Workspace MUST不成为PoseGraph的正式打开路径。

#### Scenario: 直接打开BaseTreeAsset

- **WHEN** 用户双击BaseTreeAsset
- **THEN** 正式入口 MUST打开现有基于Shell的BTSMTL document
- **AND** MUST不打开PoseGraph Workspace或旧BaseTreeWindow实现

#### Scenario: 从Profile打开PoseGraph

- **WHEN** 用户在精确Presentation Profile上下文执行Open Pose Graph
- **THEN** 正式入口 MUST打开现有PoseGraph窗口并复用共享Graph interaction core
- **AND** MUST不要求先打开Action、Timeline或Slot工作区
