# Design: ServerAuthoritative 服务端宿主产品边界

## Context

当前运行拓扑在业务上已经分开：

```text
Unity Authority
Fantasy Gate + external Unity Worker + Client A + Client B

DotRecast Authority
Fantasy Gate/DotRecast Authority Scene + Client A + Client B
```

但发布链仍是：

```text
Main.csproj
  -> Entity.dll（包含 Gate + DotRecast Entity + 全部 portable 引用）
  -> Hotfix.dll（包含 external worker + in-process scene 两套路由）
  -> 同一个 Main.exe
  -> publish 后按目录改 Fantasy.config
```

所以差异只存在于 XML 和 Authority artifact，不存在于程序边界。公共 Gate 编译时认识两个具体 Host，Unity 产品携带永远不会执行的 DotRecast 代码。第三种 Host 接入时还要继续修改公共 Router 和公共项目引用。

## Goals

- 让每种 Authority Host 对应一个明确、可审查、可单独发布的服务端产品。
- 让共享 Gate 只依赖 host-neutral contract，不依赖具体 Authority Host 实现。
- 让产品差异在项目引用、模块清单、源配置和 manifest 中显式出现。
- 保持同一 ServerAuthoritative Network Model、portable Source/Pipeline、control products 与 UDP 数据面。
- 保持 Build/Run 分离、同产品覆盖、不同产品隔离。
- 迁移完成后删除旧通用入口和分支，不保留双路径。

## Non-Goals

- 不把 Fantasy Server 改造成通用插件平台。
- 不支持一个 server process 在启动后切换产品或同时装载两种 Authority Host。
- 不重新实现 Fantasy 协议、Room、Prediction、Authority Simulation 或 DotRecast Solver。
- 不为未来 Rollback 预建空项目或占位配置。

## Final Ownership

| 模块 | 拥有内容 | 禁止内容 |
|---|---|---|
| `ThirdPerson.Server.Host` | 日志、产品定义校验、模块装载、Fantasy Entry 启动 | Room、协议 Handler、具体 Host route、Solver |
| `ThirdPerson.Server.Gate.Entity` | Room Entity、roster、generated protocol、host-neutral route state/port | DotRecast runtime、Unity Worker实现、Solver |
| `ThirdPerson.Server.Gate.Hotfix` | Client join/leave、Room lifecycle、共同可靠控制路由 | external/in-process Host 的具体注册与发送分支 |
| `ThirdPerson.Server.UnityAuthority.Entity` | external Unity Worker route adapter 与产品 identity | DotRecast、Authority Scene runtime |
| `ThirdPerson.Server.UnityAuthority.Hotfix` | Worker register、external control handler | DotRecast Scene handler |
| `ThirdPerson.Server.DotRecastAuthority.Entity` | Authority Host Entity、in-process route adapter、manifest/runtime owner | UnityEngine、CharacterController |
| `ThirdPerson.Server.DotRecastAuthority.Hotfix` | Authority Scene lifecycle、Inner handler、control adapter | external Unity Worker handler |
| `ThirdPerson.UnityAuthority.Server` | Unity 产品定义、Gate-only 配置、入口 | DotRecast项目引用与artifact |
| `ThirdPerson.DotRecastAuthority.Server` | DotRecast 产品定义、Gate+Authority配置、入口 | Unity Worker可执行逻辑 |

generated Outer/Inner message 类型仍属于共享 Gate Entity，因为它们是两种产品共同使用的控制合同。是否装载对应 Handler 由产品 Hotfix 模块决定，不能由公共 Router 在每条消息上判断产品。

## Project Layout

```text
3cDemo/Server/
  Shared/Host/
    ThirdPerson.Server.Host.csproj
  Gate/Entity/
    ThirdPerson.Server.Gate.Entity.csproj
  Gate/Hotfix/
    ThirdPerson.Server.Gate.Hotfix.csproj
  Products/UnityAuthority/
    Entity/ThirdPerson.Server.UnityAuthority.Entity.csproj
    Hotfix/ThirdPerson.Server.UnityAuthority.Hotfix.csproj
    ThirdPerson.UnityAuthority.Server.csproj
    Fantasy.config
  Products/DotRecastAuthority/
    Entity/ThirdPerson.Server.DotRecastAuthority.Entity.csproj
    Hotfix/ThirdPerson.Server.DotRecastAuthority.Hotfix.csproj
    ThirdPerson.DotRecastAuthority.Server.csproj
    Fantasy.config
```

旧 `Main/`、旧通用 `Entity/` 与旧通用 `Hotfix/` 在文件迁移完成后删除。目录和项目名表达真实所有权，不继续用 `Main`、`Entity`、`Hotfix` 作为产品身份。

## Product Definition

每个入口项目在编译期构造一个不可变 `ServerHostProductDefinition`：

