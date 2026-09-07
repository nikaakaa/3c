## MODIFIED Requirements

### Requirement: Network Test Product必须由显式Runtime Artifact列表组成

Network Test Candidate manifest MUST使用schema v3稳定记录Candidate源码身份、NetworkModelIdentity、RuntimeTopologyIdentity、全部runtime artifacts、Tool Bundles和Session Plan。每个runtime artifact MUST声明唯一RoleId、Kind、ProductId、受约束相对root、entry point、configuration identity及可选manifest path/hash；每个Tool Bundle MUST引用明确artifact、版本、合同和BundleHash。公共系统 MUST不使用固定Player/Server字段、顶层hostIdentity、目录存在性、文件名或仓库当前脚本猜测闭包。

#### Scenario: Rollback Candidate包含独立工具

- **WHEN** Build生成DeterministicRollback Candidate
- **THEN** runtime artifacts MUST精确包含Unity Player、Dedicated Relay和独立GM
- **AND** Tool Bundles MUST精确包含公共Orchestrator、Rollback启动adapter和GM工具身份

#### Scenario: Tool路径逃逸

- **WHEN** Tool Bundle的root、entry point或配置路径规范化后离开Candidate Root
- **THEN** Build或Run MUST在启动前失败
- **AND** MUST不搜索仓库Tools目录补齐

#### Scenario: Artifact路径逃逸

- **WHEN** 任一runtime artifact root、entry point或manifest path规范化后离开Candidate Root
- **THEN** Build或Run MUST在启动前失败
- **AND** MUST不搜索其它目录或修复路径

#### Scenario: Rollback 开发产品包含独立 GM

- **WHEN** Build 生成 Rollback 产品 manifest
- **THEN** artifacts MUST精确包含 `unity-client-player`、`deterministic-relay-server`、`development-gm-server`
- **AND** GM MUST是独立 ManagedExecutable，不能藏进 Relay 命令分支或 Player Scene

#### Scenario: Artifact 路径逃逸

- **WHEN** artifact 路径规范化后离开 ProductRoot
- **THEN** Build 或 Run MUST在启动前失败，不修复或搜索路径

#### Scenario: Rollback产品包含Dedicated Relay Server

- **WHEN** Build生成DeterministicRollback产品manifest
- **THEN** artifacts MUST精确包含Unity Client Player、portable .NET Dedicated Relay Server和独立GM Server三个artifact
- **AND** manifest MUST不隐藏在Player Scene中的Server角色

### Requirement: 公共Build Workflow必须与具体产品和服务器解耦

公共Network Test Build Workflow MUST只拥有源码Candidate身份、Unity Player构建、staging、hash、exact closure、Tool Bundle公共发布、schema v3 validation与版本目录原子发布。具体adapter MUST显式发布零到多个附加runtime artifacts、产品工具和类型化Session Plan。Artifact Kind与Tool Bundle合同 MUST不表达具体Network Model分支；公共workflow MUST不引用Fantasy、Authority、Rollback、GM或具体adapter类型。

#### Scenario: 新增另一种Managed Executable产品

- **WHEN** 新Product adapter返回支持合同的runtime artifacts、tool bundles和Session plan
- **THEN** 公共workflow MUST通过同一Candidate合同发布和验证
- **AND** MUST不修改公共workflow增加产品类型switch

### Requirement: 三个产品必须拥有精确且隔离的Artifact闭包

Unity Authority、DotRecast Authority与DeterministicRollback MUST分别在自己的Product根下保存一个或多个不可变Candidate。每个Candidate MUST包含该Product精确Player、附加runtime artifacts、Tool Bundles、Session Plan和schema v3 manifest。不同Product与Candidate不得互相覆盖；同CandidateId重复Build MUST失败。旧固定目录当前产物、schema v2和同产品替换语义 MUST不再支持。

#### Scenario: 连续构建三个产品

- **WHEN** 作者为三个Product分别构建多个Candidate
- **THEN** 每份Candidate MUST保留独立源码、artifact和工具闭包
- **AND** 任一新Build MUST不修改其它Product或同Product已有Candidate

#### Scenario: 构建 Rollback GM 服务

- **WHEN** Rollback adapter 发布独立 GM artifact
- **THEN** 公共 workflow MUST照常执行文件集合、hash、候选验证及原子替换
- **AND** MUST不修改 Authority 产品输出或默认附加 GM

### Requirement: Build与Run必须消费同一正式产品Manifest

Build MUST生成并验证schema v3 Candidate manifest且不启动进程。Run MUST显式选择一个Candidate和Slot，重新校验manifest、artifact、tool和Session Plan后创建独立RunManifest。Run MUST不publish、编译、修改Candidate、升级schema、选择latest或fallback；CandidateId、ProductId、Tool Bundle或Topology任一不匹配 MUST在启动业务进程前失败。

#### Scenario: 使用旧schema v1产物运行

- **WHEN** Run读取旧固定根、schema v1的player/server字段或schema v2 manifest
- **THEN** MUST明确拒绝且不创建Run实例
- **AND** MUST不兼容读取、自动升级或重新Build
