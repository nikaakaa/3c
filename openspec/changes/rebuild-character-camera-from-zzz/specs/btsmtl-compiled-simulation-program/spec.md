## ADDED Requirements

### Requirement: 相机请求合同必须与表现资源参数分离

Character 编译 MUST通过现有 Semantic IR 与 Presentation contract 表达相机命令种类、稳定 producer、generation/lifecycle 所需信息、目标语义和 Source Map。Camera producer MUST不伪装成 AnimationChannel 或拥有 Gameplay 相机状态。Profile、Sequence、效果、曲线与 Shot 的实际资源和算法参数 MUST属于 Presentation dependency，并通过精确作者引用绑定；运行消费者 MUST不按作者字符串、显示名或目录寻找资源。

#### Scenario: 技能局部Graph与Timeline引用同一效果

- **WHEN** 技能局部 Graph 节点、TreeClip 或 Timeline Clip 引用同一个正式 Shake 资源
- **THEN** 它们 MUST各自保留 producer/source identity，并在同一 Projection 中引用同一效果数据
- **AND** 资源参数 MUST不复制为两份相机运行真相

#### Scenario: 相机命令的作者来源丢失

- **WHEN** 编译无法把相机 producer 关联到精确节点、Clip 或资源引用
- **THEN** 构建 MUST失败并定位缺失的合同
- **AND** MUST不通过名字搜索相近资源补齐

### Requirement: 相机资源必须编译进唯一不可变 Presentation Projection

正式 Character Build MUST将 Camera Profile 及其全部可达序列、效果、曲线、目标槽位和 Shot 引用编译到同一 Presentation Projection 的不可变相机计划，包含确定的资源索引、原行为阶段、容量和 source mapping。Runtime 与 Preview MUST消费同一已发布计划，不解释 Unity 作者对象、原 dump、Semantic IR 或 Editor 曲线，也不创建独立相机 Build/loader 真相。Float32 与 Fixed MUST共用相同相机计划，相机计划不得包含 Numeric Target 的 ProgramHash、LayoutHash、ABI 或状态 codec。

#### Scenario: Float32 与 Fixed 加载同一角色

- **WHEN** 两种 Numeric Target 使用相同相机作者配置和请求合同
- **THEN** 两者 MUST绑定同一 Projection 中的相机资源和算法计划
- **AND** 本地镜头历史 MUST不进入任一 Numeric Target 状态

#### Scenario: 相机资产在运行时被修改

- **WHEN** Editor 修改了 Profile 但当前运行实例仍绑定原 Projection
- **THEN** 当前实例 MUST继续只消费其已发布不可变计划，编辑器 MUST显示相应过期状态
- **AND** MUST不直接读取新资产字段局部替换运行参数

### Requirement: 相机构建必须校验完整依赖与语义并原子发布

构建 MUST校验完整来源对应、资源闭包、类型、曲线、目标需求、原组合阶段、时间规则、数值域、容量和支持的原算法。未解析字段、无消费者类型、缺失资源、未知枚举、非法阶段组合和旧版本相机合同 MUST阻止发布。相机计划、Projection、请求的 Numeric Target artifacts、wrapper 与 generated references MUST继续属于现有 Character Build 的同一发布事务，不允许相机单独成功而其它合同失败。

#### Scenario: Override 缺少进入曲线

- **WHEN** Profile 可达的 Override 引用缺失或未解析曲线
- **THEN** 构建 MUST报告对应 Profile、资源与引用该资源的节点/Clip
- **AND** 原发布组 MUST保持完整，不输出默认线性曲线代替

#### Scenario: 固定数值 Target 尚未支持新命令

- **WHEN** 构建请求包含 Fixed Target 而其相机 operation 尚未实现
- **THEN** 整个请求的发布 MUST失败
- **AND** MUST不只发布 Float32 或 Projection 并报告整体成功

### Requirement: 相机依赖身份必须反映正确的修改范围

相机表现资源参数、曲线和平台绑定描述变化 MUST更新 Presentation dependency 与 ProjectionRevision；在命令种类、producer identity、目标语义和时点不变时，MUST不改变 Gameplay SourceRevision、SemanticHash、ContractHash 或 Numeric ProgramHash。改变 SkillProgram、技能局部 Graph、TreeClip/Timeline 请求的语义、时点或生命周期 MUST更新相应 Gameplay 合同。资源选择本身 MUST通过明确的 Presentation binding 进入依赖，不把 Unity 资源对象编码进 portable Program。

#### Scenario: 只修改震动幅度

- **WHEN** 作者只修改既有 Shake 资源的幅度或衰减曲线
- **THEN** 重建 MUST更新 ProjectionRevision
- **AND** 相同请求合同的 Gameplay 与 Numeric Program identity MUST保持不变

#### Scenario: 修改技能相机触发时点

- **WHEN** 作者把相机事件从一个 Timeline 时点移到另一个时点
- **THEN** Gameplay SourceRevision 与相关编译合同 MUST按正式语义变化更新
- **AND** 相机 Projection MUST与新的 producer/source mapping 一起原子发布
