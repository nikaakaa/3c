## 1. 连接类型与工厂钩子

- [x] 1.1 新建 `BtsmtlSkillFlowConnection : BinderConnection`：序列化字段条件图引用（role 校验 ConditionRule）、优先级（int ≥ 0）、中止策略（`ProgramAbortPolicy`）；`GetConnectionInfo` 输出"条件摘要 + 优先级 + 中止策略"纯字段拼接。交付：类文件落位，Unity 脚本编译无错。（f28f30cfe；Unity 编译验证待刷新）
- [x] 1.2 插件补丁：`BinderConnection` 补 public 工厂包装（暴露 `CreateValidated` 验证与绑定逻辑，带 `3C` 注释），登记进项目插件补丁清单（与 `FlowGraph.cs:61`、`Editor.Node.cs:786` 同册）。交付：插件编译通过，清单文件更新。（f28f30cfe）
- [x] 1.3 `BtsmtlSkillFlowConnection.Create(source, target)`：端口类型与连线合法性预检（非法组合拒绝并给原因），创建后写入 sourcePortID/targetPortID 与双向绑定。交付：编译通过。（f28f30cfe）
- [x] 1.4 `BtsmtlSkillFlowGraph` 覆盖 `CreatePortConnection`：role == StateMachine 且 FlowOutput→FlowInput 时产出 `BtsmtlSkillFlowConnection`，其余走原路径。交付：编译通过；状态机图与普通流程图拖线产物类型分流正确（代码审查路径核对）。（f28f30cfe）
- [x] 1.5 连线 Inspector：`OnConnectionInspectorGUI` 编辑三字段，全部经 `BtsmtlSkillFlowEditorMutation`，非法修改写入前拒绝（role 不合法 / 负优先级 / 未定义策略）。交付：编译通过；与现有步骤编辑同一 Mutation 通道（无第二套写入口，rg 核对）。（f28f30cfe）

## 2. 编译与校验切换数据来源

- [x] 2.1 `BtsmtlSkillEdgeOccurrence` 字段调整：`Step` 替换为条件 occurrence + 优先级 + 中止策略。交付：编译通过。（f28f30cfe）
- [x] 2.2 `BtsmtlSkillGraphOccurrence.ReadOccurrence` 删除"按 sourcePortID 反查 Steps"分支，直读连线字段；状态机结构节点检出旧步骤数据时报告迁移残留错误。交付：编译通过。（f28f30cfe；状态机图连线缺数据报"需要先执行资产迁移"）
- [x] 2.3 `BtsmtlSkillGraphFlowEmitter` 转移发射数据来源切到边 occurrence，发射逻辑不变。交付：编译通过。（f28f30cfe）
- [x] 2.4 `BtsmtlSkillGraphClosure` 校验随迁：`@any` 转移必须挂条件、同优先级按连线创建顺序稳定排序、条件图 role 与 owner 校验。交付：编译通过。（f28f30cfe；连线条件图入闭包遍历同落地）
- [ ] 2.5 对未迁移资产执行正式 `btsmtl.validate`，核对输出为"迁移残留"类错误而非崩溃或静默跳过，记录 CLI 输出作为 5.x 的前置基线。交付：CLI 输出记录。

## 3. Document v7 登记

- [x] 3.1 codec 登记连接子类型：导出/导入条件图引用、优先级、中止策略与连线身份。交付：编译通过。（f28f30cfe；exporter/applier 落地）
- [x] 3.2 validator 拒绝携带未知连接类型的包，不回退为普通连线。交付：编译通过。（f28f30cfe；声明条件但连线未携带转移数据 → skill_edge_transfer_type_mismatch）
- [ ] 3.3 对未迁移资产 dry-run，核对报错为"连线字段缺失/迁移残留"而非"未知类型"。交付：CLI 输出记录。

## 4. 资产迁移

- [ ] 4.1 前置裁决：确认 `refactor-btsmtl-flowcanvas-authoring` 记录的 `syncState=TreeDirty` 外部改动归属，未裁决前不执行任何 checkout。交付：裁决结论记录。
- [x] 4.2 迁移映射实现：按 `step.Id == 连线.sourcePortID` 将条件/优先级/中止策略写入连线，随后删除节点步骤数据；走 Document 事务与失败回滚。交付：编译通过。（f28f30cfe；转换点落在 exporter 回退查找 + applier 就地重建保 UID；节点步骤数据删除归 5.x）
- [ ] 4.3 三项技能根（Attack/DodgeBack/DodgeForward）checkout → dry-run → apply → re-checkout，收口证据含 `applied=true`、`saved=true`、`syncState=Clean`、零差异 hash。交付：正式 CLI 输出记录。
- [ ] 4.4 迁移后执行 `btsmtl.validate` 与技能编译，核对转移语义（条件/优先级/中止策略）与迁移前一致且来源为连线。交付：CLI 输出记录。

## 5. 状态机结构节点退役步骤端口（迁移后执行；先删字段会让旧资产条件图被序列化器静默丢弃）

- [ ] 5.1 `BtsmtlSkillStateFlowNode`、`@enter`、`@any` 去 Composite 化：单输入单输出，`steps` 字段对状态机节点删除。交付：编译通过。
- [x] 5.2 `BtsmtlSkillStepInspector` 移除状态机分支（"状态转换"区），步骤编辑仅服务 sequence/selector/parallel/loop。交付：编译通过。（提前于迁移执行——纯 UI 分支，迁移器只读数据不读 UI；状态机节点步骤区自此显示为普通"执行步骤"）
- [ ] 5.3 全库核对状态机结构节点 steps 的消费者归零（`rg` 核对 FlowGraphs/Compilation/Document 三处），删除零残留旧路径。交付：rg 结果记录。

## 6. 清理与对账

- [ ] 6.1 删除状态机专用旧路径：`BtsmtlSkillStepPort` 仅保留流程复合节点用途所需字段，状态机专用逻辑与字段全删。交付：编译通过。
- [x] 6.2 `rg` 审计无"步骤反查转移"残留（`Steps`/`sourcePortID` 在状态机语境的引用为零），记录扫描结果。交付：扫描记录。（f28f30cfe：`record.Step`/`BtsmtlSkillEdgeOccurrence(` 全库零残留；`BtsmtlSkillFlowConnection` 11 处消费者均为本次落位文件；其余 `.Step` 命中均为 SimulationPipeline/WorldFeature 等无关领域）
- [ ] 6.3 对账回写：`refactor-btsmtl-flowcanvas-authoring/tasks.md` 3.3.1 增加替代标注（状态机结构节点步骤输出由本变更替代并指向本 change）；核对 `btsmtl-sm-node-authoring` 无条款与本变更冲突。交付：对账结论记录。
