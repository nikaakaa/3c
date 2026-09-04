## Purpose

定义角色动画片段自带 BlendShape 的正式素材、绑定、采样、混合和最终写入合同，使表情与骨骼共享同一播放时间、Pose Plan 和表现帧提交，保留控制曲线与骨骼混合的明确分工，避免另建表情播放器或图外 Renderer 写入。

## ADDED Requirements

### Requirement: 片段属性必须与正式素材和模型建立完整绑定

本能力 MUST 只接入正式动画片段自带的连续 BlendShape 曲线。正式 AnimationClip MUST 是属性曲线唯一 authoring 真相；Profile MUST 将 Graph 声明的稳定参数身份映射到明确 Renderer binding、Mesh 内容身份和 BlendShape。Build MUST 固定 dense 参数/属性索引、目标索引、单位、默认值、曲线存在性、正式时间范围及源 revision，Runtime MUST 不按显示名、层级搜索或哈希碰撞猜测目标。

原始数据导入 MUST 使用明确的 source identity 与完整解码记录，MUST 不按文件名合并不同 CAB/PathId。导入只更新本次声明的属性曲线；已有骨骼、Foot/Phase 曲线与作者修订 MUST 保持，冲突 MUST 返回明确结果而不得覆盖后继续。

#### Scenario: 发布一条已解码表情曲线

- **WHEN** 原始轨道、目标 Clip、模型形变和时间范围都有匹配且完整的身份
- **THEN** 显式导入 MUST 发布对应 AnimationClip 属性曲线及来源/质量记录
- **AND** 后续 Build MUST 从该正式 Clip 生成运行资源，不读取研究目录作为运行输入

#### Scenario: 当前Mesh不再包含对应形变

- **WHEN** Mesh 内容 revision 或 BlendShape 映射与编译绑定不一致
- **THEN** 构建或 Actor 装配 MUST Invalid，并定位 Renderer 与属性身份
- **AND** MUST 不按旧索引、相近名称或默认模型继续

### Requirement: 属性值域和缺轨道行为必须由正式合同声明

BlendShape 参数 MUST 显式保存单位和默认值。源值到目标单位的转换 MUST 由导入记录证明并纳入资源 hash，MUST 不无条件乘 100、限制为 0–1 或裁掉负值。合法源曲线没有动画某个已声明属性时，Build MUST 将该通道明确编译为使用声明默认值的常量，并正常参与本次片段混合；声明有轨道却缺失数据、解码失败或绑定不明 MUST Invalid，不得改成默认值。

#### Scenario: 一个完整片段不动画某个面部属性

- **WHEN** 源绑定清单确认该属性没有曲线，且项目为它声明了默认值
- **THEN** Build MUST 发布明确的常量通道，该 source 每次采样 MUST 输出相同默认值
- **AND** MUST 不沿用上一动作遗留的表情或把未解码轨道标成合法缺省

#### Scenario: 原始值超出常见权重范围

- **WHEN** 原始解码包含有限负值或大于常见范围的数值
- **THEN** 导入 MUST 保留原值并依据明确模型单位完成验证
- **AND** MUST 不以统一 clamp 隐藏数值或模型绑定问题

### Requirement: 骨骼和属性必须形成同一source采样结果

同一 source 的骨骼与属性 MUST 共用 Program 已确定的 effective time、loop、source-local clip weight、资源 generation 和表现 Frame。准备 MUST 只在全部必须数据可用时返回 Ready；属性未就绪 MUST 使完整 source Pending，非法属性 MUST 使完整 source Invalid。Runtime MUST 复用同一 Player 与 Source 生命周期，不单独推进表情时钟或建立第二个播放器。

#### Scenario: 骨骼已就绪但表情payload仍在加载

- **WHEN** 该 source 的必须属性尚未完成准备
- **THEN** 完整 source MUST 保持 Pending，并执行既有候选 target readiness 规则
- **AND** MUST 不先提交新骨骼、再沿用上一帧表情

#### Scenario: 过渡同时使用NativeClip与ACL

- **WHEN** 已编译的过渡同时采样两个不同 backend 的合法 source
- **THEN** 两者 MUST 发布同一 typed 骨骼/参数合同，由既有 Program 计算过渡和生命周期
- **AND** 属性值 MUST 不因 backend 不同而走另一套时间或混合规则

