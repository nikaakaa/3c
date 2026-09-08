## MODIFIED Requirements

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

当前正式Document的Presentation Reconciler MUST按owner依赖生成类型化Mutation，与人工编辑共用validator、资产级事务、子资产identity分配及诊断。Pose采用原生作者模型后，Reconciler MUST直接修改该正式owner及可达子资产，不创建旧作者拓扑中转。Source Slot、Binding、Sync Group、图及状态机的创建、修改、引用与删除 MUST处于同一事务；成功后从最终资产反向导出，任一失败完整回滚。MUST不写Unity YAML、私有序列化path、generated Projection或第二份字符串binding。仅UI接入不得改变业务字段或新增协议分支；第三方运行状态不进入editable。

#### Scenario: apply新增Clip Source Slot与binding
- **WHEN** 目标新增Graph-owned Source Slot、Profile-owned Clip Binding并让Clip Player引用它
- **THEN** Reconciler MUST按依赖创建子资产、配置binding、设置节点引用并保存真实owner
- **AND** 任一失败 MUST回滚全部相关子资产、数组、节点引用及Gameplay、Timeline、Clip与Presentation变化

#### Scenario: apply修改Locomotion Sync Group
- **WHEN** 文档目标调整Group中的原生AnimationClip成员
- **THEN** Reconciler MUST用结构化Clip引用生成Profile Mutation并校验唯一性
- **AND** MUST不修改Clip Curve或自动Build Projection

#### Scenario: 没有业务变化的往返
- **WHEN** 原生Pose作者图导出后未改正文直接dry-run
- **THEN** 计划 MUST为空，不因显示字段、空值编码或底层序列化实现生成伪修改
