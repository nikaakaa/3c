## MODIFIED Requirements

### Requirement: Shell必须保持重操作的显式触发边界

Shell Toolbar MAY暴露domain提供的Compile或Build命令，但selection、Inspector focus、Graph mutation、窗口创建、窗口恢复、Preview target切换、AssetDatabase import或refresh MUST不自动触发Program、Projection、Foot Analysis、Motion Matching Database构建。Shell MAY刷新轻量validator与Stale状态，但 MUST不自行修复Stale产物。

#### Scenario: 修改Pose Graph连线

- **WHEN** 作者连接一个Pose edge
- **THEN** mutation adapter MUST更新真实Pose Graph owner并允许轻量validation刷新
- **AND** Projection Build MUST保持未触发并显示Stale

#### Scenario: 显式点击Compile

- **WHEN** 作者点击当前domain正式提供的Compile或Build命令
- **THEN** Shell MUST只调用该domain唯一正式命令入口
- **AND** MUST不复制compiler、发布事务或AssetDatabase保存逻辑

## ADDED Requirements

### Requirement: 游戏AI作者窗口必须随旧领域退役

项目 Graph Shell MUST不再装配旧 AI Tree 窗口、菜单、页栈、Data Catalog 或 AI Program 状态面板。游戏 AI 图 MUST在其正式插件作者窗口中编辑和调试；项目 Shell MUST保留其它领域的既有布局、Undo、选择、导航和轻量刷新，MUST不重新承载插件图或创建第二个 AI Workbench。

#### Scenario: 打开角色技能和AI行为

- **WHEN** 作者分别打开角色技能内容与插件 AI 行为
- **THEN** 两者 MUST进入各自唯一正式作者窗口
- **AND** 旧 AI Tree 窗口 MUST不再作为可用入口出现