### Requirement: 属性混合必须复用既有参数规则并与骨骼Mask分离

动画属性 MUST 随现有 typed Pose Parameter 页传播。Blend Space MUST 使用同一批 effective sample 与 source-local 权重；状态 Standard Blend MUST 使用该状态过渡的全局权重；BlendStack MUST 使用其 scalar contribution 权重。Layered Bone Blend 与 Additive MUST 保持现有 Base 参数传播，MUST 不把逐骨骼 Mask 或逐骨骼 blend profile 再乘到属性值上。需要改变参数来源时 MUST 通过现有 Parameter Resolve 的明确 Base、Overlay、Weighted、Max 或 Min 策略，MUST 不在末端自动插入曲线修补或隐藏的覆盖分支。

现有 Inertialization 和 reset MUST 沿用其参数响应与历史所有权，不增加 ACL 私有属性滤波。表情参数 MUST 不因为属于数值就成为 Foot/Phase 或 Gameplay 控制曲线；本次 MUST 不自动增加眨眼、说话、表情状态机或另一套优先级系统。

#### Scenario: 手臂骨骼Mask发生改变

- **WHEN** 作者只修改骨骼分层节点的手臂 Mask，参数来源与解析策略未变
- **THEN** 该节点的属性输出 MUST 保持原 Base 参数语义
- **AND** MUST 不因手臂混合力度变化再次削弱表情或控制曲线数值

#### Scenario: 动作从无闭眼渐变到闭眼

- **WHEN** 两个完整 source 的闭眼值分别为 0 和 100，Standard Blend 的 target 全局权重为 0.25
- **THEN** 该过渡的闭眼参数 MUST 为 25
- **AND** MUST 不再乘一次手臂、头部或其它骨骼的 Mask 权重

### Requirement: 唯一Final Publication必须共同发布骨骼和属性

最终属性值 MUST 进入现有 Final Publication 的同 lineage Pending 结果，MUST 不形成独立提交者或第二最终 Pose。唯一发布 owner MUST 在首次可见写入前验证全部骨骼和属性结果、Renderer/Mesh/目标索引、容量、Frame、generation、completion 与有限值；合法后按固定顺序写入骨骼和 BlendShape，再统一 Seal。

Source、decoder、Preview 和 Animancer 图输出 MUST 不直接改变所管理 Renderer 的最终 BlendShape。Source Graph 的存在只为采样与 capture，不拥有可见属性发布。属性或骨骼任一预验证失败时 MUST 不执行该次最终写入；Barrier 前按现有规则 Discard，Barrier 内或之后按现有规则 Fault。发生不可预期的底层写入异常时 MUST 报告 Fault，不得声称能够自动回滚已写入对象后继续运行。

#### Scenario: 当前Frame的骨骼与属性全部完整

- **WHEN** 两部分结果及全部目标绑定匹配同一表现帧
- **THEN** Final Publication MUST 在同一发布流程写入骨骼和 BlendShape，并只提交一次结果
- **AND** Writer 开始后 MUST 不再做动画选择、混合、资源加载或可能失败的业务计算

#### Scenario: 属性目标在发布前失效

- **WHEN** 任一声明 Renderer 或 Mesh binding 在整体预验证时失效
- **THEN** 本次最终骨骼和属性写入 MUST 一起被阻止
- **AND** MUST 不只提交骨骼、只丢弃表情或启用第二 Renderer writer

### Requirement: 属性能力必须与发布包和诊断形成同一闭包

Build MUST 将属性 schema、参数布局、默认值、模型 binding、源依赖、backend、资源 hash 和容量纳入同一不可变 Projection/Program 身份。旧 schema 产物 MUST 重建而不得兼容解释。Runtime 与所有 Preview MUST 消费同一产物及发布规则。诊断 MUST 只读取已 Seal 的属性采样/混合/写入事实；关闭诊断 MUST 不改变数值、资源驻留或 release。

#### Scenario: 属性绑定修改后加载旧Projection

- **WHEN** 正式模型或属性合同已变，而运行包仍引用旧 layout/hash
- **THEN** 装配 MUST 拒绝该组合并要求完整重建
- **AND** MUST 不现场补参数、按名称重新映射或沿用旧表情页
