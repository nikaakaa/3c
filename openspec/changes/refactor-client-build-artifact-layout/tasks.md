# Tasks

## 1. 客户端构建路径合同

- [x] 1.1 新建项目层 ProductBuild Editor 目录与对应 meta。
- [x] 1.2 定义唯一 `ClientBuildArtifactLayout`。
- [x] 1.3 让 layout 从 `Application.dataPath` 规范化 Unity 项目根。
- [x] 1.4 定义唯一正式 `BuildRoot`。
- [x] 1.5 定义 `WorkspaceRoot` 为 `Build/.Workspace`。
- [x] 1.6 定义 `ContentRoot` 为 `Build/Content`。
- [x] 1.7 定义 `PlayersRoot` 为 `Build/Players`。
- [x] 1.8 定义 `NetworkRoot` 为 `Build/Network`。
- [x] 1.9 定义 Content 路径的 BuildTarget segment。
- [x] 1.10 定义 Content 路径的 DefaultPackage segment。
- [x] 1.11 定义 Content 路径的 ResourcePackageVersion segment。
- [x] 1.12 定义 Player 路径的 BuildTarget segment。
- [x] 1.13 定义 Player 路径的 ClientBuildVersion segment。
- [x] 1.14 拒绝空白、`.`、`..` 和路径分隔符版本值。
- [x] 1.15 拒绝任何规范化后离开 `BuildRoot` 的路径。
- [x] 1.16 拒绝把 Workspace 路径当作正式 Content、Player 或 Network root。
- [x] 1.17 拒绝把 `Builds` 或 `Bundles` 解析为正式根。

## 2. TEngine Editor 构建服务收敛

- [x] 2.1 将 TEngine `BuildConfig` 改为要求显式 BuildTarget。
- [x] 2.2 将 TEngine `BuildConfig` 改为要求显式 PackageVersion。
- [x] 2.3 将 TEngine `BuildConfig` 改为要求显式 BuildOutputRoot。
- [x] 2.4 将 TEngine `BuildConfig` 改为在构建 Player 时要求显式 PlayerOutputPath。
- [x] 2.5 删除 `BuildConfig` 的 `./Builds/` 默认值。
- [x] 2.6 删除按日期分钟生成 PackageVersion 的默认方法。
- [x] 2.7 删除按平台返回 `Build/Windows`、`Build/Android`、`Build/IOS` 的默认 Player 路径方法。
- [x] 2.8 定义不引用 ProductStartup 类型的 TEngine Content build request。
- [x] 2.9 定义不引用 ProductStartup 类型的 TEngine Player build request。
- [x] 2.10 定义结构化 TEngine Content build result。
- [x] 2.11 定义结构化 TEngine Player build result。
- [x] 2.12 让 Content build result 返回 YooAsset 输出版本目录。
- [x] 2.13 让 Content build result 返回 BuildReport 与失败信息。
- [x] 2.14 让 Player build result 返回 BuildReport 与失败信息。
- [x] 2.15 保留构建前 HybridCLR 热更 DLL 编译和复制步骤。
- [x] 2.16 让热更 DLL 步骤失败时终止后续 YooAsset 构建。
- [x] 2.17 让 TEngine 服务只使用调用方提供的输出路径。
- [x] 2.18 删除 Windows 一键构建的 `Builds/Windows` 写入。
- [x] 2.19 删除 Android 一键构建的 `Bundles` 写入。
- [x] 2.20 删除 iOS 一键构建的 `Bundles` 写入。
- [x] 2.21 删除 F8 一键构建的隐式路径与隐式版本入口。
- [x] 2.22 删除通用 TEngine BuildPipelineWindow 的正式产品菜单入口。
- [x] 2.23 保持 TEngine 构建服务不引用 ClientBuildArtifactLayout、ProductStartupProfile 或商业产品 manifest。

## 3. 商业客户端正式构建入口

- [x] 3.1 为 `ThirdPersonClient.Editor` 增加 `TEngine.Editor` 正式程序集引用。
- [x] 3.2 定义 `CommercialClientBuildRequest`。
- [x] 3.3 在 request 中保存显式 BuildTarget。
- [x] 3.4 在 request 中保存显式 ResourcePackageVersion。
- [x] 3.5 在 request 中保存显式 MinimumClientBuildVersion。
- [x] 3.6 从唯一 ProductStartupProfile 读取 ClientBuildVersion。
- [x] 3.7 禁止从目录名、文件时间或 EditorPrefs 推断三类版本。
- [x] 3.8 定义 `CommercialClientBuildResult`。
- [x] 3.9 新建唯一 `CommercialClientBuildWorkflow`。
- [x] 3.10 新建唯一商业客户端构建窗口和菜单入口。
- [x] 3.11 让构建窗口只显示由 layout 推导的只读正式目标路径。
- [x] 3.12 让构建窗口在执行前显示 Client、Minimum Client 与 Resource 三类版本。
- [x] 3.13 提供正式 Content 构建命令。
- [x] 3.14 提供正式 Player 构建命令。
- [x] 3.15 提供按 Content 后 Player 固定顺序执行的完整构建命令。
- [x] 3.16 禁止构建窗口接受任意 OutputRoot。
- [x] 3.17 禁止构建窗口接受任意 PlayerOutputPath。
- [x] 3.18 在构建前校验 ProductStartupProfile 和版本字段。
- [x] 3.19 在构建前校验目标正式版本目录不存在。
- [x] 3.20 在构建前创建受 BuildRoot 约束的唯一 Workspace candidate。

