## 1. 固定现状与迁移边界

- [x] 1.1 读取本 change 的 proposal、design、tasks 与全部 spec delta。
- [x] 1.2 确认 `add-dotrecast-authoritative-server-backend` 已完成归档且本 change 的归档顺序前置条件已满足。
- [x] 1.3 盘点 `Main.csproj`、`Entity.csproj`、`Hotfix.csproj` 的完整项目引用闭包。
- [x] 1.4 盘点 Gate Room、generated protocol、external Unity Worker route 与 DotRecast Authority Scene 类型所有权。
- [x] 1.5 盘点两个 Editor Build 入口、两个 Run 脚本、两个 build manifest 和固定输出目录。
- [x] 1.6 记录当前两个发布目录的 executable、Entity、Hotfix、portable DLL 与配置 hash。
- [x] 1.7 确认本 change 不修改 Program、Pipeline、checkpoint、correction、UDP codec 或 Presentation 语义。
- [x] 1.8 确认 Outer/Inner wire schema不需要修改；若需要修改则停止并说明 tradeoff。

## 2. 建立共享 Server Host 合同

- [x] 2.1 新建 `ThirdPerson.Server.Host` 项目并声明唯一共享 Host bootstrap 所有权。
- [x] 2.2 定义 immutable `ServerHostProductDefinition` 与稳定 `ServerProductId`。
- [x] 2.3 定义 Entity module marker descriptor 与唯一 ModuleId 规则。
- [x] 2.4 定义 Hotfix module descriptor、显式文件、load order 与唯一 ModuleId 规则。
- [x] 2.5 定义产品 RequiredSceneTypes 与 ForbiddenModuleIds。
- [x] 2.6 建立产品定义完整性校验并拒绝空、重复、冲突模块。
- [x] 2.7 建立 Entity marker 的精确强制装载路径。
- [x] 2.8 建立单一 product Hotfix `AssemblyLoadContext` owner。
- [x] 2.9 按清单顺序精确加载全部 Hotfix module并触发注册。
- [x] 2.10 让 Hotfix reload 原子替换完整模块 generation。
- [x] 2.11 拒绝目录扫描、程序集角色猜测和旧单一 `Hotfix.dll` fallback。
- [x] 2.12 将 NLog 与 Fantasy Entry 启动收敛到共享 bootstrap。
- [x] 2.13 让产品入口在 Fantasy Entry 前完成定义、模块和配置校验。

## 3. 拆分共享 Gate 模块

- [x] 3.1 新建 `ThirdPerson.Server.Gate.Entity` 项目。
- [x] 3.2 迁移 generated Outer/Inner protocol 与 RouteType 到 Gate Entity。
- [x] 3.3 迁移 `ServerAuthoritativeRoom`、roster、ticket 与共同 route state 到 Gate Entity。
- [x] 3.4 从 Gate Entity 移除 DotRecast、DotRecastAuthority 与 Solver 项目引用。
- [x] 3.5 新建 `ThirdPerson.Server.Gate.Hotfix` 项目。
- [x] 3.6 迁移共同 Client join/leave、Room lifecycle、可靠事务与失败传播 Handler。
- [x] 3.7 保证 Gate Hotfix 只引用 Gate Entity 和 host-neutral ServerAuthoritative 合同。
- [x] 3.8 从 Gate Hotfix 移除 external Unity Worker 与 DotRecast Scene 的具体 route 实现。
- [x] 3.9 保持 Gate 不执行 Program、Pipeline、WorldSolver 或 gameplay datagram。

## 4. 收敛 Authority Host Route 边界

- [x] 4.1 从现有 `ServerAuthoritativeAuthorityHostRouter` 提取 host-neutral route port。
- [x] 4.2 定义注册、roster、ticket、heartbeat、full checkpoint、failure 与 release 的唯一调用面。
- [x] 4.3 建立启动时一次性 `AuthorityHostRouteAdapter` 安装点。
- [x] 4.4 让 adapter 缺失、重复安装或 ProductId 不匹配时拒绝服务器启动。
- [x] 4.5 让 Room lifecycle 只调用已安装 adapter，不判断具体 route kind。
- [x] 4.6 删除公共 Router 中对 external Worker Session 的具体分支。
- [x] 4.7 删除公共 Router 中对 DotRecast Authority Scene Address 的具体分支。
- [x] 4.8 将内部 `InProcessDotRecastScene` 命名迁移为 `InProcessAuthorityScene`。
- [x] 4.9 保持 route wire 数值与 generated protocol schema 不变。
- [x] 4.10 保持 Active Room 只锁定一个 Host route且不支持运行时切换。

