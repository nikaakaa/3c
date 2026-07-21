# Design: 客户端构建产物目录与发布边界

## 目标

让开发者只凭路径就能回答三个问题：

1. 这是项目输入、工具缓存还是正式交付产物？
2. 这个目录由谁写入，失败时能否被当作正式版本？
3. 本地 HTTPS 服务、远端 CDN、Player 或 Network Test Product 应消费哪一层？

## 非目标

- 不改变 Runtime 资源缓存位置和 YooAsset 下载语义。
- 不建立自动 CDN 上传或部署环境管理。
- 不统一服务端仓库的 publish 目录。
- 不把 Network Test Product 降低成普通 Player；它包含多个进程和自己的 manifest。
- 不迁移 Unity、HybridCLR 固定工具工作区来追求表面上的单目录。

## 当前问题

### 同义目录没有业务边界

```text
Build/      Unity Player 与 Network Test Product
Builds/     TEngine 正式 YooAsset 输出
Bundles/    YooAsset EditorSimulate，也可能被旧 Android/iOS 一键构建写入
```

三个名字都像“构建结果”，但消费者完全不同。`Bundles` 还没有被 ignore，模拟 manifest 已进入 Git。

### TEngine 按入口和平台选择不同路径

- `BuildConfig` 默认 `./Builds/`。
- Windows 一键构建使用 `Builds/Windows`。
- Android/iOS 一键构建使用 `Bundles`。
- Player 默认写 `Build/<Platform>`。
- ResourcePackageVersion 默认由当前日期和分钟推断。

这些不是一个正式产品构建合同。相同操作从不同菜单执行会得到不同物理结构，也无法证明 Player、Content 与 StartupPolicy 使用匹配版本。

### 原始构建目录不是发布闭包

YooAsset 输出根同时包含 `OutputCache`、BuildReport、目标版本目录和增量构建数据。CDN 只应接收运行时需要的 version、manifest、Bundle 与启动策略。直接把整个 `Builds` 或 `Bundles` 当作服务器根会把构建缓存和模拟文件混入发布边界。

## 目录合同

```text
3C_Client/
├─ Assets/                          受版本控制的源码和资产
├─ Packages/                        受版本控制的包与依赖
├─ ProjectSettings/                 受版本控制的 Unity 配置
├─ Library/                         本机工具缓存
│  └─ YooAsset/BuildOutput/         YooAsset 默认 Builder 与 EditorSimulate
├─ HybridCLRData/                   HybridCLR 本机工作区，禁止提交
└─ Build/                           唯一正式客户端产物根，禁止提交
   ├─ .Workspace/                   正式工作流 staging、原始输出和增量缓存
   ├─ Content/
   │  └─ <BuildTarget>/
   │     └─ DefaultPackage/
   │        └─ <ResourcePackageVersion>/
   ├─ Players/
   │  └─ <BuildTarget>/
   │     └─ <ClientBuildVersion>/
   └─ Network/
      ├─ UnityAuthority/
      ├─ DotRecastAuthority/
      ├─ DeterministicRollback/
      └─ RunLogs/
```

`Build/.Workspace` 是正式构建过程内部的可删除工作区，不是发布物。`Build/Content`、`Build/Players` 和 `Build/Network` 只接收成功并通过闭包校验的产物。

## 决策一：使用单数 `Build` 作为唯一正式根

选择 `Build` 是因为现有 Network Test Product 已经以 `Build/Network` 作为 current contract，Unity Player 也已经使用该根。新增 `Build/Content` 与 `Build/Players` 可以在不破坏网络产品身份的前提下消除 `Builds`。

改成新的 `Artifacts` 根可以让词义更准确，但会同时迁移三个已完成 Network Product 的路径、文档、manifest、Run 工具和大量现有操作习惯；业务收益只是名字更漂亮。保留 `Builds` 专门放资源改动较少，但开发者仍必须记住单复数差异，无法解决当前问题。

## 决策二：正式构建由项目工作流拥有，TEngine 只执行底层步骤

项目层新增唯一 `CommercialClientBuildWorkflow`，拥有：

- ProductStartupProfile 与 ClientBuildVersion 读取。
- ResourcePackageVersion 和 MinimumClientBuildVersion 的显式输入与校验。
- 固定正式路径。
- staging、精确闭包校验和原子发布。
- Content 与 Player 发布 manifest。

TEngine Editor 只拥有：

- 编译并复制 HybridCLR 热更 DLL。
- 执行 YooAsset build request。
- 按显式请求构建 Unity Player。
- 返回结构化成功、失败和输出路径。

继续直接使用通用 TEngine 窗口最省改动，但窗口允许任意输出路径、时间版本和平台分裂默认值，不能成为商业产品的唯一入口。把 ProductStartupProfile 或商业目录写入 TEngine 包又会违反项目业务不得进入基础包的边界。因此选择项目工作流包装无业务的 TEngine 服务，并删除可写正式目录的旧一键菜单。

## 决策三：版本目录不可静默覆盖

Content 目录使用 ResourcePackageVersion，Player 目录使用 ClientBuildVersion。两者是不同业务身份，不使用同一个日期字符串代替。

正式目标版本已存在时构建失败，不覆盖、不合并，也不根据文件时间判断是否相同。这样可以避免 CDN 或客户端缓存仍引用旧字节，而磁盘目录已经被同版本新文件替换。

允许覆盖 `Current` 对本地快速迭代更方便，但会破坏资源版本与文件字节的一一对应。代价是本地每次正式资源发布都必须明确增加 ResourcePackageVersion；只验证 Gameplay 时继续使用 Editor 本地入口，不需要反复发布 Content。

