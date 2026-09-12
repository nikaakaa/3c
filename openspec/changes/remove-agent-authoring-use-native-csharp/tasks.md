## 1. 基线与删除边界

- [ ] 1.1 核对当前源码、资产和Git差异，记录原生FSM、Slate UI、Pose与共同节点定义已有改动，交付不覆盖范围和实际冲突清单。
- [ ] 1.2 列出AgentAuthoring、SkillDocument、PresentationDocument及目录外的调用者，交付类型/文件/消费者表，区分协议、重复规则、已有领域能力与真实缺失业务规则。
- [ ] 1.3 核对原生FSM与事件图等active change的Document增量，交付各冲突条款的正式去向；不改写其他窗口已确认合同或把冲突默认为已解决。
- [ ] 1.4 核对实际`.btsmtl`工作包和未应用内容，交付可删除包清单；存在用户差异时记录精确内容与待裁决项，不自动丢弃。

## 2. 校验回归正式业务

- [ ] 2.1 删除Agent中的schema/hash/sync/local identity等协议校验，交付删除清单与无正式业务消费者的引用证据。
- [ ] 2.2 对照正式Graph、Timeline、Presentation规则删除Agent重复校验；确实缺失的规则只补所属模块，交付逐规则输入、拒绝条件、唯一实现及调用链。
- [ ] 2.3 将依赖Agent Validator/Report的诊断调用直接接到现有Character编译诊断，交付同一Frontend/Target来源及无Agent包装依赖的引用结果。
- [ ] 2.4 核对Foot Motion完整曲线组、MotionWarp、Action target、Animation channel和状态转移owner约束的正式归属，交付业务规则保留表，不新增中央Validator。

## 3. Skill与Timeline直接C#接口

- [ ] 3.1 清理Skill节点JSON Apply/Export/Parse/Resolver，以现有Configure/Set表达配置；缺失方法补入正式类型，交付各节点类型与资源/值输入的API对照。
- [ ] 3.2 核对Graph、原生FSM、Macro、Blackboard和SkillDefinition的创建/修改/删除接口，补齐确有缺失的业务能力，交付正式类型、身份、系统入口与端口调用链。
- [ ] 3.3 将Timeline Clip JSON配置改为正式强类型参数，覆盖各已注册Clip及资源/曲线/外部binding，交付原字段到正式业务入口的完整对照。
- [ ] 3.4 将Slate新增Clip界面和相关配置入口改为同一强类型调用，删除BuildClipProperties等JSON转换，交付UI到TimelineData的唯一创建/回写链。
- [ ] 3.5 核对固定目标创建、原位配置、复制与删除的身份行为，补齐现有factory确需的身份参数，交付GUID/local file ID/authoring identity保持与复制规则说明，不建通用Ensure框架。

## 4. Presentation与实际owner操作

- [ ] 4.1 清理PresentationDocument中仅为目录包服务的适配；复用现有Pose/Profile/Linked Pose与注册Curve Mutation，交付每项原可写能力的正式入口及删除范围。
- [ ] 4.2 核对Skill Graph、FSM、Timeline TreeClip body与共享资源的owner移动和清理，复用现有领域实现并补齐缺失操作，交付引用闭包和共享资产不误删的代码证据。
- [ ] 4.3 整理实际跨owner业务操作的Undo与保存边界，消除内层提前保存和重复事务；交付参与owner、Graph序列化、失败恢复及保存能力限制，不搬迁Agent总事务。
- [ ] 4.4 核对人工入口与C#入口共用正式API及错误来源，交付代表性创建/配置/删除调用说明；无自动执行、全量声明、JSON或私有序列化旁路。

## 5. 整体删除Agent层

- [ ] 5.1 删除AgentAuthoring中Document、Snapshot、Store、Exporter、Reconciler、Mutation/Session、Validator/Report及其专属support，交付删除类型清单和业务代码无反向引用结果。
- [ ] 5.2 删除SkillDocument/PresentationDocument残留DTO、mapper、applier与无消费者目录，正式业务方法归入已有领域并清理旧命名，交付目录/类型去向及无兼容转发证据。
- [ ] 5.3 删除五个BTSMTL authoring MCP及专属scheduler，保留独立Character Build和其他工具，交付注册项前后对照。
- [ ] 5.4 删除Agent Controller Window与Definition导航，保留正式Graph/Timeline/Profile入口，交付菜单和Inspector调用引用结果。
- [ ] 5.5 删除协议专属测试、失效asmdef依赖、确认可删除的工作包及专属使用说明，交付`.meta`配对与消费者检查；不新增测试或删除其他系统JSON。

## 6. 规范清理与交付

- [ ] 6.1 按本change逐Requirement对照现行spec，处理全部Agent专属删除和领域增量，交付规范差异表；保留其他任务新增场景，不回退与本次无关的Pose/运行合同。
- [ ] 6.2 把独立Build工具条款归入正式编译能力，清理三份退役Agent规范及project/技能入口的失效协议描述，交付唯一文档入口与残留检索结果。
- [ ] 6.3 执行适用的程序集编译与引用检查，交付实际命令、结果和未覆盖范围；dotnet/msbuild禁用build server并结束后shutdown，不用新增测试代替编译结果。
- [ ] 6.4 运行本change严格OpenSpec校验和差异检查，交付结果；若其他active change尚有合同冲突，明确列出而不宣称整体无冲突。
- [ ] 6.5 在execution.md汇总代码链、业务输入输出、删除路径、小步中文提交和剩余事项，交付精确文件链接；不把静态检查称作Unity运行或端到端验收。
