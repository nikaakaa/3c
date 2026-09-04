## Why

当前 BTSMTL 作者链已经具备共享画布、Capability、Document v4、正式 Mutation 和编译执行基础，但节点规则、Document 对账、窗口导航、调参和 Projection 组装仍集中在多个大类中，一次业务修改需要理解过多无关状态。作者窗口还存在编译时直接关闭、重复订阅以及运行刷新重建控件的情况；需要在保持正式作者数据和运行行为的前提下，按业务所有权完成模块化，并接好已有场景运行式预览方案的作者端接口。

## What Changes

- 按控制流、状态与条件、输入与黑板、Action、Motion、AI 分离 BTSMTL 节点作者规则。领域模块提供声明和正式配置操作，现有共享 Capability 与唯一 Port Shape Projector继续服务人工 UI、Document、Clipboard、Mutation 和 Validator；复用已安装的 Pose Node Definition，不重新设计 Pose 编译器或运行算法。
- 将 Package Codec／Mapper、Gameplay 与 Presentation Reconciler、Mutation lowering 按业务内容拆分。入口只组织严格解析、引用索引、依赖顺序和完整计划，继续使用一个 Document hash、一份完整计划和唯一资产事务，不增加分片 apply 或领域专用 MCP。
- 将 Tree、Pose、Action 窗口中的文档导航、领域命令、编辑资格、调参协调和只读诊断投影移入明确模块。共享 Shell 继续负责区域、画布和交互；每个窗口保留自己的选择、页面和 Follow／Pin，运行目标与数据版本通过正式上下文投影。
- 修正作者可观察的窗口行为：脚本编译不再无条件关闭窗口；重载后恢复可解析的文档、页面与视图状态；只读运行刷新保留当前字段编辑、选择和滚动位置；关闭或重绑只释放本地绑定一次。
- 按动画来源、有限 Action producer、混合目录、既有相机／Cue、装备等内容拆分外层 Presentation Projection 编译，继续汇入同一 Character Build。保持既有 dense index 分配、规范顺序、数学、ABI、产物身份算法和原子发布。
- **BREAKING（Editor 内部扩展接口）**：迁移被替代的中央节点配置接口、窗口业务回调和不匹配的文件／类型命名，删除对应旧实现与转发别名。既有 Unity 作者资产、序列化节点类型、stable identity、Document v4 文件与五工具合同保持。
- 与 `rebuild-btsmtl-preview-with-scene-play` 分工：该变更唯一拥有场景配置、启动／重建、正式输入、预览执行切换和旧播放器删除；本变更拥有窗口和作者模块的内部边界。同一调用点只迁移一次，不复制预览协调器、不在本变更扩展旧 fixture。

## Capabilities

### New Capabilities

无。内部模块拆分不作为新的运行或作者业务能力。

### Modified Capabilities

- `graph-authoring-editor-shell`：明确编译与重载后的作者上下文恢复、只读刷新对编辑状态的保护，以及本地视图生命周期的单次订阅与释放行为。
- `character-action-animation-authoring-workspace`：明确动作工作区恢复精确角色／动作／调用点上下文，以及 Preview／Live 数值刷新不打断现有编辑操作的行为。

## Impact

- 代码范围：`Main/Runtime/BTSMTL/TreeDesigner/Editor`、`BTSMTL/Timeline/Editor`、`Main/Editor/CharacterPipeline/Authoring`、`AgentAuthoring`、`Main/Editor/CharacterSimulation/Compilation/Presentation/CharacterPresentationProjectionCompiler.cs` 及其直接调用者。完整迁移表、模块输入输出和目录归属见 `design.md`。
- 正式数据：本次内部重构不要求迁移角色业务资产、不改变 Graph／Timeline／Profile 所有权、不升级 Document 或 Program ABI。若实现发现必须改变这些合同，应先报告具体冲突，不能隐式升级或保留兼容路径。
- 关联变更：已有场景预览、Pose 架构、ZZZ IK、相机、ACL、诊断与性能工作分别保留其业务所有权；不接管它们的算法、资源接入或剩余验收，不修改 TrainingEnemy 资产。通用解释器整体去留仍属于未作出的产品范围决定，本次不据“未找到挂载”推导删除整套执行能力。
- 验证：复用现有 Validator、Document 往返、编译产物与正式 Replay／Proof。记录实现前完整工作区和资产基线，先证明同版本可重复，再对账每个迁移单元；不新增测试代码，不把手动验收写入任务，不用运行异常仍标记 Completed 的性能产物证明无回归。
- 规划只写本 change 的文档和 delta specs，不提前改写 current specs、其它 active change 或项目代码。与现行规范的保持项、差异和已发现冲突见 `design.md` 的规范对账表。
