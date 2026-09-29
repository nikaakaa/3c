# native-flowcanvas-pose-runtime Specification

## Purpose
定义作者原生姿态图直接执行时的实例、输入、求值、资源、状态和输出约束，使图编辑与真实运行保持对应关系，并在取消中间编译产物后保留既有动画、混合、IK、预览和失败隔离行为。

## Requirements

### Requirement: Pose必须直接运行原生作者图

系统 MUST通过已安装的 FlowCanvas Runtime 实例化并运行正式 Pose 图、typed 节点、连接和子图引用。系统 MUST不生成或读取 Pose IR、ProgramImage、全图操作表或加载期编译计划。所有运行端口 MUST具有真实类型和执行行为，图／连接／输出不得保留只支持 Editor 的占位实现。

#### Scenario: 合法Pose图进入运行
- **WHEN** 角色加载合法原生 Pose 图及其明确资源绑定
- **THEN** 系统 MUST运行该图的原生节点和连接，并生成最终姿态
- **AND** MUST不要求存在任何旧 Pose Image 或整体角色 Program

### Requirement: 图实例与子图调用必须隔离可变状态

每个角色及需要独立历史的子图调用 MUST拥有独立实例状态；作者资产和共享资源 MUST保持只读。播放时间、状态过渡、惯性化和 IK 历史 MUST只归对应实例，不得跨角色共享或复制成第二份公共状态。

#### Scenario: 同图角色使用不同输入
- **WHEN** 两个角色引用同一图但移动状态和播放时间不同
- **THEN** 两个原生实例 MUST分别计算和保留其状态，互不改变对方结果

### Requirement: Pose必须由正式表现时钟手动驱动

Pose MUST由现有表现宿主在明确的帧和阶段驱动，不得自行使用插件 Update 创建第二时钟。EventGraph MUST先成功发布 typed 变量 Frame；Pose MUST只读该 Frame 与 committed Body／Intent。有限 Action 请求 MUST直接进入原动作播放链，不经事件图转发。

#### Scenario: 一帧更新动画变量并求值Pose
- **WHEN** 表现宿主开始一次合法帧
- **THEN** 事件图 MUST先完成本次变量发布，Pose 再消费同一帧身份的只读输入
- **AND** Pose MUST不得 Set 共享动画实例变量或修改 Gameplay

### Requirement: 共享节点必须在一次求值内产生一致结果

Pose节点 MUST按角色、调用实例、本次求值身份和阶段复用结果。重复读取同一输出 MUST不重复推进时间、状态转换、源采样、Foot／FBBIK 或最终写入。跨帧或不同调用实例 MUST不错误复用旧结果。

#### Scenario: 两个混合分支引用同一Player
- **WHEN** 同一次求值中两个分支读取相同 Player 输出
- **THEN** 两次读取 MUST取得同一合法样本，播放时间只推进一次
- **AND** 下一帧 MUST按新帧身份重新求值

### Requirement: 原生图必须保留空间和全局合法性检查

系统 MUST检查 typed 端口空间、必要输入、唯一最终输出、递归子图、悬空引用、重复 Goal Slot、唯一 Goal Set／FBBIK 及写冲突；错误 MUST定位图、节点、端口或引用链。必要检查 MUST在作者显式校验或实例绑定边界完成，不得因取消编译而放行，也不得重新生成 IR。

#### Scenario: 两个Goal节点写同一目标槽
- **WHEN** 原生图的两个 Goal 贡献声明相同的唯一目标槽
- **THEN** 系统 MUST报告冲突节点和槽并拒绝运行该图，不得按执行顺序覆盖

### Requirement: 原生节点必须保留播放和过渡业务语义

Player、PoseState、Slot、BlendStack、惯性化、Linked Pose、Phase 同步和状态子图 MUST保持其原准入、时间、权重、relevance、历史及 capture／release 语义。源准备与姿态求值 MUST通过同一原生图的明确阶段完成，准备结果 MUST不冒充已完成姿态。不得用默认 Idle、通用插件状态转换或强停旧 source 替代正式过渡。

