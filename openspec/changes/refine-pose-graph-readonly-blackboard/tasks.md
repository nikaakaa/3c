# PoseGraph只读Blackboard任务

## 1. 只读输入与作用范围

- [ ] 1.1 定义Graph输入的正式分类、声明owner、消费者和可访问范围，分清动画实例变量、只读表现事实、Pose曲线和节点/资源配置。
- [ ] 1.2 根据EventGraph正式规划确定PoseGraph变量读取接口，绑定同一稳定变量身份、类型和实例值来源；缺少正式接口时明确记录依赖，不自建兼容布局或更新器。
- [ ] 1.3 定义Root、StatePose、普通Subgraph与Linked Pose入口的可访问输入和公开参数规则，区分外部变量读取与子图调用参数，不把根图声明复制到全部子图。
- [ ] 1.4 让FlowCanvas原生Blackboard投影当前图可访问的正式输入，显示名称、类型、来源、作用范围和使用情况；合法未使用声明保持可见或可筛选，不因暂未连线而禁止使用。
- [ ] 1.5 让拖拽只创建绑定正确声明的Get，禁止PoseGraph主图创建共享动画变量Set；编辑绑定和引用继续使用唯一typed Mutation。
- [ ] 1.6 移除动画属性导入器向每张PoseGraph复制BlendShape等声明的路径，改由正式曲线/资源合同提供编译所需完整曲线清单，保持最终属性写入消费者。
- [ ] 1.7 区分动画输入读取和指定输入Pose的曲线读取，补齐类型、来源、作用范围、Stage依赖和缺失数据诊断；停止依靠清空root.Parameters缩减运行时数据布局。
- [ ] 1.8 为Body内部FootPlacement权重建立明确曲线绑定或公开输入合同；保留已有曲线混合、惯性响应与Foot权重作用，迁移完成后删除不再需要的根图Get和透传端口。
- [ ] 1.9 在Blackboard、Get、子图入口和Slot主要显示区域使用作者名称，内部稳定ID只用于引用与详情诊断，重命名不破坏连接。
- [ ] 1.10 通过正式Document/Mutation删除确认无引用的重复子图与废弃声明；存在用户改动冲突时保留现场并交由用户决策。
- [ ] 1.11 同步共享Capability、Document字段投影、Exporter、Reconciler、Mutation、Validator、Compiler source map和只读观察，使人工编辑与Agent使用同一正式语义。
- [ ] 1.12 更新对应spec与项目当前状态，明确EventGraph接口、曲线传播与只读消费边界的实际交付范围，移除迁移后的旧入口和过期说明，不把未交付能力写成current truth。
