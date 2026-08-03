## ADDED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

唯一`GraphAuthoringEditorShell` MUST提供Toolbar、Navigator、Graph Canvas、Details与可折叠Bottom Dock五个通用区域。Shell MUST继续通过显式domain adapter取得各区域内容，不得按BTSMTL Node、AI Node、Pose Node、AnimationChannel、Blackboard或Runtime Trace类型构造领域UI。Graph Canvas MUST继续承载唯一GraphView、selection、breadcrumb、搜索、clipboard和Undo链路；其它区域 MUST不保存第二份node、edge或selection集合。

#### Scenario: 打开BTSMTL RootTree

- **WHEN** 作者通过正式入口打开RootTree
- **THEN** Shell MUST在Navigator装配当前Graph Data Catalog、在Graph Canvas装配BTSMTL GraphView、在Details装配BTSMTL selection Inspector
- **AND** MUST不创建Pose Graph Navigator、Pose Preview或动画字段

#### Scenario: 打开Character Pose Graph

- **WHEN** 作者从显式CharacterAnimationPresentationProfile上下文打开Pose Graph
- **THEN** 同一Shell MUST装配Pose domain Navigator、GraphView、Details与Bottom Dock
- **AND** MUST不创建BaseGraph、BaseNode或第二套GraphView

### Requirement: Workspace布局状态必须是editor-only且不污染authoring

Navigator、Details和Bottom Dock的宽度、展开、折叠、选中页签、搜索、分组与Preview面板布局 MUST只保存为window-local或Editor view-state。任何布局变化 MUST不修改Graph、Timeline、Profile、Definition、Rig、Program或Projection revision。窗口尺寸不足时区域 MAY按确定规则折叠，但 MUST不切换到旧Data/Inspector互斥写路径。

#### Scenario: 折叠Bottom Dock

- **WHEN** 作者折叠Preview与Diagnostics区域
- **THEN** Graph Canvas MUST扩展使用可用空间
- **AND** 当前Graph asset MUST不变脏

#### Scenario: domain reload恢复窗口

- **WHEN** Editor domain reload后恢复Graph窗口
- **THEN** Shell MAY恢复editor-only布局状态
- **AND** document与runtime target仍 MUST按各自稳定identity重新绑定，不得恢复旧对象实例

### Requirement: Shell必须保持重操作的显式触发边界

Shell Toolbar MAY暴露domain提供的Compile或Build命令，但selection、Inspector focus、Graph mutation、窗口创建、窗口恢复、Preview target切换、AssetDatabase import或refresh MUST不自动触发Program、Projection、Foot Analysis、Motion Matching Database或AI Program构建。Shell MAY刷新轻量validator与Stale状态，但 MUST不自行修复Stale产物。

#### Scenario: 修改Pose Graph连线

- **WHEN** 作者连接一个Pose edge
- **THEN** mutation adapter MUST更新真实Pose Graph owner并允许轻量validation刷新
- **AND** Projection Build MUST保持未触发并显示Stale

#### Scenario: 显式点击Compile

- **WHEN** 作者点击当前domain正式提供的Compile或Build命令
- **THEN** Shell MUST只调用该domain唯一正式命令入口
- **AND** MUST不复制compiler、发布事务或AssetDatabase保存逻辑

### Requirement: 旧固定两栏布局必须原子迁移

Tree、AI与Pose Graph正式窗口 MUST迁移到同一Workspace region合同。系统 MUST删除旧固定`left-panel Inspector + right-panel Graph`装配、旧Data/Inspector互斥页签和重复selection projection；不得保留旧UXML入口、布局兼容开关、Pose Graph专用Shell或临时reparent桥接。

#### Scenario: 迁移后直接打开旧BTSMTL资产

- **WHEN** 作者双击任意现有BaseTreeAsset
- **THEN** 正式入口 MUST只打开新Workspace Shell
- **AND** MUST不同时创建旧TreeWindow布局或第二Inspector