## 决策四：原始构建先进入 Workspace，再原子发布精确闭包

正式 Content 构建顺序：

```text
显式 Release Request
-> 校验三类版本身份与目标路径
-> 编译并复制 HotFix DLL
-> YooAsset 写入 Build/.Workspace/Content
-> 读取 BuildResult 与 Manifest
-> 组装候选发布目录
-> 生成 StartupPolicy.json
-> 生成 CommercialContentRelease.manifest.json
-> 校验 exact file closure 与 hash
-> 原子发布 Build/Content/<Target>/DefaultPackage/<ResourceVersion>
```

失败只留下 `.Workspace` 中的可删除数据，不得产生看似存在的正式版本目录。`OutputCache` 和 BuildReport 保留在 Workspace，不能进入发布闭包。

正式 Player 构建同样先写 Workspace，完成后校验 executable、Data 目录和 Player manifest，再发布到 `Build/Players/<Target>/<ClientBuildVersion>`。

直接让 YooAsset 写最终目录可以减少一次文件复制，但构建中断会留下半成品，后续本地服务器可能误服务该目录。选择 staging 增加一次磁盘 I/O，换取明确的正式版本边界。

## 决策五：YooAsset 默认与模拟输出进入 Library

YooAsset 2.3.17 的默认输出根由 embedded Editor 代码硬编码为项目根 `Bundles`，没有项目级配置挂点。本变更把该唯一 Editor helper 改为 `Library/YooAsset/BuildOutput`。因此：

- EditorSimulate 的 `Simulate-*` 只进入 Library。
- 直接打开 YooAsset 原始 Builder 产生的低层输出也只进入 Library。
- 只有项目正式工作流通过显式 request 写入 `Build/.Workspace`，并且只有通过发布校验的闭包进入 `Build/Content`。

只把 `Bundles` 加入 `.gitignore` 不需要修改 embedded YooAsset，但根目录仍会不断出现含义模糊的目录。迁入 Library 需要在 YooAsset 升级时审查这一处 editor-only patch；业务收益是项目根长期保持清晰，而且不改变 Runtime 包、Manifest 或下载语义。

## 决策六：保留 HybridCLRData 作为工具工作区

`HybridCLRData` 是 HybridCLR 插件使用的本机工作区，部分路径可配置，部分生成链仍以该根组织。强行迁入 Library 会扩大对 HybridCLR 插件的修改范围，但不会改善正式发布边界。

因此本变更不改其生成逻辑，只将整个目录加入 ignore 和 Repository Policy。正式热更 DLL 仍必须经过构建服务复制到 `Assets/AssetRaw/HotUpdate/DLL`，随后由 YooAsset Content 构建采集；`HybridCLRData` 本身永远不是发布源。

## 决策七：Network 保持独立产品分区

Network Test Product 不只是一个 Player，它包含 Player、Managed Server、产品 manifest、进程角色和 RunLogs。把它迁入 `Build/Players` 会错误表达产品闭包，并破坏 current spec。

本变更只让 `NetworkTestProductBuildWorkflow` 从公共 `ClientBuildArtifactLayout.NetworkRoot` 取得现有 `Build/Network`，产品子目录和 schema v2 不变。

## 决策八：本地 HTTPS 服务直接服务一个正式 Content 版本目录

正式 Content 版本目录包含启动和资源运行所需完整闭包。本地 Caddy 或其它 HTTPS 服务把该目录设为 document root，客户端继续使用唯一 ResourceEndpoint。项目内不再复制 `LocalCDN`、`Remote` 或 `ServerData` 镜像。

直接服务避免双份 Bundle 和“构建成功但忘记同步 CDN 目录”。代价是切换本地资源版本时必须明确修改服务器 document root 或其外部部署映射。远端上传、域名和证书仍属于部署环境，不进入本变更。

## 失败处理

| 失败 | 正式结果 |
|---|---|
| ClientBuildVersion、ResourcePackageVersion 或 MinimumClientBuildVersion 缺失 | 构建开始前失败，不创建 Workspace candidate |
| 输出路径离开 Unity 项目 `Build` 根 | 拒绝请求，不尝试修正或回退默认路径 |
| HotFix DLL 编译/复制失败 | Content 构建失败，不运行 YooAsset |
| YooAsset 构建失败 | 保留可删除 Workspace 诊断，不创建正式版本 |
| StartupPolicy 无法按当前 schema 生成 | 候选闭包失败，不发布 |
| exact closure 或 hash 不匹配 | 候选闭包失败，不发布 |
| 相同正式版本目录已经存在 | 明确失败，不覆盖、不合并 |
| Player Build 失败 | 不创建正式 Player 版本目录 |
| Network Product 构建失败 | 沿用现有 staging/原子替换语义，不影响 Content/Players |

## 迁移与删除

- 删除 `Builds` 正式输出旧根，不迁移旧资源版本。
- 删除 `Bundles` 及已跟踪的四个模拟 manifest。
- 删除无版本旧 Player 根 `Build/Client` 与 `Build/Client_ServerAu`。
- 删除 TEngine 的 `Builds/Windows`、Android/iOS `Bundles` 和 `Build/Windows` 默认路径。
- 删除按日期分钟生成 ResourcePackageVersion 的默认方法。
- 删除未使用的 `isAutoAssetCopeToBuildAddress`、`BuildAddress` 及序列化数据。
- 删除通用 TEngine 一键构建菜单对正式目录的写入能力。
- 不增加旧目录探测、迁移器、软链接、目录镜像或运行时兼容读取。