```text
ProductId
ExecutableName
ConfigurationIdentity
EntityModuleMarkers[]
HotfixModuleDescriptors[]
AuthorityHostRouteAdapterFactory
RequiredSceneTypes[]
ForbiddenModuleIds[]
```

产品定义是代码装配事实，不是运行时下拉配置。启动时只执行一次：

```text
Product Entry
  -> validate exact ProductDefinition
  -> install exactly one AuthorityHostRouteAdapter
  -> force-load exact Entity module markers
  -> load exact ordered Hotfix modules into one product ALC
  -> validate Fantasy.config scene set
  -> Fantasy Entry.Start
```

Entity 模块必须由 marker 类型直接引用，不能扫描输出目录找程序集。Hotfix 模块允许使用产品定义中的精确文件名，因为它们必须进入可卸载上下文；loader 只加载清单中的文件，并要求文件存在、hash 匹配、ModuleId 唯一。它不扫描 DLL、不按程序集名猜测角色，也不回退旧 `Hotfix.dll`。

Hotfix reload 以整个产品 Hotfix 模块集合为原子单位：先验证新集合，再卸载旧 ALC，最后按固定顺序装载并 `EnsureLoaded`。不能只替换其中一个模块后保留混合 generation。

## Host Route Boundary

共享 Room 只持有一个 host-neutral route：

```text
AuthorityHostRoute
  HostRouteId
  HostProfileId
  HostEndpointIdentity
  ControlPort
```

共享 lifecycle 只调用：

```text
RegisterHost
SendRoster
SendTicket
SendHeartbeat
RequestFullCheckpoint
SendFailure
ReleaseHost
```

Unity 产品的 adapter 将这些调用映射到外部 Worker Session；DotRecast 产品的 adapter 将它们映射到进程内 Authority Scene Address。公共代码不使用 `switch`、具体类型 cast 或 `if DotRecast`。产品入口在 Fantasy 启动前安装唯一 adapter，重复安装或缺失直接失败。

当前内部枚举 `InProcessDotRecastScene` 改为通用 `InProcessAuthorityScene`。DotRecast 仍通过 HostProfileId、SolverId、WorldId 和 Scene manifest 表达具体实现；通用 route kind 不再泄漏 backend 名称。该重命名不改变 wire 数值和 generated schema。

## Product Configuration

两份 `Fantasy.config` 都是提交到源码并由各自 csproj 原样复制的正式配置：

```text
Unity Authority product
  scenes = [Gate]

DotRecast Authority product
  scenes = [Gate, DotRecastAuthority]
```

构建工具只验证源配置与发布配置 exact-byte/hash 一致，不生成、裁剪或补写 Scene。配置有少量重复是可接受的部署事实；相比共享 XML 再做发布期 mutation，它能让产品在源码审查和 `dotnet publish` 时就暴露真实拓扑。

## Build Products

```text
3cDemo/Server/Build/Network/UnityAuthority/Server/
  ThirdPerson.UnityAuthority.Server.exe
  Fantasy.config
  ServerProductBuild.json
  shared Gate/ServerAuthoritative dependencies
  UnityAuthority product modules

3cDemo/Server/Build/Network/DotRecastAuthority/Server/
  ThirdPerson.DotRecastAuthority.Server.exe
  Fantasy.config
  ServerProductBuild.json
  shared Gate/ServerAuthoritative dependencies
  DotRecast product modules/dependencies
  Authority/...
```

Unity 产品发布校验必须拒绝：

- `ThirdPersonSimulation.DotRecast*.dll`；
- DotRecast Authority Entity/Hotfix 模块；
- `Authority/` scene artifact；
- DotRecast Authority Scene 配置。

DotRecast 产品发布校验必须要求：

- DotRecast 与 DotRecastAuthority portable 程序集；
- DotRecast Authority Entity/Hotfix 模块；
- Gate + DotRecastAuthority 精确 Scene 集合；
- Authority manifest、Program 与 Navigation artifact 的 hash。

`ServerProductBuild.json`至少保存：

```text
SchemaVersion
BuildId
ServerProductId
ExecutableRelativePath + SHA256
ConfigurationRelativePath + SHA256
EntityModules[] { ModuleId, RelativePath, SHA256 }
HotfixModules[] { ModuleId, RelativePath, SHA256, LoadOrder }
PortableDependencies[] { ModuleId, RelativePath, SHA256 }
AuthorityArtifacts[] { ArtifactId, RelativePath, ContentHash }
```

canonical writer与reader只位于共享Host。Editor Build完成产品publish，并在DotRecast产品中先通过正式Exporter写入Authority artifact，然后调用刚发布的产品入口`--write-server-product-manifest <BuildId>`；产品入口从自己的唯一`ServerHostProductDefinition`生成并立即回读校验manifest。Editor不复制ModuleId、依赖、Scene或artifact清单，避免Build工具与Server启动拥有两份产品真相。

