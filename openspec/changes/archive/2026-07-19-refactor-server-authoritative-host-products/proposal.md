# Change: 拆分 ServerAuthoritative 服务端宿主产品

## Why

Unity Authority 与 DotRecast Authority 当前虽然发布到不同目录并使用不同 `Fantasy.config`，但二者都由同一个 `3cDemo/Server/Main/Main.csproj` 发布。公共 `Entity.csproj` 同时引用 Core、Float32、ServerAuthoritative、DotRecast 与 DotRecastAuthority，公共 `Hotfix.csproj` 同时编译外部 Unity Worker 路由和进程内 DotRecast Authority Scene。结果是两个目录中的 `Main.exe`、`Entity.dll`、`Hotfix.dll` 与 portable 依赖闭包相同，Unity Authority 包也携带 DotRecast runtime；Unity 构建入口只是在 publish 后删除 DotRecast Scene XML 节点。

这只能证明“部署目录隔离”，不能证明“服务端产品隔离”。新增第三种 Authority Host 时仍需修改公共 `Entity/Hotfix/Main` 与 Router，公共基座继续认识具体网络后端，不符合已经确定的可插拔边界。

## What Changes

- 建立唯一共享 Fantasy Server Host bootstrap，只负责日志、显式产品定义校验、Entity/Hotfix 模块装载和 `Fantasy.Entry` 启动，不拥有任何具体 Authority Host 分支。
- 将 Gate Room、固定 roster、客户端控制连接、generated protocol 与 host-neutral route contract 收敛为共享 Gate 模块；共享 Gate 不引用 DotRecast、DotRecastAuthority、Unity CharacterController 或具体 Host Scene runtime。
- 将外部 Unity Authority Worker control route 与 handler 收敛为 Unity Authority 产品模块。
- 将进程内 DotRecast Authority Scene Entity、lifecycle、handler、control adapter、manifest runtime 与 DotRecast portable 依赖收敛为 DotRecast Authority 产品模块。
- 新建两个明确的可执行产品：`ThirdPerson.UnityAuthority.Server` 与 `ThirdPerson.DotRecastAuthority.Server`。每个产品拥有唯一入口项目、ProductId、精确模块目录、源 `Fantasy.config`、输出可执行文件和 server product manifest。
- 用启动时一次性安装的 `AuthorityHostRouteAdapter` 取代公共 Router 对 `ExternalUnityWorker` 与 `InProcessDotRecastScene` 的具体分支。一个服务端产品只安装一种 route adapter，Active Room 不支持运行时切换。
- 将内部 route kind 的具体 DotRecast 命名收敛为通用 in-process authority scene 语义；具体 HostProfile、Solver 与 World identity 仍由产品和握手事实表达，不修改 Gameplay Network Model 语义。
- 建立显式产品模块装载合同：Entity 模块由编译期 marker 精确声明，Hotfix 模块由有序清单在同一个可卸载上下文中原子加载。缺失、重复或多余模块直接拒绝启动，不扫描目录、不按名称猜测、不回退通用 `Hotfix.dll`。
- 为 Unity Authority 提交 Gate-only 源配置，为 DotRecast Authority 提交 Gate + DotRecast Authority Scene 源配置；删除 publish 后修改 XML 的路径。
- 扩展 Build manifest，使其记录 ServerProductId、入口文件、配置 hash、精确模块/依赖 hash 与 Authority artifact hash；Run 只消费匹配产品的 manifest 和产物。
- 删除旧 `Main.csproj`、旧通用 `Main.exe` 假产品、硬编码单一 `Hotfix.dll` 的 `AssemblyHelper`、公共程序集中的具体 Host 分支、Unity publish 后配置裁剪和脚本中的 `Main.exe` 假设。

## Scope

### In Scope

- Fantasy Server 入口、Entity/Hotfix 模块边界与产品装配。
- Unity Authority 与 DotRecast Authority 的 server build/publish/run 入口。
- Gate 到 Authority Host 的 control route 装配边界。
- 两种产品的正式配置、manifest、依赖闭包与清理。
- 受影响的 OpenSpec current spec 与 `openspec/project.md`。

### Out of Scope

- 不修改 Corin Program、Semantic IR、Float32 Kernel、Prediction/Authority Pipeline、checkpoint、correction、UDP gameplay 数据面或表现同步语义。
- 不新增第三种网络模型，不实现 deterministic rollback server product。
- 不改变 Unity Authority 四进程和 DotRecast Authority 三进程拓扑。
- 不新增运行时网络模型切换、通用插件扫描、fallback product 或兼容 `Main.exe`。
- 不新增测试代码，不运行 Unity batchmode。

## Current Spec Comparison

- current `server-authoritative-host-portability`只约束 Authority Source、Pipeline、control transport 与 launch request 的 portable runtime 所有权，没有约束 Fantasy Server 可执行产品和程序集闭包。本 change 新增产品级边界，不复制 portable runtime。
- current `fantasy-unity-authoritative-session`要求四进程正式启动入口，但没有要求 Unity Authority 使用独立 Gate-only server product。本 change 修改该 requirement，使正式启动入口必须消费专属产品。
- 已归档的 `add-dotrecast-authoritative-server-backend` current spec要求 Unity/DotRecast 使用不同目录、配置与 manifest，但归档实现仍发布同一 `Main.csproj`。本 change 保留它的目录和进程拓扑要求，并补上不同入口项目、不同模块闭包和禁止发布后配置裁剪的更强约束。
- active `add-deterministic-rollback-kcc-model`不得修改本 change 的共享 Gate 或两个既有产品。未来若需要服务端产品，必须新增自己的产品模块并复用同一 Host/Product 合同。
- `add-dotrecast-authoritative-server-backend` 已先归档，归档顺序前置条件已满足；本 change 不恢复或保留单一 `Main` 路径。

## Impact

- 受影响代码：`3cDemo/Server/Main`、`Entity`、`Hotfix`、ServerAuthoritative Gate/Authority Scene 文件、两个 Unity Editor Build/Run 入口和两个 PowerShell 启动脚本。
- 新增代码边界：共享 Server Host、共享 Gate Entity/Hotfix、Unity Authority 产品 Entity/Hotfix/Entry、DotRecast Authority 产品 Entity/Hotfix/Entry。
- 受影响产物：两个 server build 目录中的 executable、`Fantasy.config`、server product manifest、network test build manifest 和依赖 DLL 闭包。
- 协议影响：不计划修改 Outer/Inner wire schema；generated protocol 继续作为共享 Gate 合同。若实施中发现必须修改协议才能移除具体产品依赖，必须停止并另行说明 tradeoff，不在本 change 中暗改 generated 文件。
- 迁移是破坏性的：旧 `Main.exe`、通用 `Entity.dll/Hotfix.dll` 产品假设和旧 build manifest 不再可启动，不提供兼容 reader 或旧脚本 fallback。
