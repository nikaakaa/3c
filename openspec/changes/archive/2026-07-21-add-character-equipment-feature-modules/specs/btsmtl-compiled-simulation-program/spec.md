## ADDED Requirements

### Requirement: Compiled Program必须包含不可变Equipment catalog和layout

Target Program MUST包含canonical Equipment Slot、Route、Equipment、Feature、Parameter constant、Action binding、graph entry、Tag/Effect contribution、Presentation requirement与Initial Loadout catalog，并为Equipment aggregate、Feature local state、pending change和Action Equipment Context分配类型化state layout。Catalog和layout MUST参与Program/Layout identity，Runtime MUST不从Unity authoring资产补建。

#### Scenario: Program加载装备catalog

- **WHEN** Runtime创建Corin ProgramCatalog
- **THEN** MUST一次构建Equipment lookup和entry layout
- **AND** 每Tick MUST不重扫Feature、Graph或Action列表

#### Scenario: Equipment catalog bytes被修改

- **WHEN** canonical equipment bytes与ProgramHash不匹配
- **THEN** Program load MUST拒绝
- **AND** MUST不只重算Equipment子表继续运行

### Requirement: Program identity必须覆盖Equipment authoring真相

SourceRevision、SemanticHash、ProgramHash与LayoutHash MUST按各自现行职责覆盖Equipment Profile及全部引用Feature/Graph/Timeline/parameter/Tag/Effect/Presentation requirement。相同source在Float32与Fixed MAY具有不同ProgramHash/LayoutHash，但 MUST具有可核对的同一SemanticHash；不同Equipment catalog的Program snapshot MUST不可交换。

#### Scenario: 只修改武器参数

- **WHEN** 作者修改Sawblade MotionScale
- **THEN** SourceRevision、SemanticHash与目标ProgramHash MUST改变
- **AND** 旧generated Program MUST被判定过期

#### Scenario: 只修改Unity Prefab视觉资源

- **WHEN** SpawnedVisualAsset引用或binding pose改变
- **THEN** Presentation Projection identity MUST改变
- **AND** 若Program只保存稳定VisualBindingId，Gameplay SemanticHash MUST不因Unity表现内容无意义改变

### Requirement: Program Execution Layout必须预构建Equipment索引

Program runtime initialization MUST按Program一次构建Slot/Route/Equipment/Feature/Parameter/entry和state address索引，并验证引用闭包。Actor/Tick热路径 MUST使用稳定index或typed handle，不得执行LINQ catalog重建、字符串查找、AssetDatabase访问或Feature list排序。

#### Scenario: 每Tick解析PrimaryAction

- **WHEN** Route Host解析MainWeapon PrimaryAction
- **THEN** MUST通过预构建Slot/Route/Feature index定位entry
- **AND** MUST不分配临时集合或按字符串扫描

#### Scenario: 初始化发现悬空entry

- **WHEN** Route catalog引用不存在的operation entry
- **THEN** Program execution layout build MUST失败
- **AND** Session MUST不进入Active