## 5. 建立 Unity Authority Server 产品

- [x] 5.1 新建 `ThirdPerson.Server.UnityAuthority.Entity` 项目。
- [x] 5.2 将 external Unity Worker route adapter 与产品 route identity迁入 Unity Entity。
- [x] 5.3 新建 `ThirdPerson.Server.UnityAuthority.Hotfix` 项目。
- [x] 5.4 将 Worker register、external control handler 与 Session route迁入 Unity Hotfix。
- [x] 5.5 新建 `ThirdPerson.UnityAuthority.Server` 入口项目。
- [x] 5.6 定义 Unity Authority ProductId、精确 Entity/Hotfix 模块清单与 forbidden module集合。
- [x] 5.7 提交只包含 Gate Scene 的 Unity Authority `Fantasy.config`。
- [x] 5.8 让 Unity 产品只引用 Host、Gate、Unity Authority 与共同 ServerAuthoritative 依赖。
- [x] 5.9 禁止 Unity 产品引用 DotRecast、DotRecastAuthority 与 Authority Scene artifact。
- [x] 5.10 将输出 executable 固定为 `ThirdPerson.UnityAuthority.Server.exe`。
- [x] 5.11 保持 Unity Authority Worker、Client A、Client B 与 Gate 的四进程拓扑不变。

## 6. 建立 DotRecast Authority Server 产品

- [x] 6.1 新建 `ThirdPerson.Server.DotRecastAuthority.Entity` 项目。
- [x] 6.2 迁移 DotRecast Authority Host Entity、Scene diagnostics 与 in-process route adapter。
- [x] 6.3 让 DotRecast Entity 显式引用 portable DotRecast 与 DotRecastAuthority 项目。
- [x] 6.4 新建 `ThirdPerson.Server.DotRecastAuthority.Hotfix` 项目。
- [x] 6.5 迁移 Authority Scene lifecycle、Inner handler 与 control transport。
- [x] 6.6 新建 `ThirdPerson.DotRecastAuthority.Server` 入口项目。
- [x] 6.7 定义 DotRecast Authority ProductId、精确 Entity/Hotfix 模块清单与 required module集合。
- [x] 6.8 提交只包含 Gate + DotRecastAuthority Scene 的产品 `Fantasy.config`。
- [x] 6.9 让 DotRecast 产品显式引用 Host、Gate、DotRecast 产品模块与 portable authority依赖。
- [x] 6.10 保证 DotRecast 产品不引用 UnityEngine、CharacterController 或 Unity Worker runtime。
- [x] 6.11 将输出 executable 固定为 `ThirdPerson.DotRecastAuthority.Server.exe`。
- [x] 6.12 保持 Fantasy Server、Client A、Client B 的三进程拓扑不变。

## 7. 建立 Server Product Build Manifest

- [x] 7.1 定义 versioned `ServerProductBuild` schema。
- [x] 7.2 写入 BuildId、ServerProductId、configuration 与 executable 相对路径/hash。
- [x] 7.3 写入 source `Fantasy.config` 相对路径/hash与精确 Scene 集合。
- [x] 7.4 写入 Entity module Id、文件、hash。
- [x] 7.5 写入 Hotfix module Id、文件、hash与load order。
- [x] 7.6 写入 portable dependency Id、文件、hash。
- [x] 7.7 为 DotRecast 产品写入 Authority manifest、Program 与 Navigation artifact hash。
- [x] 7.8 建立 manifest canonical writer/reader 与严格字段校验。
- [x] 7.9 让 network test build manifest 引用精确 server product manifest path/hash。
- [x] 7.10 让 Run 在启动前验证 product、executable、config、modules、dependencies与artifacts。
- [x] 7.11 拒绝未知、多余、缺失或 hash 不匹配的 server module。

