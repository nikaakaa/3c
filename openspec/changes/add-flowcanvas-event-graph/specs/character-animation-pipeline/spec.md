## ADDED Requirements

### Requirement: 动画变量必须由唯一事件宿主在Pose推进前提供

每个动画实例所需作者变量 MUST由其唯一事件图宿主生产，在同次正式表现输入准备后、Pose推进前完成一次更新并发布只读值。该值 MUST替换旧固定参数生产，不能与按名称补值的旧桥同时作为提供者。

输入更新成功与最终姿势提交 MUST有各自明确身份；成功更新的原生状态 MUST不因随后Pose未就绪而回退。Pose根事务、Action生命周期、source准备、混合、Foot、FBBIK、Evaluate Barrier、Seal和最终写入 MUST继续由现有唯一链路拥有；事件图 MUST不能执行这些引擎调度职责。

#### Scenario: 一次正常动画表现更新

- **WHEN** 正式角色事实到达并且事件更新成功
- **THEN** 系统 MUST将本次只读变量交给同次Pose推进
- **AND** 更新图、Pose求值和最终写入 MUST各自只执行其所属工作一次

#### Scenario: 原生输入更新失败

- **WHEN** 事件图发生节点错误或输出不合法
- **THEN** 本次变量 MUST不发布，本次Pose MUST不从部分结果启动
- **AND** 系统 MUST保留该实例的明确故障，不以旧值、默认值或旧生产链继续

#### Scenario: Pose进入Faulted

- **WHEN** 现有Pose事务因Barrier内或之后失败使该Actor动画运行Faulted
- **THEN** 事件宿主 MUST停止该Actor后续更新，直到其正式Reset或Replacement