## 4. Content staging 与正式发布闭包

- [x] 4.1 让商业 Content 工作流把 TEngine/YooAsset 原始输出写入 Workspace。
- [x] 4.2 保持 YooAsset `OutputCache` 只位于 Workspace。
- [x] 4.3 保持 YooAsset BuildReport 只位于 Workspace。
- [x] 4.4 从成功 BuildResult 取得精确版本目录。
- [x] 4.5 从 YooAsset manifest 取得正式 Bundle 文件集合。
- [x] 4.6 将 version 文件加入候选发布闭包。
- [x] 4.7 将 manifest bytes、hash 与 json 加入候选发布闭包。
- [x] 4.8 将 manifest 引用的 Bundle 加入候选发布闭包。
- [x] 4.9 拒绝候选闭包包含 `OutputCache`。
- [x] 4.10 拒绝候选闭包包含 BuildReport。
- [x] 4.11 拒绝候选闭包包含 `Simulate` 或 `Simulate-*` 文件。
- [x] 4.12 定义 canonical StartupPolicy Editor writer。
- [x] 4.13 使用显式 MinimumClientBuildVersion 生成 `StartupPolicy.json`。
- [x] 4.14 让生成的 StartupPolicy 通过当前 Runtime parser 合同。
- [x] 4.15 定义 `CommercialContentReleaseManifest` schema。
- [x] 4.16 在 release manifest 中记录 BuildTarget。
- [x] 4.17 在 release manifest 中记录 DefaultPackage identity。
- [x] 4.18 在 release manifest 中记录 ResourcePackageVersion。
- [x] 4.19 在 release manifest 中记录 MinimumClientBuildVersion。
- [x] 4.20 在 release manifest 中记录每个文件的相对路径、长度与 hash。
- [x] 4.21 校验候选目录与 release manifest exact file closure 一致。
- [x] 4.22 校验候选目录不存在路径逃逸和重复大小写路径。
- [x] 4.23 只在全部校验成功后原子发布正式 Content 版本目录。
- [x] 4.24 让相同 ResourcePackageVersion 已存在时明确失败。
- [x] 4.25 让 Content 构建失败只清理自己的 transient candidate。
- [x] 4.26 禁止 Content 构建失败修改既有正式版本。

## 5. Player staging 与正式发布闭包

- [x] 5.1 让商业 Player 工作流把 Unity Player 原始输出写入 Workspace。
- [x] 5.2 使用 ProductStartupProfile 的 ClientBuildVersion 构造正式 Player 目录。
- [x] 5.3 定义 `CommercialPlayerReleaseManifest` schema。
- [x] 5.4 在 Player manifest 中记录 BuildTarget。
- [x] 5.5 在 Player manifest 中记录 ClientBuildVersion。
- [x] 5.6 在 Player manifest 中记录配套内置资源身份。
- [x] 5.7 在 Player manifest 中记录 executable 与 Data 目录闭包 hash。
- [x] 5.8 校验 Player executable 存在且位于 candidate 内。
- [x] 5.9 校验 Player Data 目录位于 candidate 内。
- [x] 5.10 校验 candidate 与 Player manifest exact file closure 一致。
- [x] 5.11 只在全部校验成功后原子发布正式 Player 版本目录。
- [x] 5.12 让相同 ClientBuildVersion 已存在时明确失败。
- [x] 5.13 让 Player 构建失败只清理自己的 transient candidate。
- [x] 5.14 禁止 Player 构建失败修改既有正式版本。

## 6. YooAsset Editor 输出隔离

