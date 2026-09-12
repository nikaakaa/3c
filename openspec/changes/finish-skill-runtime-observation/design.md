## Context

见[proposal](proposal.md)。原生绘制、诊断来源映射、实例工具栏与导航已有代码；旧6.4.2、7.2.2、7.3、7.4、7.5.2未完成。历史实现见[归档任务](../archive/2026-09-12-refactor-btsmtl-flowcanvas-authoring/tasks.md)，本change只补缺口和证据，不重建观察系统。

## Goals / Non-Goals

**Goals:** 作者准确观察一次真实技能释放及其子调用，暂停/结束后显示不串实例，采集可关闭且开销可核对。

**Non-Goals:** 不迁移作者FSM、升级Document、运行插件图或创建预览场景；不接管网络Pass/Adapter或Corin完整Replay；不新增测试。普通Play观察不以场景协调器为前提。

## Decisions

### D1 一次观察绑定一次正式执行

输入为既有运行注册与只读诊断，identity包含Session、Actor、Program/Pipeline、Skill、ActionInstance、generation及调用路径。多个合法目标要求明确选择，Pinned失配显示失配，不能自动选首项；运行版本改变时停止错误叠加。父节点、子图和breadcrumb维持同一调用，结束保留终态，不混入下一次释放。业务取舍：显式选择增加一次操作，但能避免同图多角色或多调用时显示另一实例。

### D2 展示与采集不参与执行

原生FlowCanvas/FSM绘制消费SourceMap映射的已完成事件和端口值，区分瞬时经过、持续等待、终态及优化消除；不调用getter补值，不用graph.isRunning推进或伪造状态。FSM State/Transition来源适配由integrate-native-fsm-skill-authoring交付；本change只接通统一会话与显示。共同文件冲突按职责局部对齐，不整文件覆盖。

### D3 生命周期与容量有明确边界

沿现有兴趣订阅、完成边界发布和有界缓存工作；换页、关窗、退出Play解绑，Unity暂停保留最后结果。覆盖、淘汰、未采集明确显示，不能把缺失当作未执行。使用同一输入与场景测量关闭、节点观察、值观察三档成本，不用估算证明性能。业务取舍：有界保留允许长时间观察，代价是历史不完整；显示缺口而不恢复无界记录。

### D4 与场景和网络保持接口边界

rebuild-btsmtl-preview-with-scene-play唯一拥有受控场景操作；已有普通Play可直接观察，不新增协调器前置条件。网络纠正后的身份确认消费正式Session事件，网络实现和运行证据由integrate-corin-dump-authoring-replay负责。

当前尚无btsmtl-flowcanvas-runtime-observation正式spec文件，本change完整承接旧delta，保持所有Requirement/Scenario。与场景预览提案的差异是“观察无需专用场景”；两者分别拥有只读显示与场景生命周期，不将其中一个实现成另一个的强制替代。旧v5/v7文档文字不成为观察协议版本来源。

## Risks / Trade-offs

- [同图多实例或网络纠正] → 核对完整身份并显示失配，拒绝猜目标。
- [关闭后仍采集或跨线程部分结果] → 复用正式订阅释放与完成边界快照，交付实际释放和开销记录。
- [本变更被再次塞入作者/网络工作] → 只接收观察任务，下游证据按各自owner回填。

## Migration Plan

先盘点现有会话与诊断接口，再补实例/调用导航和生命周期，核对采集容量与实际开销，最后汇总普通Play和共享子图多调用证据。没有源映射或真实上下文时明确等待，不建立临时执行器。旧已有实现保持，替代的观察适配确认零消费者后删除。