#### Scenario: 新状态源尚未Ready
- **WHEN** 状态转换条件成立但目标 source 尚未完成正式准备
- **THEN** 系统 MUST按已有 readiness 和过渡合同处理，不得输出未完成源或提前释放仍被显示的旧源

### Requirement: 姿态缓冲与物理资源必须归属明确

输入姿态 MUST只读；会修改姿态的节点 MUST使用自己拥有的输出缓冲。缓冲 MUST按实例复用，正常帧不得为每次端口读取复制整套骨骼或分配整图状态。Source MUST唯一拥有 ACL／Playable，Constraint MUST唯一拥有 Foot／Goal／FBBIK，最终输出 owner MUST唯一写骨骼。取消全图 Worker 调度不得改变算法公式和数值顺序。

#### Scenario: 一个输入姿态用于两个不同控制分支
- **WHEN** 两个节点从同一姿态输入执行不同修改
- **THEN** 各自 MUST产生独立结果，源姿态和另一分支不得被覆盖

### Requirement: 原生Pose必须保留完整帧提交和失败隔离

系统 MUST保留唯一 Animancer Evaluate Barrier、整 Rig 合法性检查、统一提交／丢弃／故障和最终骨骼写入。节点或资源失败 MUST不得发布半帧结果；原生异常不得被吞掉后以默认值继续。图替换／Dispose MUST停止旧实例调用、完成已调度工作并按 owner 释放资源。

#### Scenario: IK节点在输出前失败
- **WHEN** 本次原生求值发生 Constraint 错误
- **THEN** 宿主 MUST按当前阶段的正式失败合同丢弃或进入故障，不得提交部分骨骼或半更新历史

#### Scenario: 替换活动Pose图
- **WHEN** 作者显式替换当前图实例
- **THEN** 系统 MUST清理旧实例和资源并以明确重置语义启动新实例
- **AND** 旧实例晚到结果 MUST不得写入新实例，系统不承诺自动保留旧图历史

### Requirement: 运行预览与观察必须使用同一原生执行路径

正式角色、Pose 预览和 Live Debug MUST复用同一个原生图实例创建与节点执行规则。观察 MUST使用稳定作者身份、调用实例和已完成结果；不得为了调试编译隐藏 Image、建立第二播放器或改变图输入。作者字段、端口和 C# 导出／生成 MUST继续由同一正式领域定义提供。

#### Scenario: 观察运行中的Foot节点
- **WHEN** 作者查看当前角色 Foot 节点的实际输出
- **THEN** UI MUST定位同一作者节点和调用实例的正式已完成结果
- **AND** MUST不再次执行节点、读取半帧状态或改变原求值

### Requirement: Pose必须提供供角色外壳调用的完整帧入口

Pose领域 MUST独立提供原生图／资源准备、实例创建、完整帧执行、停止和已完成观察接口。角色表现外壳 MUST调用完整帧入口并消费最终typed结果，保持同一表现时钟、唯一Animancer Barrier和最终输出边界。source demand准备、姿态求值、Pending检查、提交与丢弃 MUST由Pose内部唯一帧协调点组织；外壳不得重新排列阶段、解释Pose内部操作或复制图状态。实际采用的图版本、实例与ResetGeneration MUST由Pose owner确认，核心只汇集。

#### Scenario: 核心装配已准备的Pose实例
- **WHEN** Pose owner返回合法实例及其实际内容版本
- **THEN** 角色外壳 MUST调用正式完整帧入口并消费typed结果，不创建另一份图或Image
- **AND** Barrier前失败按原规则Discard，Barrier内或之后失败按同一Actor故障归属阻止后续帧；Pose成功后的外围业务提交失败也必须进入该故障归属