- [x] 6.1 将 YooAsset Editor 默认输出根改为 `Library/YooAsset/BuildOutput`。
- [x] 6.2 让 EditorSimulate 的 `Simulate-*` 目录只写入新 Library 根。
- [x] 6.3 让 YooAsset 原始 Builder 的默认输出只写入新 Library 根。
- [x] 6.4 保持商业 Content 工作流通过显式 request 写入自己的 Workspace。
- [x] 6.5 删除根目录 `Bundles` 的运行时或 Editor 路径引用。
- [x] 6.6 删除已跟踪的 `Bundles/DefaultPackage/Simulate` 文件。
- [x] 6.7 删除未跟踪的旧 `Bundles/Simulate-*` 生成目录。
- [x] 6.8 删除旧 `Builds` 生成目录。
- [x] 6.9 删除无版本旧 `Build/Client` Player 目录。
- [x] 6.10 删除无版本旧 `Build/Client_ServerAu` Player 目录。
- [x] 6.11 保留现有 `Build/Network` 正式产品目录。

## 7. Network Product 路径接入

- [x] 7.1 让 `NetworkTestProductBuildWorkflow` 从 `ClientBuildArtifactLayout.NetworkRoot` 取得根路径。
- [x] 7.2 保持 UnityAuthority ProductRoot 不变。
- [x] 7.3 保持 DotRecastAuthority ProductRoot 不变。
- [x] 7.4 保持 DeterministicRollback ProductRoot 不变。
- [x] 7.5 保持 `Build/Network/RunLogs/<Model>/<RunId>` 不变。
- [x] 7.6 保持 Network workspace、staging 和原子替换语义不变。
- [x] 7.7 禁止 Network Build 写入 Content 或 Players 分区。
- [x] 7.8 禁止商业 Content/Player Build 写入 Network 分区。

## 8. 旧配置与版本默认清理

- [x] 8.1 从 TEngine UpdateSetting 删除未使用的 `isAutoAssetCopeToBuildAddress` 字段。
- [x] 8.2 从 TEngine UpdateSetting 删除未使用的 `BuildAddress` 字段。
- [x] 8.3 删除对应 `IsAutoAssetCopeToBuildAddress` getter。
- [x] 8.4 删除对应 `GetBuildAddress` getter。
- [x] 8.5 从 `TEngineUpdateSettings.asset` 删除旧序列化字段。
- [x] 8.6 全局删除 `../../Builds/Unity_Data/StreamingAssets`。
- [x] 8.7 全局删除 `./Builds/` 正式默认值。
- [x] 8.8 全局删除项目根 `Bundles` 正式输出引用。
- [x] 8.9 全局删除无版本 `Build/Windows/Release_Windows.exe` 输出引用。
- [x] 8.10 全局删除按日期分钟推断 PackageVersion 的调用。

## 9. Git 与 Repository Policy

- [x] 9.1 在根 `.gitignore` 明确忽略客户端 `Bundles` 旧根。
- [x] 9.2 在根 `.gitignore` 明确忽略客户端 `HybridCLRData`。
- [x] 9.3 保持客户端 `Build` 与 `Builds` 整体忽略。
- [x] 9.4 将 `Bundles` 加入客户端生成根 Repository Policy。
- [x] 9.5 将 `HybridCLRData` 加入客户端生成根 Repository Policy。
- [x] 9.6 保持 Repository Policy 只读取 Git 索引。
- [x] 9.7 让已跟踪旧模拟清单触发明确违规。
- [x] 9.8 让未跟踪的本机 Build、Library、HybridCLRData 不影响候选提交结论。

## 10. 文档与项目口径

- [x] 10.1 更新 `openspec/project.md` 客户端正式产物三分区。
- [x] 10.2 更新 `openspec/project.md` 商业客户端构建入口归属。
- [x] 10.3 更新商业启动文档的 Content 构建路径。
- [x] 10.4 更新商业启动文档的 Player 构建路径。
- [x] 10.5 说明 Workspace、OutputCache 和 BuildReport 不属于发布闭包。
- [x] 10.6 说明本地 HTTPS 服务直接选择一个正式 Content 版本目录。
- [x] 10.7 说明远端 CDN 上传和证书部署不在本变更范围。
- [x] 10.8 删除文档中把 `Builds` 或 `Bundles` 当正式发布根的说明。
- [x] 10.9 记录 YooAsset embedded Editor 输出 patch 的升级审查点。
- [x] 10.10 记录 HybridCLRData 只是本机工具工作区而不是发布源。

## 11. 编译与规格校验

- [x] 11.1 编译受影响的 `TEngine.Editor` 程序集。
- [x] 11.2 编译受影响的 `YooAsset.Editor` 程序集。
- [x] 11.3 编译受影响的 `ThirdPersonClient.Editor` 程序集。
- [x] 11.4 所有 dotnet build 使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 11.5 构建结束后立即执行 `dotnet build-server shutdown`。
- [x] 11.6 运行 Repository Policy 脚本。
- [x] 11.7 运行 `openspec validate refactor-client-build-artifact-layout --strict --no-interactive`。
- [x] 11.8 运行 `openspec validate --all --strict --no-interactive`。

