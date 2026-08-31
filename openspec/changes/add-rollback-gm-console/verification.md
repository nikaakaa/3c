# 独立 GM 控制台实施记录

## 交付形态

用户纠正后的最终入口是额外 GM 进程的标准终端窗口。文本前端和 HTTP 服务同进程；终端通过同一服务 API 调用命令，GM 再通过查询桥读取 Relay。窗口名为 3C Rollback GM，由本机 Windows Terminal 承载。

游戏内窗口、InputAction、Prefab 接入、专用设备焦点及 Player 明文 HTTP 改动已经全部撤回。输入适配器、角色 Host 和 Rollback Prefab 与加入 GM 前内容一致。构建配置属于 Editor-only，Player 不携带 GM 连接配置或凭据。

## 构建与运行结果

- 日期：2026-09-01。
- 正式 BuildId：20260831-171216。
- 正式 RunId：20260901-011937。
- GM 与 Relay .NET 项目均零警告、零错误构建，并执行 build-server shutdown。
- Unity Editor 编译通过；使用现有 Build Deterministic Rollback 入口完成 Player、Relay、GM 原子发布，未运行 batchmode。
- 通过正式 Start-DeterministicRollbackDemo.ps1 校验产物并启动四个业务进程，GM 终端窗口已创建。
- GM artifact 不包含 ThirdPersonSimulation runtime DLL；Relay artifact 不包含 GM 命令处理器 DLL。
- Gm/GmConsoleManifest.json 存在；旧 Player/3C_Client_Data/StreamingAssets/RollbackGmClient.json 不存在。

## 命令链结果

help、session.info、actor.list、runtime.status 均返回 Success，响应 requestId 与请求一致。两名角色实际握手完成且名单锁定，输入前沿和 canonical/confirmed 正常推进。session.info 返回 TickRate=60、最大预测领先量=8、确认延迟=4。观察时 invalidInputs=0、droppedDatagrams=0。

拒绝路径均返回预期结果：未知命令 UnknownCommand、额外参数 InvalidArguments、旧运行实例 TargetEnded、错误命令版本 VersionMismatch、错误访问 token HTTP 401。操作日志包含接受/完成或拒绝结果，不记录 token。

另外直接使用已发布的 GmConsoleModel 与 GmHttpConsoleConnection 调用 help "actor.list"，验证文本解析、服务调用、结果格式化、历史和本地清屏。没有增加测试程序集、测试工程或测试源码。

## 验证边界

本记录证明正式产物、进程启动、客户端状态层和服务端查询链已经工作；未代替用户逐项验收终端键盘操作，也未进行 Action/IK 的玩法端到端验收。当前运行日志没有观察到启动/空闲阶段的 Presentation exception，不代表已有 Action 问题已经修复。

多场会话目录、四端启动、图形 UI、玩法修改和采样不在本轮。没有自动归档本 change。

## 日志与入口

- 日志：3cDemo/Client/3C_Client/Build/Network/RunLogs/DeterministicRollback/20260901-011937。
- 终端入口：3cDemo/Server/Products/DevelopmentGm/GmTerminalConsole.cs。
- 服务入口：3cDemo/Server/Products/DevelopmentGm/Program.cs。
- 命令分发：3cDemo/Server/Shared/Development/Gm.Server/GmCommandDispatcher.cs。
- Relay 查询桥：3cDemo/Server/Shared/Development/RollbackGm.RelayBridge/RollbackRelayQueryBridge.cs。
