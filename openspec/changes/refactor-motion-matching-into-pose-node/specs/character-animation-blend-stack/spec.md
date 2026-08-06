# character-animation-blend-stack Specification

## RENAMED Requirements

- FROM: `### Requirement: 每个显式Blend Stack节点必须拥有唯一有序状态`
- TO: `### Requirement: 每个Blend Stack owner必须拥有唯一有序状态`
- FROM: `### Requirement: Blend Stack节点必须由固定Animation Job输出统一Pose Value`
- TO: `### Requirement: Blend Stack Kernel必须由固定Animation Job输出统一Pose Value`
- FROM: `### Requirement: Node-local Blend Policy必须是该节点唯一转场权威`
- TO: `### Requirement: Owner-local Blend Policy必须是该owner唯一转场权威`

## MODIFIED Requirements

### Requirement: 每个Blend Stack owner必须拥有唯一有序状态

系统 MUST提供统一`CharacterAnimationBlendStackKernel`，并要求每个调用owner拥有独立、有序、固定容量的entry state、generation、clock、Stored Pose和source usage。Motion Matching的owner MUST是具体`MotionMatchingPose`节点实例。系统 MUST删除当前没有正式资产消费者的显式MM BlendStack作者节点及其`CharacterMotionMatchingPoseSourceSlot`输入；Kernel MUST不成为自主节点、全局service或第二MM运行入口。

#### Scenario: 两个MM节点使用Kernel

- **WHEN** 同一Actor的两个MotionMatchingPose节点都存在active entries
- **THEN** Kernel MUST使用两个不同owner workspace求值
- **AND** entry generation、Stored Pose和release MUST不跨owner共享

### Requirement: CrossFade必须使用独立Clock、Curve与每骨骼规范化Weight

每次Jump创建的entry MUST保存自己的clock、curve identity、duration和per-bone Blend Profile引用。Kernel MUST在同一owner内对所有live entry与Stored Pose贡献按骨骼规范化，并 MUST以明确frame delta推进clock。Kernel MUST不读取Unity Time、不使用Animancer transition权重，也 MUST不从上一owner或全局默认值补齐缺失curve。

#### Scenario: 三个entry重叠

- **WHEN** owner内三个entry在同一帧具有非零贡献
- **THEN** Kernel MUST按每骨骼计算三个贡献的规范化权重
- **AND** 每个骨骼的总贡献 MUST与正式Pose Value合同一致

### Requirement: Blend Stack必须只发布source usage而不得拥有Marker Sync

Blend Stack owner MUST按实际entry和Stored Pose贡献发布source usage、retention与release。Kernel MUST不拥有Marker Sync、数据库搜索、Chooser、Action lifecycle或Gameplay状态。MM source的时间计划由MotionMatchingPose selection plan提供；Kernel只消费已完成的entry source pose与clock。

#### Scenario: MM entry仍被Stored Pose引用

- **WHEN** live entry已压缩但其Pose贡献存在于Stored Pose
- **THEN** owner MUST按Stored Pose合同保持所需Pose页或已完成捕获
- **AND** MUST不让Kernel创建Marker Sync状态

### Requirement: Blend Stack容量必须通过Stored Pose连续压缩

Projection Build MUST为每个owner生成明确最大live entries与Stored Pose pages。达到容量时，Kernel MUST把最老且仍有贡献的entries按当前每骨骼贡献压缩为一个完整Stored Pose，再加入新entry。Kernel MUST不丢弃最老entry、不缩短fade、不扩容，也 MUST不调用外部Inertialization替代容量处理。

#### Scenario: 容量压缩

- **WHEN** 新Jump到达且固定entry slots已满
- **THEN** Kernel MUST先形成与压缩前总贡献等价的Stored Pose
- **AND** 新entry MUST使用腾出的正式slot进入

### Requirement: Per-Bone Blend Profile必须依赖稳定Rig Identity

每个Blend Policy和per-bone Blend Profile MUST绑定与owner Pose完全相同的RigId与Revision，并覆盖全部Pose slots。MM节点、Database Artifact、entry source Pose、Stored Pose和Blend Profile任一Rig lineage不一致 MUST使Build或当前帧失败。系统 MUST不按骨骼名称迁移权重或填充默认权重。

#### Scenario: Rig revision变化

- **WHEN** Presentation Rig revision升级而Blend Profile仍引用旧revision
- **THEN** Projection Build MUST拒绝MM节点
- **AND** MUST要求作者显式重建或更新正式资产

### Requirement: Animancer必须只作为Source Pose采样后端

Animancer MUST只根据owner给出的source identity与sample time产生Pose page。Blend clock、curve、per-bone权重、Stored Pose和release MUST由统一Kernel和owner处理。系统 MUST不同时运行Animancer transition与Kernel crossfade处理同一次MM Jump。

#### Scenario: MM Jump采样新旧source

- **WHEN** 两个entry都有非零权重
- **THEN** Animancer MUST分别提供两个source Pose
- **AND** 固定Animation Job MUST完成唯一混合

### Requirement: Blend Stack Kernel必须由固定Animation Job输出统一Pose Value

Projection Build MUST把owner容量、Rig布局、Blend Profile、entry program输出页和Stored Pose页编入固定Animation Job。Job MUST输出完整Local Pose Value及completion、owner identity、frame identity和Rig lineage。Job MUST不遍历authoring资产、不调用搜索、不创建Playable层或写Physical Transform。

#### Scenario: Job完成MM内部混合

- **WHEN** 全部非零entry Pose和权重页均完成
- **THEN** Job MUST输出一个完整MM Local Pose Value
- **AND** 下游 MUST不需要知道entry数量或source类型

### Requirement: Owner-local Blend Policy必须是该owner唯一转场权威

每个Blend Stack owner MUST保存完整Blend Policy identity。对MotionMatchingPose，policy MUST只控制该节点内部Jump；PoseStateMachine与AnimationSlot各自控制不同业务边界。Compiler MUST拒绝缺失policy、跨Rig policy或同一Jump同时连接外部BlendStack/Inertialization的图。

#### Scenario: 配置重复MM transition

- **WHEN** 作者在MM节点下游增加只为同一Jump服务的Inertialization
- **THEN** Validator MUST报告重复continuity owner
- **AND** Compiler MUST不生成两次淡入

### Requirement: Pose Value必须完整表达Stack输出

Blend Stack输出 MUST是包含全部Rig Pose slots、root sample、completion、owner identity、frame identity和Rig lineage的正式Pose Value。部分entry失败、权重页未完成、Stored Pose lineage错误或总权重非法 MUST使输出Invalid；Kernel MUST不移除失败entry后重归一化剩余Pose。

#### Scenario: 一个entry Pose未完成

- **WHEN** 非零权重entry没有完成Pose page
- **THEN** owner输出 MUST为Invalid
- **AND** MUST不只混合其它entries

### Requirement: Blend Stack调试必须完整解释节点Pose来源

Pose Watch、Preview、Live Debug和Trace MUST按owner identity显示generation、entry source、sample time、entry processing program、clock、curve、per-bone profile、最终权重、Stored Pose、usage与release。诊断 MUST读取正式Kernel页，不得运行shadow blend或把MM internal stack显示成独立作者节点。

#### Scenario: 检查MM Stored Pose

- **WHEN** 调试帧发生容量压缩
- **THEN** 工具 MUST显示被压缩entries、压缩时贡献和Stored Pose lineage
- **AND** Canvas MUST仍把该状态归属到对应MotionMatchingPose

