# Design: PoseGraph只读Blackboard与输入范围

本设计由原refactor-character-pose-graph-architecture的Decision 26整体分离。当前只实施本change的12项任务；不承接旧change其余架构待办，不修改事件图规划窗口拥有的文档。

## 1. PoseGraph只读输入与曲线作用范围

状态：待实施。本节不是已归档运行基础的一部分。对应任务1.1—1.12；EventGraph规划窗口维护变量声明、更新、Set和事件执行方案，本change只维护PoseGraph消费侧的完整要求。

### 输入输出与所有权

| 数据类别 | 正式来源与作用范围 | PoseGraph作者入口 |
|---|---|---|
| 动画实例变量 | EventGraph正式变量合同与当前动画实例；具体schema和帧交接由事件图规划确定 | 只读Get，同一声明，不复制值或变量布局 |
| 角色表现事实 | 已有Presentation Fact合同与同帧只读数据 | 只读引用；不提供Gameplay写入或Set |
| 姿势曲线与动画属性 | 指定输入Pose的source-local采样结果，按上游正式混合和惯性政策传播 | 曲线选择/绑定或正式曲线读取，不复制为全图共享变量 |
| 固定配置与资源 | 节点typed字段、Graph抽象Slot与Profile Binding | 原生Inspector与正式资源绑定，不进入变量列表 |

输入是正式声明、可访问范围、调用接口和资源/曲线合同；输出是原生Blackboard投影、稳定Get引用、曲线绑定及编译后的typed读取。PoseGraph仍负责状态机、播放器、混合与IK，普通共享变量只读并不意味着节点没有自己的运行状态。

### 可见范围

Root、StatePose、Subgraph和Linked Pose入口只列当前图可访问的声明。公开调用参数由子图接口显式提供；共享实例变量按唯一合同读取，不在每个子图重新声明。合法但尚未连线的声明允许展示并标记未使用，列表筛选不改变正式数据；不能把根图必须为空当成完成条件。

Graph/Node/Parameter/Slot内部identity继续稳定。主要显示名称、类型、来源与范围；稳定ID放入详情和诊断。重命名只改变作者名称，Get和连接继续绑定原声明身份。

### Body与Foot曲线

Body内部明确FootPlacement Weight来自哪个输入Pose的哪条曲线，使用经过上游采样、Blend、Slot与适用Inertialization处理后的同一值。数据来自角色外部控制时才声明公开输入；不根据节点显示名或固定参数字符串猜测来源。

节点属性选择曲线的方案减少连线，适合直接使用现成权重；通用曲线读取节点可以把额外计算明确画出，适合需要组合或缩放的作者操作。两者是作者表达方式取舍，不能各自建立曲线采样和运行真相。具体采用方式与事件图共享值合同一致后确定。

迁移先接通正式读取，再删除原根图Foot Weight Get与不再需要的Body透传端口。脚权重继续只控制既有Goal可见权重，不改变Anchor、连续历史、Landing Reach、求解顺序或Gameplay事实。BlendShape继续由唯一最终属性写入器消费，删除Blackboard投影不等于删除运行曲线。

### 编译、文档与迁移

编译必须分别解析变量、表现事实和输入Pose曲线的身份、类型、作用范围与求值依赖。现有root.Parameters汇集方式必须在迁移声明前改为完整收集正式输入与曲线依赖；不能先清空根参数再用缺失默认值补偿。

人工UI和Document仍由同一Capability、typed Mutation、Exporter/Reconciler与Validator表达；来源变化不得仅隐藏UI而留下旧复制路径。Graph Catalog与子资产清理必须按正式对象引用执行，只删除确认无引用的重复对象；用户未提交修改与本方案冲突时保持现场，不选择性覆盖。

当前spec要求稳定ParameterId、类型、默认值、允许来源和source-local曲线传播，本节沿用这些边界并补足作者可见范围。project.md现行只读Blackboard描述的是当前实现，不能据此把EventGraph作者变量的Set一并禁止。事件图合同与当前代码不一致时必须明确标记待迁移，不得写成已完成。

### 完成定义

作者能从当前图可访问输入中拖出正确Get、区分变量与曲线、进入Body追踪权重来源；重命名不破坏引用。编译产物使用唯一变量与曲线来源，旧属性复制、废弃Get/端口和确认无引用的重复对象已删除。本change执行记录放在execution.md；原验证与历史限制保留于旧change的verification-history.md，不列入tasks.md。
