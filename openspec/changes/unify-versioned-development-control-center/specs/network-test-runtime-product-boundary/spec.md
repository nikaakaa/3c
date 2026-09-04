## MODIFIED Requirements

### Requirement: Network Test Product必须由显式Runtime Artifact列表组成

Network Test MUST消费开发候选 schema v4，稳定保存 Source/构建身份、NetworkModelIdentity、RuntimeTopologyIdentity、runtime artifacts、精确工具引用及角色计划。每个 artifact MUST声明唯一 RoleId、载体 Kind、ProductId、受约束相对 root/entrypoint、配置及可选子 manifest/hash。共享工具 MUST通过 ArtifactRoot 中精确不可变引用定位。公共系统 MUST不恢复固定 Player/Server 字段、Model猜进程、目录猜闭包或仓库当前脚本。

#### Scenario: Rollback产品包含Dedicated Relay Server

- **WHEN** 构建Rollback候选
- **THEN** 清单 MUST准确声明Client Player、Dedicated Relay、GM及各自角色/工具身份
- **AND** 角色载体 MUST不被误认为新增Network Model

#### Scenario: Artifact路径逃逸

- **WHEN** artifact或工具引用离开其已声明封存根
- **THEN** 发布/运行 MUST拒绝
- **AND** MUST不搜索其他目录补齐

### Requirement: 公共Build Workflow必须与具体产品和服务器解耦

DevelopmentCandidateBuildWorkflow MUST统一源码固定、构建配方、Unity/外部编译、工具引用、staging、精确闭包与原子发布。产品 adapter MUST显式提供附加 artifacts、工具和角色计划，不相互调用 helper。公共合同 MUST只表达载体、输入输出和能力，不引入 Fantasy、Authority、Rollback 或具体 adapter 分支。

#### Scenario: 新增另一种Managed Executable产品

- **WHEN** 显式adapter声明受支持的ManagedExecutable artifact
- **THEN** 公共工作流 MUST通过现有合同发布与验证
- **AND** MUST不增加按模型类型分支

### Requirement: 三个产品必须拥有精确且隔离的Artifact闭包

Unity Authority、DotRecast Authority、DeterministicRollback MUST分别在共享库的独立 ProductId 下保存不可变 Candidate。每个候选 MUST准确绑定本产品 Player、附加 artifact、工具、角色计划与文件闭包。已有候选不可覆盖，运行配置只属于 Run，不写入已封存候选。旧固定当前产物与 schema 1/2/3 MUST不作为新活动路径。

#### Scenario: 连续构建三个产品

- **WHEN** 作者从不同提交为三个产品分别构建
- **THEN** 所有合法候选 MUST各自保留来源与闭包
- **AND** 任一发布或运行 MUST不修改其他候选

### Requirement: Build与Run必须消费同一正式产品Manifest

Build MUST在共享库发布前校验开发候选 schema v4 及全部引用。Run MUST固定该 CandidateId 和 manifest/hash，通过唯一 Development Run Host 按角色计划启动，不能选择最新、编译、升级 schema 或修复候选。Run MAY生成本次实例配置，但生成结果 MUST绑定候选、工具、RunId 和 SessionId。

#### Scenario: 使用旧schema v1产物运行

- **WHEN** 作者指向旧固定根或不支持的manifest
- **THEN** Run MUST拒绝并提供原因
- **AND** MUST不把旧数据复制进新目录作为合法版本
