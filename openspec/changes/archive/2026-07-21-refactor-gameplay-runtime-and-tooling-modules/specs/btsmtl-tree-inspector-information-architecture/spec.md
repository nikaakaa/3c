## ADDED Requirements

### Requirement: Tree Editor内部职责必须由独立模块拥有

Tree Editor MUST保留现有TreeWindow、TreeView和Inspector对外入口，但graph mutation、node/edge visual、selection inspector、Graph Data Catalog、window navigation/domain reload restore与runtime overlay MUST由职责独立的内部模块拥有。mutation模块 MUST是create/link/delete/paste/condition cleanup与Undo的唯一owner；Inspector与Data Catalog MUST不互相持有可写状态；window navigation与runtime overlay MUST分别拥有authoring locator和window-local debug binding。系统 MUST不创建第二套Graph写入口、运行时Tree读取路径或按名称近似恢复。

#### Scenario: 删除StateMachine Transition edge

- **WHEN** 作者在TreeView删除带condition graph的Transition edge
- **THEN** TreeView MUST把唯一mutation request交给graph mutation模块
- **AND** mutation模块 MUST在同一Undo边界更新edge、condition ownership与identity
- **AND** Inspector或visual layer MUST不再次修改asset

#### Scenario: Data页切换到Inspector页

- **WHEN** 作者在同一TreeWindow切换Data与Inspector
- **THEN** Data Catalog的source/filter/foldout状态 MUST由catalog模块保持
- **AND** Inspector MUST只根据当前selection投影authoring内容
- **AND** 页签切换 MUST不改变Graph locator或runtime binding

#### Scenario: Play Mode domain reload

- **WHEN** TreeWindow经domain reload恢复当前Graph
- **THEN** navigation模块 MUST只使用serialized owner、property path和GraphAuthoringId恢复authoring target
- **AND** runtime overlay模块 MUST创建新的window-local binding
- **AND** MUST不恢复旧runtime instance或按窗口顺序选择其它Graph

#### Scenario: Graph与Timeline同时Live Debug

- **WHEN** TreeWindow和TimelineEditorWindow同时观察同一Session
- **THEN** Tree runtime overlay MUST只修改自己的Graph binding
- **AND** 共享provider current state与Capture history position MUST保持统一
- **AND** Timeline窗口本地binding MUST不被Tree导航或页签修改
