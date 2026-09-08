## Purpose

定义连续参数驱动的动画样本混合能力，包括正式样本资产、有限模式、确定的权重与时间映射、表现资源绑定、编译产物、作者修改和只读观察。该能力复用角色现有姿态与最终输出链，不要求创建独立八向演示，也不替代技能或移动状态。

## ADDED Requirements

### Requirement: 系统必须提供正式Blend Space资产

Blend Space MUST保存稳定资产与样本身份、Rig、模式、轴类型与单位、精确动画引用、样本坐标、时间策略和参数聚合策略。资产 MUST不保存当前运行权重、播放器、世界对象或运行时历史。

#### Scenario: 作者创建一维移动Blend Space

- **WHEN** 作者配置Speed轴及Idle、Walk、Run样本
- **THEN** 各样本 MUST拥有稳定且唯一的SampleId
- **AND** 保存与重载 MUST保持样本身份和配置一致

### Requirement: Blend Space必须使用有限正式模式

正式模式 MUST限定为Linear1D、FreeformCartesian2D与FreeformDirectional2D。未知模式、非法样本或无法求解的配置 MUST明确拒绝，不选择其它模式或最近样本代替。

#### Scenario: 编译未知模式

- **WHEN** 配置不属于正式模式集合
- **THEN** 构建 MUST拒绝并定位资产

### Requirement: 权重求解必须是target-neutral纯计算

权重求解 MUST只消费编译样本数据及有限参数，在预分配空间输出非负、有限、归一化的样本权重。它 MUST不推进播放器、访问作者资产或修改Gameplay状态。Linear1D MUST在相邻样本间线性插值；空结果及非有限值 MUST报告明确失败。

#### Scenario: 求解Linear1D区间值

- **WHEN** Speed位于两个相邻样本之间
- **THEN** 求解 MUST只赋予这两个样本非零权重，权重之和为1

### Requirement: Blend Space时间策略必须显式且确定

资产 MUST显式选择SharedNormalizedPhase或LocomotionPhase。LocomotionPhase MUST使用固定DynamicCycle Phase Reference及同一Profile同步组中的各动画Phase计划，禁止按最大权重切换时间领导者。StationaryPose MUST使用固定采样位置且不得成为Phase Reference。缺少组、参考或曲线 MUST阻止构建，不回退另一种时间策略。

#### Scenario: 参数跨越多个动态样本

- **WHEN** 参数连续变化并改变样本权重
- **THEN** 动态样本 MUST保持同一规范phase，并按各自映射取得采样时间

### Requirement: BlendSpacePlayer必须是显式Pose Graph Player

BlendSpacePlayer MUST通过类型匹配的Graph-owned Source Slot引用Profile-owned唯一Binding，不在节点中保存第二份资源选择。构建 MUST按资产轴合同校验X或X/Y参数、类型和单位，并生成固定source index；Runtime MUST不按名称或目录查找资源。

#### Scenario: 二维节点缺少Y参数

- **WHEN** 二维资产的Player缺少Y输入
- **THEN** 校验 MUST拒绝并定位节点和缺失轴

### Requirement: BlendSpacePlayer与连续性节点必须分责

BlendSpacePlayer MUST只处理同一来源内部的参数权重、时间映射及样本贡献。跨状态过渡、来源释放与惯性残差 MUST继续由现有显式过渡或连续性节点拥有，不自动插入第二播放器、BlendStack或Inertialization。

#### Scenario: BlendSpace source identity变化

- **WHEN** Player的来源或代际发生变化
- **THEN** MUST发布正式不连续事实，并由已配置的连续性节点处理

### Requirement: Blend Space必须编译为固定Projection计划

Projection MUST保存稳定身份、Rig、轴、样本表、权重求解数据、时间映射、资源及分析依赖、参数策略和容量。Runtime MUST只消费身份匹配的不可变计划，不读取Editor曲线、Binding资产或现场构建样本关系。

#### Scenario: Runtime创建BlendSpacePlayer

- **WHEN** 角色加载合法Projection
- **THEN** Player MUST使用对应编译计划与预分配运行空间

### Requirement: Blend Space必须复用正式采样后端

Blend Space MUST把样本权重和有效时间交给角色现有来源采样链。来源后端 MUST由正式资源Binding确定，不得重新决定样本权重或phase，也不得另建最终Pose、IK或物理骨骼写入链。本能力 MUST不强迫已经采用其它正式后端的资源恢复为展开动画播放。

#### Scenario: 三个样本同时贡献

- **WHEN** 求解结果包含三个正权重样本
- **THEN** 正式后端 MUST按该权重与时间采样，并汇入同一角色最终输出

### Requirement: Pose Parameter必须按样本权重显式聚合

样本参数 MUST按RequireAllSamplesWeighted、WeightedAvailableSamples或Unavailable的显式策略聚合，不使用未声明默认值或上一帧参数。缺失样本事实 MUST保留供观察。

#### Scenario: 部分样本缺少可选参数

- **WHEN** 策略为WeightedAvailableSamples且部分正权重样本缺值
- **THEN** MUST对有值样本重新归一化后聚合，并报告缺失来源

### Requirement: Foot数据必须使用姿势相同的样本贡献

构建 MUST按每个实际样本的Clip、Motion Reference、Rig、Calibration及分析输入身份校验正式Foot Analysis依赖，并保留每个source usage身份。运行所需数据 MUST按现行Foot Motion注册曲线及Projection合同生成，不新增可写feature副本。运行聚合 MUST使用与姿势一致的样本权重和有效时间；Player MUST不执行世界查询、Landing、Pelvis或IK。

#### Scenario: Walk与Run共同贡献

- **WHEN** Walk与Run分别以0.4和0.6权重贡献姿势
- **THEN** 对应脚部样本贡献 MUST使用同一权重与时间
- **AND** 后续脚部求解 MUST继续走角色唯一已配置链

### Requirement: Blend Space必须拥有正式资产编辑体验

作者 MUST能通过正式资产入口编辑模式、轴、样本、时间和参数策略，并进入既有Undo与保存事务。资源引用 MUST定位精确资产和消费者。修改 MUST使关联产物过期，只有明确构建命令才能发布新Projection；不得通过选择、重绘或保存自动构建。

#### Scenario: 作者拖动二维样本

- **WHEN** 作者修改样本位置
- **THEN** MUST保持SampleId并记录可撤销修改，不自动构建产物

### Requirement: Preview与Runtime必须共享同一Blend Space计划

可用的Blend Space预览 MUST使用相同编译权重和时间映射，不能用临时Mixer另算结果。运行观察 MUST读取匹配版本的已发布事实，显示轴值、样本身份、权重、phase、有效时间及数据来源；版本不匹配时 MUST显示Unavailable。本合同不表示Scene Play迁移已完成。

#### Scenario: 运行时观察BlendSpacePlayer

- **WHEN** 作者选择正在运行的Player
- **THEN** MUST显示该版本实际发布的样本贡献，不从作者配置或播放器状态重新推算
