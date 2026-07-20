# network-test-runtime-product-boundary Specification

## Purpose

定义Network Model与可独立Build/Run的Network Test Product分离、schema v2 runtime artifact清单和公共构建工作流边界。

## Requirements

### Requirement: Network Model与Network Test Product必须分离表达

系统 MUST将Network Model定义为输入、确认、恢复与状态权威语义，将Network Test Product定义为可独立Build/Run的测试环境。`UnityAuthority`与`DotRecastAuthority` MUST是同一`ServerAuthoritativeHybrid` Model的不同Authority backend产品；`DeterministicRollback` MUST是独立Model产品。公共Build、manifest与Run工具 MUST不把产品数量等同于Model数量，也 MUST不按Model类型推断运行进程。

#### Scenario: 列出三个测试产品

- **WHEN** Editor注册Unity Authority、DotRecast Authority与DeterministicRollback三个Build/Run入口
- **THEN** 产品catalog MUST包含三个互不共享输出目录的ProductId
- **AND** Model identity MUST明确前两个产品共享ServerAuthoritativeHybrid语义而第三个使用DeterministicRollback语义

### Requirement: Network Test Product必须由显式Runtime Artifact列表组成

Network Test Product manifest MUST使用schema v2稳定记录NetworkModelIdentity、RuntimeTopologyIdentity与全部runtime artifacts。每个artifact MUST声明唯一RoleId、Kind、ProductId、受约束相对root、entry point、configuration identity及可选的artifact-owned manifest path/hash。公共系统 MUST不再使用固定`Player + Server`字段、含糊的顶层`hostIdentity`、`ServerShape`枚举、目录存在性或文件名猜测产品闭包。

#### Scenario: Rollback产品包含Dedicated Relay Server

- **WHEN** Build生成DeterministicRollback产品manifest
- **THEN** artifacts MUST精确包含一个Unity Client Player和一个portable .NET Dedicated Relay Server
- **AND** manifest MUST不隐藏在Player Scene中的Server角色

#### Scenario: Artifact路径逃逸

- **WHEN** 任一artifact root、entry point或manifest path规范化后离开当前Product Root
- **THEN** Build或Run MUST在启动进程前失败
- **AND** MUST不搜索其它目录或修复路径

### Requirement: 公共Build Workflow必须与具体产品和服务器解耦

公共Network Test Product Build Workflow MUST只拥有Unity Player构建、staging、hash、exact file closure、candidate validation、原子替换与产品目录隔离。具体adapter MUST显式发布零到多个附加runtime artifacts。Artifact Kind MUST只表达`UnityPlayer`或`ManagedExecutable`等启动载体，不得表达Fantasy、Authority、Rollback或Network Model。公共workflow MUST不引用具体Server Product、Rollback Relay Server、Unity Authority、DotRecast Authority、DeterministicRollback或具体adapter类型，也 MUST不包含按产品分支的构建逻辑。

#### Scenario: 新增另一种Managed Executable产品

- **WHEN** 新产品adapter返回一个受支持Kind的managed executable artifact
- **THEN** 公共workflow MUST通过同一artifact合同完成staging、校验与产品manifest生成
- **AND** MUST不修改公共workflow中的产品类型分支

### Requirement: 三个产品必须拥有精确且隔离的Artifact闭包

Unity Authority产品 MUST包含Unity Player与独立Fantasy Gate Server Product artifact；DotRecast Authority产品 MUST包含Unity Client Player与Fantasy Gate + DotRecast Authority Server Product artifact；DeterministicRollback产品 MUST包含Unity Client Player与portable Dedicated Relay Server artifact。不同产品 MUST使用不重叠的固定输出目录，同产品Build MAY原子替换自己的当前artifact与manifest并保留日志，MUST不修改其它产品。

#### Scenario: 连续构建三个产品

- **WHEN** 作者依次Build Unity Authority、DotRecast Authority与DeterministicRollback
- **THEN** 三个Product Root MUST分别保留自己的Player、附加artifact与schema v2 manifest
- **AND** 后一次Build MUST不覆盖前两个产品的产物或日志

### Requirement: Build与Run必须消费同一正式产品Manifest

Build MUST生成并在原子替换前验证schema v2 product manifest、全部artifact manifest/hash和exact file closure，且 MUST不启动进程。Run MUST只读取并验证当前正式manifest后启动其中声明的进程角色，MUST不触发Unity Build、dotnet publish、配置导出、目录修复、schema迁移或fallback。

#### Scenario: 使用旧schema v1产物运行

- **WHEN** Run读取包含旧`player/server`字段或不支持schema version的产品manifest
- **THEN** Run MUST在启动任何进程前明确失败
- **AND** MUST不兼容读取、自动升级或重新Build
