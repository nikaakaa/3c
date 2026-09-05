## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统当前已安装的作者包继续遵守`btsmtl-agent-authoring-document.v4`，但 Camera 迁移 MUST把已确认的 v4 Camera 语义完整迁入最终`btsmtl-agent-authoring-document.v5`，不得为了过渡先建立一套 v4 Camera 分片。v5实际接口、schema、codec和owner未提交前，本变更只记录依赖和边界，不得把v5代码或包写成current truth。v5 owner MUST继续是唯一整包Document的装配根，Camera domain不得创建第二个Document包。

#### Scenario: 当前v4包中出现Camera目标

- **WHEN** 当前系统对已有Character执行checkout或读取v4工作包
- **THEN** service MUST保持现有v4 current truth和五个生命周期工具语义
- **AND** Camera迁移 MUST只产生面向最终v5的语义对账，不得发布一套临时v4 Camera包或第二个schema

#### Scenario: BTSMTL v5接口尚未提交

- **WHEN** Camera实现需要v5 owner、codec或Mutation接口而实际BTSMTL提交尚不存在
- **THEN** Camera流程 MUST报告精确的未安装依赖并保持未完成
- **AND** MUST不写占位类型、桥接签名、兼容reader或伪造可运行的v5包

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Graph、StateMachine、Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish

### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

v5 owner MUST统一拥有manifest、schema、整包文件闭包、editable/context分区、sync基线、hash、事务和反向发布。Camera domain继续拥有Camera Profile、Sequence、Effect、Curve、结构化对象引用语义和typed Camera Mutation的业务定义，但不得拥有manifest装配、整包codec、整包hash或第二套事务入口。Camera分片只能出现在Character domain的完整Document闭包中，AI domain MUST拒绝Camera可写分片；场景Transform、物理世界、原dump、生成Camera计划和Runtime状态继续只读或不可写。

#### Scenario: v5 owner装配Camera目标

- **WHEN** Character Document同时包含Camera、Gameplay、Timeline和Presentation目标
- **THEN** v5 owner MUST按同一manifest、document hash和事务组装完整包
- **AND** Camera domain MUST只提供自己的typed语义和Mutation lowering，不得自行写manifest或发布第二个package

#### Scenario: AI domain提交Camera分片

- **WHEN** AI domain的editable目录包含Camera Profile、Effect或Camera Curve目标
- **THEN** strict parser或Validator MUST拒绝该分片并定位domain与文件路径
- **AND** MUST不把Camera字段降级为AI自定义字典

#### Scenario: AI读取Character文档包

- **WHEN** checkout导出Character Controller
- **THEN** editable MUST表达Agent正式可写的Graph、StateMachine、Condition、Timeline、Blackboard、Action、Presentation与Clip Curve结构
- **AND** context MUST只读表达Node/Graph schema、可引用asset、dependency与必要能力摘要
- **AND** 文档包 MUST不暴露Unity YAML、managed-reference布局或私有SerializedProperty path

#### Scenario: AI尝试修改只读context

- **WHEN** context文件semantic hash与checkout基线不同
- **THEN** parser或Reconciler MUST返回`readonly_context_modified`
- **AND** MUST不把变化降低为Mutation

### Requirement: 可编辑能力必须由唯一authoring capability catalog闭合

Camera作者能力 MUST由同一能力目录向人工编辑、Compiler、v5 owner、strict codec、Reconciler、Mutation preflight和Validator提供字段、单位、坐标/时间域、枚举、结构化引用、SkillProgram/技能局部Graph/TreeClip/Timeline producer和source mapping。Camera domain继续拥有Profile/Sequence/Effect/Curve语义和typed Mutation；v5 owner负责把这些能力装配进完整Document。SkillProgram Root、技能局部Graph和TreeClip/Timeline是Camera producer的唯一作者入口；C# Locomotion控制拓扑不得提供Camera图节点或平行控制图。已提交PresentationCommand、ActionInstance来源、producer generation和新的SourceMap是Camera运行消费边界。未知字段、旧v4 Camera字段、任意字符串资源引用或未注册入口 MUST严格失败。

#### Scenario: SkillProgram触发Camera请求

- **WHEN** SkillProgram Root、技能局部Graph或TreeClip/Timeline声明Camera producer
- **THEN** compiler MUST保留精确PresentationCommand、ActionInstance来源、producer generation和SourceMap
- **AND** MUST不从C# Locomotion拓扑创建Camera节点，也不按显示名或目录寻找资源

