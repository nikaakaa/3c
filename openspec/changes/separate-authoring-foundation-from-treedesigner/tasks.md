本清单只实施作者合同抽离、消费者引用迁移和旧 TreeDesigner 退出。现有技能执行、变量寻址、生命周期、Pose 原生求值与 Timeline 调度继续由原领域拥有；不以本次整理为由重构这些运行机制。

## 1. 建立作者合同边界

- [ ] 1.0 在修改代码前列出本切片的旧类型、新归属、直接消费者、序列化引用和受影响资产；标出与当前工作区其他修改重叠的位置
- [ ] 1.1 按 design.md 的依赖表建立共享作者合同与黑板合同目录，迁出字段、类型、逻辑端口、scope/lifetime、InputBinding 和 fact projection 的有效定义
- [ ] 1.2 将共享合同从 TreeDesigner、FlowCanvas、NodeCanvas 和窗口实现中解耦，原生框架适配留在领域模块，保持 Simulation/Core 与 Timeline runtime 不依赖 Editor
- [ ] 1.3 把 Skill、Pose、Timeline 等领域定义留在各自 owner，改为引用共享合同，不建立总节点注册表或第二套可写 DTO
- [ ] 1.4 将混合文件中的业务合同、框架适配和编辑描述拆成职责独立的类型，删除无消费者的旧 Agent 协议字段

## 2. 保留原生图与端口语义

- [ ] 2.1 保留 Skill FlowGraph/FSM、Pose Graph 和现有 Timeline 唯一可写结构及现有直接读取入口，只删除已确认无消费者的 BaseGraph 转换重载，不重写已接通的原生图编译入口
- [ ] 2.2 把领域逻辑端口、字段和连接约束接入 FlowCanvas/NodeCanvas 适配，保证端口对象不拥有第二份业务规则或运行真值
- [ ] 2.3 统一人工编辑、C# authoring、剪贴板、连接变更和编译准备对同一能力定义的读取，清理按标题、路径或旧 Edge 反推端口的代码
- [ ] 2.4 保持 Skill 编译执行和 Pose 角色原生运行实例的边界，删除失去消费者的旧节点执行适配和整图运行入口

## 3. 分离黑板数据所有权与生命周期

- [ ] 3.1 保留现有分域 Provider 与 Skill Local 的读写边界，只迁移作者声明、面板和编译读取对旧类型的引用，不重写 Provider 实现
- [ ] 3.2 保留原生 VariableId 与项目声明的稳定关联和当前正式编译映射；名字、类型和默认值只由原生 Variable 保存，scope/lifetime 等项目规则由独立声明提供，不增设重复字段
- [ ] 3.3 将现有声明编译与地址解析消费者改为引用迁出的作者合同；保持既有 typed slot/address、owner、generation、provenance 和重置算法，只删除属于旧 BaseExposedProperty 链的 lookup，不清理原生框架的合法变量容器
- [ ] 3.4 将 Data Catalog、Provider picker、ValueNode 创建和 Details 写入统一到真实 owner，删除继承变量复制和 Provider 镜像变量路径
- [ ] 3.5 保留现行 Blackboard fact projection 与 provenance 完整条款及对应实现，包括 Bool/Frame/SyncFact 限制、WindowType/WindowId/Digest、ActionInstance/EventId、不同动作独立 candidate 和 Model Egress 准入边界

## 4. 迁移自研编辑能力

- [ ] 4.1 从旧 TreeDesigner Editor 混合文件中拆出仍被原生编辑器使用的 Details、Navigator、Selection、Clipboard、Undo、资源选择和只读 diagnostics 合同
- [ ] 4.2 将共享编辑集成接到 FlowCanvas/NodeCanvas 原生画布和 Pose GraphEditor，保留领域数据、端口、mutation 与运行诊断的单向装配
- [ ] 4.3 保持 Data/Details 分离、source-aware 筛选、Live Debug 只读边界和显式 Compile/Build/Preview 操作，移除旧固定窗口与重绘触发的重操作
- [ ] 4.4 仅迁移清单中受旧模型或自有类型迁名影响的资源和引用，保持节点参数、连接、默认值及 owner identity；已使用原生图的资产不整图重建

## 5. 退出旧 Timeline 与程序集耦合

- [ ] 5.1 保留现有正式 Timeline 调用身份入口，将仍接收 BaseGraph 的真实消费者接入对应 typed source，删除无消费者 overload；身份按 Prepare、Playback、TreeClip 各阶段需要传递，不新增全字段通用上下文
- [ ] 5.2 保留 Timeline 的时间边界、TreeClip、Marker、Advance/Commit/Discard、快照和 ActionCue 语义，删除 Timeline 对旧图执行器的直接依赖
- [ ] 5.3 按程序集边界更新 Runtime、Editor、Timeline 和领域引用；普通类型保持原序列化身份，本次作者合同类型执行精确旧到新映射
- [ ] 5.4 对无法映射的 managed-reference 资产停止迁移并记录具体资产、字段和缺口，完成已确认资产迁移后删除旧 assembly、空壳类型和兼容链

## 6. 删除旧 TreeDesigner 链路

- [ ] 6.1 在全部正式消费者迁出后删除 TreeWindow、TreeView、GraphView、NodeView、PortView、旧菜单、旧 Browser 和旧 Inspector 打开路径
- [ ] 6.2 删除无消费者的 BaseGraph、BaseTree、BaseTreeAsset、BaseNode、PropertyPort、BaseExposedProperty 执行链、旧解释器、旧编译重载和旧适配器
- [ ] 6.3 清理 TreeDesigner 命名空间、程序集引用、Editor 引用、序列化类型名和孤立资源，保留第三方 FlowCanvas/NodeCanvas 及有效领域资产
- [ ] 6.4 同步主规范 Purpose 和受影响条款，处理与未归档变更的冲突；不替其它任务归档、不覆盖其它任务的正确修改

## 7. 分步提交与交付边界

- [ ] 7.1 按黑板作者合同、节点能力合同、编辑集成、Timeline 调用等完整业务切片提交中文记录；每个切片同时迁移定义、消费者和必要资产并删除对应旧路径，不按技术层分成相互失配的提交
- [ ] 7.2 每个切片开始前确认文件归属与序列化映射，发现与工作区其他修改冲突时停止该切片并提交具体冲突；完成后更新实际迁移清单
- [ ] 7.3 完成后交付新的模块路径、保留/迁移/删除清单和主规范冲突处理结果，代码实现阶段另行启动
