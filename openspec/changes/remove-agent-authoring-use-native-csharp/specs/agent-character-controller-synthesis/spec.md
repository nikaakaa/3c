## REMOVED Requirements

### Requirement: Agent必须保持Generated Foot Analysis只读

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Validator必须透传正式Foot Analysis编译诊断

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Snapshot 必须是只读投影

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Compiler 必须调用 BTSMTL 正式 authoring API

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Node Emitter 必须使用白名单

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 资产解析必须来自当前角色 authoring context

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Validator 必须检查 Agent 生成 graph 的 BTSMTL 语义

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Compile Report 必须支持 Agent 自修复

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent 评估必须区分结构、语义和业务覆盖

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 正式资产必须仍由人类可微调

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Compiler模块必须按authoring职责聚合

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Snapshot 必须完整投影 MotionWarp authoring

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Validator 必须复用 MotionWarp 正式校验

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Snapshot必须只读投影Body Motion Profile

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Authoring Document必须是声明式控制器结构

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Skill StateMachine转移参数必须只有Edge一份

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document reconcile必须维护 identity 生命周期

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须降低为唯一类型化Mutation计划

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须输出稳定 authoring identity

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须通过类型化Mutation修改 MotionWarp

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须完整表达 Action target authoring

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须通过正式类型化Mutation配置 Animation Channel

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须完整读写Clip注册Curve与Timeline本地Curve

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。