#### Scenario: Camera domain试图自行装配Document

- **WHEN** Camera importer或editor尝试直接创建manifest、package codec或独立Mutation service
- **THEN** preflight MUST拒绝该路径并要求交给v5 owner的整包事务
- **AND** MUST不保留v4/v5双写或第二条apply链

#### Scenario: Exporter发现未登记Node类型

- **WHEN** 当前正式Graph包含一个未形成完整capability descriptor的可写Node
- **THEN** checkout MUST报告Node identity、Graph identity与缺失能力
- **AND** MUST不把C# type name直接写入editable作为绕过

### Requirement: Document v4必须原子替代v3

当前已安装链仍严格拒绝v3及更早schema；Camera迁移不得借此增加v4 Camera兼容路径。待BTSMTL v5实际接口提交后，v5 owner MUST以同一整包事务、唯一codec和唯一Mutation链原子替代当前v4，旧v4工作包 MUST要求显式重新checkout，不得通过Camera专用reader、兼容字段或双schema静默迁移。过渡期间v4仍是current truth，不得宣称v5已安装。

#### Scenario: Camera v4工作包尝试继续发布

- **WHEN** Camera目标只存在于临时v4扩展或旧Camera reader中
- **THEN** dry-run MUST拒绝该路径并要求按最终v5 owner重新checkout/装配
- **AND** MUST不生成半套Camera Projection或保留双codec

#### Scenario: v5整包替代成功

- **WHEN** v5 owner、Camera domain能力、Reconciler、Mutation、Validator和反向发布全部实际提交并通过整包hash校验
- **THEN** 系统 MUST只保留v5 schema、codec和Mutation调用链
- **AND** MUST删除旧Camera v4 reader、兼容字段和并行apply路径

#### Scenario: 读取v3文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v3`
- **THEN** dry-run与apply MUST拒绝该文档且不修改资产
- **AND** 调用方 MUST显式重新checkout

### Requirement: Document v4失败恢复必须同时覆盖Unity owner与正式package

在v5实际接口提交后，整包Application Service MUST锁定全部Gameplay、Timeline、Presentation、Camera和真实资源owner，并由v5 owner拥有唯一Undo、rollback、Save、反向export和package原子替换。Camera Mutation失败、共享资源冲突、Validator失败或package发布失败 MUST恢复同一事务内全部owner；Camera domain不得缩小事务范围，也不得在v4过渡包中单独报告Clean。

#### Scenario: Camera与Timeline共同Mutation失败

- **WHEN** Camera资源创建成功但Timeline引用或Presentation校验失败
- **THEN** v5 owner MUST回滚Camera、Timeline、Gameplay和同包Presentation owner
- **AND** 正式Document MUST保持apply前内容且不得报告Clean

#### Scenario: Clip Curve Validator失败

- **WHEN** Gameplay和Timeline mutation已经执行，但Clip Curve Validator发现Phase非单调
- **THEN** Application Service MUST回滚同一事务内全部Gameplay、Timeline、Clip与Presentation owner
- **AND** 正式Document package MUST保持apply前内容且响应不得报告`Clean`

## ADDED Requirements

### Requirement: Preview会话必须由ScenePlay owner统一管理

`rebuild-btsmtl-preview-with-scene-play` MUST拥有Preview会话、独立命令源、可复用ScenePlay fixture、seek重建、物理输出租约和会话清理。Camera只提供正式Runtime、Projection、Rig/目标/物理绑定、Reset和只读诊断；在共享接口实际提交前，Camera change不得写Preview桥接或占位签名。Timeline游标只定位作者内容或观察历史，Gameplay状态变化走正式运行或受控试验重建，seek不得直接修改Simulation或形成第二角色执行链。

#### Scenario: ScenePlay预览相机与动作

- **WHEN** ScenePlay owner用明确fixture播放包含Camera producer的SkillProgram/TreeClip/Timeline内容
- **THEN** Camera MUST按统一owner提供的Projection、Body/目标/输入和物理绑定执行正式Runtime
- **AND** Camera MUST不创建第二会话、第二命令源或第二角色执行链

#### Scenario: Timeline游标seek

- **WHEN** 作者在Timeline中跳转到另一个时间点
- **THEN** ScenePlay owner MUST从初始状态或受控检查点重建正式运行历史
- **AND** Timeline cursor MUST不直接改Simulation，Camera只响应Reset和逐帧输入并发布只读结果