现有 Unity 侧 network test build manifest保存该 server product manifest 的相对路径与 hash。Run 先验证 network manifest，再验证 server product manifest 和全部文件，最后启动固定拓扑。Run 不调用 `dotnet publish`，Build 不启动进程。

## Runtime Chains

### Unity Authority

```text
ThirdPerson.UnityAuthority.Server
  -> shared Host bootstrap
  -> Gate Entity/Hotfix
  -> UnityAuthority Entity/Hotfix
  -> external Worker route adapter
  -> Fantasy Gate

Unity Authority Worker
  -> existing portable Authority Source/Pipeline/Composer
```

### DotRecast Authority

```text
ThirdPerson.DotRecastAuthority.Server
  -> shared Host bootstrap
  -> Gate Entity/Hotfix
  -> DotRecastAuthority Entity/Hotfix
  -> in-process Authority Scene route adapter
  -> Fantasy Gate + DotRecast Authority Scene
  -> existing portable Authority Source/Pipeline/Composer
```

两条链从 Gate 之后仍消费同一 control products、Authority Pipeline 与 UDP data-plane contract。产品拆分不产生第二套 Gameplay runtime。

## Migration Order

1. 提取共享 Host bootstrap 与显式产品定义/模块装载合同。
2. 将现有 Gate Entity/Hotfix 与 DotRecast Entity/Hotfix 按所有权移动到新项目。
3. 将公共 concrete Router 改成启动时唯一 route adapter。
4. 建立 Unity Authority 产品和 Gate-only 源配置，切换 Build/Run。
5. 建立 DotRecast Authority 产品和 Gate+Authority 源配置，切换 Build/Run。
6. 为两个发布目录写入并验证 server product manifest。
7. 删除旧 Main、AssemblyHelper、公共具体 Host 分支、XML mutation 与 `Main.exe` 脚本假设。
8. 更新 project/current spec 并完成严格校验。

迁移期间不得保留一个可运行的旧 `Main.exe` 作为保险。新产品全部编译并切换调用方后，在同一 change 中删除旧入口。

## Decisions And Tradeoffs

### Decision: 两个可执行项目，而不是一个可执行文件加两份配置

- 收益：项目引用在编译期暴露依赖泄漏；Unity 包无法无意携带 DotRecast；未来产品只新增自己的入口和模块。
- 代价：多两个入口 csproj、两份很小的配置和 product manifest。
- 业务取舍：这是证明“后端可替换”的核心交付，额外装配文件比运行时分支更容易审查，也不会增加 Gameplay 心智负担。

### Decision: 共享 Gate 模块，而不是复制两份 Gate Server

- 收益：Room、协议、roster、可靠事务和错误语义仍只有一份。
- 代价：需要一个窄的 host route port 与产品 adapter。
- 业务取舍：两种后端差异只在 Authority Host，复制 Gate 会让修复房间逻辑时出现双写和行为漂移。

### Decision: 显式产品模块清单，而不是反射/目录插件扫描

- 收益：启动闭包可计算、可 hash、缺失即失败；没有未知 DLL 被自动执行。
- 代价：新增模块必须同时更新产品定义。
- 业务取舍：网络测试产品数量很少，显式改一处比通用插件系统更安全，也符合“不做 fallback”的项目规则。

### Decision: 产品安装唯一 route adapter，而不是公共 Router switch

- 收益：共享 Gate 对新增 Host 开放、对修改关闭；运行中不存在 Host 热切换。
- 代价：产品启动前必须完成一次 adapter 装配，Room 创建依赖该事实。
- 业务取舍：当前测试环境本来就由独立 Build/Run 选择，不需要把部署选择带进每条消息的运行逻辑。

### Decision: 提交两份正式配置，而不是 publish 后 XML transform

- 收益：源码就是部署真相，单独 `dotnet publish` 也能得到正确产品；配置 hash 稳定。
- 代价：Gate machine/process/world 配置有少量重复。
- 业务取舍：这些是产品部署数据，不是 Gameplay authoring；小规模明确重复优于隐藏 mutation 和错误 Scene 共启。

## Risks

- Fantasy 的 ModuleInitializer 与 Hotfix ALC 对装载顺序敏感。实现必须先建立精确模块清单和原子 reload，再移动业务类型；不能靠偶然项目引用触发装载。
- 共享 Gate Hotfix 与产品 Hotfix 同处一个 ALC 时，依赖解析必须只来自产品发布根。缺 PDB 时 Debug 发布合同也必须明确失败或正式声明 PDB 非必需，不能隐式回退默认上下文。
- 已归档 DotRecast change 的 current spec把目录/配置隔离描述为产品隔离。本 change 的 delta补充独立入口、模块闭包和产品 manifest requirement，且所需归档顺序已经满足。
- 如果拆分过程中发现 Fantasy source generator 不能跨多个 Entity/Hotfix 模块注册，必须停止并说明：可选方案是产品专属聚合程序集，仍保持两个产品闭包；不得退回单一 Main + runtime switch。
