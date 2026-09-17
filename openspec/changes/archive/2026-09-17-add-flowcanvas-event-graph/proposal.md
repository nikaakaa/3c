## Why

用户明确确认参考UE动画蓝图的作者分工：现有动画层的计算、判断和变量更新尽量在EventGraph中表达，C#负责正式数据接入和底层执行，PoseGraph读取结果计算姿势。本提案必须完成真实的既有逻辑迁移，不能以清理输入、移除空图或暂时找不到业务为由结束。

本版替代上一版“MotionPhase继续留在C#、Corin可能无需事件图”的收窄方向。迁移当前已有速度/方向/加速度/朝向误差及运动阶段计算，保持原公式、阈值和动作表现，不新增步频、播放倍率、平滑或新的状态策略。

## What Changes

- 参考UE的事件更新、动画变量和AnimGraph消费分工，保留FlowCanvas原生runtime；不复制UE虚拟机，不增加事件图Compiler，也不把节点相关的Pose求值和底层调度搬进全局事件图。
- 将当前FactProjector中动画专用派生量迁入原生图：水平速度、垂直速度、移动方向、期望方向、水平加速度、朝向误差、MotionPhase。原实现、公式、历史和消费者去向在design中逐项固定。
- C#只提供对齐后的Body/Intent原始观测、已提交状态、时钟和身份；保留Grounded、MovementMode等既有Gameplay事实。输出动画变量不反写Gameplay，也不回填FactFrame作为兼容读取。
- **BREAKING**：取消动画派生量作为外部Fact与EventGraph变量同时生产的路径。实例历史迁到同一原生图状态，Pose/条件/RootOrientationWarp/MM等原消费者通过唯一变量合同读取原值；不改变其算法和决策。
- 补齐迁移必需的向量、旋转只读输入及MotionPhase枚举类型，保持Float/Int32/Bool精确交接。原生变量、配置、导出、帧和Pose绑定必须使用同一类型与声明，不以Float或不透明object模拟其它类型。
- Corin事件图必须有正式输入、计算、Set和执行连接，并至少把MotionPhase接入现有Pose预测状态选择、将朝向误差等接回原消费者；保留原MovementMode状态规则，不换成新速度规则。空Start/Update图或Profile绑定不构成完成。
- 保留公共C# authoring的export_code/generate_assets、直接API、完整输出和明确根绑定。Corin recipe改为表达真实既有逻辑，不删除整个接入目标；旧Document和motor桥保持退役。
- 原无变量需求的其它动画根可以按正式合同不装配事件宿主，但这不免除Corin现有动画更新逻辑的迁移，不作为本次退出条件。
- 不新增测试或验证任务；任务只列接口、图内容、消费者迁移、删除和文档工作，用户自行端到端验证。

## Capabilities

### New Capabilities

- flowcanvas-event-graph：复用原生事件、typed变量和作者API，承载真实动画更新内容与公共C#输出。
- character-animation-event-graph：UE式动画更新分工、现有派生量的明确迁移、实例历史、类型和Pose消费合同。

### Modified Capabilities

- graph-authoring-domain-framework：保持原生EventGraph与编译Pose/Skill的执行边界和正式作者入口。
- character-animation-pipeline：原始Fact接入 → 一次原生动画更新 → 唯一变量帧 → 既有Pose/动画消费者；移除旧派生Fact生产与消费。

## Impact

- 本任务：Host输入节点与类型、原生变量/帧、动画宿主、CorinAnimationEventGraphAuthoringCode及真实图内容、事件图输出薄适配。
- Pose/Presentation任务：CharacterPresentationFactFrame/Projector拆分原始接入与派生字段，CharacterAnimationInputContract及Pose/条件/RootOrientationWarp/MM读取、静态绑定和作者输入视图。职责不因本次更新而交叉写同一文件。
- C# authoring任务：公共代码输出/生成入口和基础表达能力；本任务只补事件图领域薄适配，不另建输出器或MCP。
- 现有MovementMode、Action/Slot、曲线、Foot/IK、Pose时钟/混合和最终Writer保持原职责；只更换已确定派生值的唯一生产/读取来源。
- 当前文档依据用户在已授权实施过程中确认的UE方向修订；更新后向原实现窗口发送一次正式文档指针，替代上一版取消Corin事件图内容的实施方向。
