## REMOVED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 物理分片不得改变Document整包同步语义

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Character Document不得提供旧顶层Graph入口

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Graph JSON必须使用稀疏规范authoring语言

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Skill Graph状态与变量身份必须直接表达

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 系统Node必须通过只读anchor参与Graph连接

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Skill StateMachine转移必须由Edge唯一承载

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 新Graph必须声明正式owner

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Graph逻辑与layout必须使用独立分片

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Timeline结构与Curve payload必须分离

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 可编辑能力必须由唯一authoring capability catalog闭合

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 文档包codec必须严格解析并计算整包规范hash

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 同步状态必须由live revision和整包hash推导

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Document必须确定性降低为完整Mutation Plan

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent实现必须是通用Document适配器

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: dry-run与apply必须锁定同一整包语义

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: apply成功后必须从最终Unity树反向发布整个文档包

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Conflict必须通过显式rebase处理

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: 文件与Editor事件不得自动触发重操作

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Presentation分片必须保持整包同步与稳定owner

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Presentation JSON必须由共享Capability生成稀疏typed字段

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Pose Transition JSON必须保存可解析混合资产引用

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Document v8必须原子替代v7

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Document v8失败恢复必须同时覆盖Unity owner与正式package

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: AnimationClip注册Curve必须使用独立严格分片

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document必须从正式作者metadata投影完整闭包

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。

### Requirement: Agent Document不得拥有第二套作者写入入口

**Reason**: Agent authoring 中间协议及其专属生成、同步、校验和报告整体退役，正式业务不再依赖此层。

**Migration**: 直接使用 `character-csharp-authoring` 规定的正式业务 API；已有 Graph、Timeline、Presentation、Curve 与编译约束仍由对应领域规范承担。删除协议专用与重复规则，只有真实缺失的业务约束补入所属模块，不保留同名入口或改名框架。