## 8. 切换 Build 与 Run 工作流

- [x] 8.1 将 Unity Authority Editor Build 改为 publish `ThirdPerson.UnityAuthority.Server.csproj`。
- [x] 8.2 删除 Unity Build 的 publish 后 `Fantasy.config` XML裁剪。
- [x] 8.3 让 Unity Build校验 Gate-only 源配置与发布配置 exact match。
- [x] 8.4 让 Unity Build校验发布闭包不包含 DotRecast模块或Authority artifact。
- [x] 8.5 将 DotRecast Authority Editor Build 改为 publish `ThirdPerson.DotRecastAuthority.Server.csproj`。
- [x] 8.6 让 DotRecast Build校验 Gate + Authority源配置与发布配置 exact match。
- [x] 8.7 让 DotRecast Build校验全部 required module 与 Authority artifact。
- [x] 8.8 保持同产品 Build替换自己的 Player、Server、manifest与Authority artifact。
- [x] 8.9 保持同产品 Build不删除既有 RunId日志。
- [x] 8.10 保持 Unity 与 DotRecast 固定输出目录互不覆盖。
- [x] 8.11 更新 Unity Run脚本使用 `ThirdPerson.UnityAuthority.Server.exe`。
- [x] 8.12 更新 DotRecast Run脚本使用 `ThirdPerson.DotRecastAuthority.Server.exe`。
- [x] 8.13 更新进程清理逻辑按两个明确 executable与固定产品目录识别。
- [x] 8.14 保持 Build不启动进程、Run不触发编译。
- [x] 8.15 保持两个产品现有 Player编译选项与Server Debug配置；将Fantasy Server文件日志锁定到当次RunId目录，禁止Run修改exact file closure保护的ProductRoot。

## 9. 删除旧产品路径

- [x] 9.1 删除旧 `3cDemo/Server/Main/Main.csproj` 与 `Program.cs`。
- [x] 9.2 删除旧通用 `Entity.csproj` 与 `Hotfix.csproj`。
- [x] 9.3 删除硬编码单一 `Hotfix.dll` 的旧 `AssemblyHelper`。
- [x] 9.4 删除旧通用 `Entity.dll/Hotfix.dll/Main.exe` 的构建与脚本假设。
- [x] 9.5 删除公共项目对 DotRecast 与 DotRecastAuthority 的反向引用。
- [x] 9.6 删除旧 concrete `ServerAuthoritativeAuthorityHostRouter`。
- [x] 9.7 删除发布后配置 mutation、旧 config source与兼容 reader。
- [x] 9.8 搜索并删除失效的 `Main.exe`、`Main.csproj`、旧 ModuleId 与旧 route kind引用。
- [x] 9.9 确认仓库只剩两个正式 ServerAuthoritative server product入口。

## 10. 文档、编译与严格校验

- [x] 10.1 更新 `openspec/project.md` 的 Server 代码组织、产品边界与Build/Run描述。
- [x] 10.2 核对本 change 的 spec delta完整覆盖“不同目录不等于不同产品”，不直接改写归档前的current spec。
- [x] 10.3 编译共享 Host、Gate Entity与Gate Hotfix项目并带规定build参数。
- [x] 10.4 编译 Unity Authority Entity、Hotfix与Server入口并带规定build参数。
- [x] 10.5 编译 DotRecast Authority Entity、Hotfix与Server入口并带规定build参数。
- [x] 10.6 编译受影响的 `ThirdPersonClient.Editor` 项目并带规定build参数。
- [x] 10.7 每轮编译后立即执行 `dotnet build-server shutdown`。
- [x] 10.8 publish Unity Authority server product并生成精确product manifest。
- [x] 10.9 publish DotRecast Authority server product并生成精确product manifest。
- [x] 10.10 核对两个发布闭包、配置Scene集合、executable与manifest hash。
- [x] 10.11 确认未运行 Unity batchmode且未新增测试代码。
- [x] 10.12 运行 `openspec validate refactor-server-authoritative-host-products --strict --no-interactive`。
- [x] 10.13 全部任务真实完成后将本文件所有任务更新为 `[x]`。
