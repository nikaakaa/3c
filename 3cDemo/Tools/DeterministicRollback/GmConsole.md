# 独立进程的 Rollback GM 文本控制台

当前是在额外 GM 进程的终端窗口输入命令，不是游戏内面板。图形 UI 留待后续；文本前端和 GM HTTP 服务现在同处一个进程，并使用同一服务 API。Unity Player 不安装 GM 组件或输入焦点，也不持有工具访问凭据。

## 启动和使用

使用项目现有 Tools/3C/Internal/Build Deterministic Rollback 构建，再通过 Tools/3C/Internal/Run Deterministic Rollback 或 Start-DeterministicRollbackDemo.ps1 启动。

Run 校验既有产物后依次启动 Relay、可见的 3C Rollback GM 控制台窗口、Client A、Client B，共四个进程。两个游戏窗口之外的 GM 窗口就是命令入口。

- Enter：提交当前命令。
- ↑/↓：浏览命令历史。
- PgUp/PgDn：查看结果的前后页。
- F5：显式重新连接当前配置的 GM 服务。
- Ctrl+L：清屏，不取消在途请求、不删除服务端日志。
- Ctrl+C 或关闭 GM 窗口：退出工具，不停止 Relay 和 Player。

## 命令

| 命令 | 返回 |
| --- | --- |
| help | 实际已安装的命令 |
| help actor.list | 对应说明、版本和用法 |
| session.info | Build/Session/Model/Protocol/Program、TickRate、最大预测领先量 |
| actor.list | 预期角色、实际握手、名单锁定和输入前沿 |
| runtime.status | Relay 网络计数、canonical/confirmed、可靠消息与队列 |

命令名区分大小写，以小写字母开头，可含数字、点、横线和下划线。参数以空白分隔，支持引号和引号内转义，不求值 C# 或脚本。

角色在配置中存在不等于持续在线；runtime.status 不包含客户端 FPS、最终骨骼、IK 或完整 Action 状态。发送不代表执行完成，只有匹配请求 Id 和目标运行身份的真实响应才会完成记录。

## 模块与配置

链路是：终端输入 → 解析与客户端状态层 → GM HTTP API → 校验与独立处理器 → Relay HTTP 查询桥 → Relay 线程快照 → 结构化结果 → 终端显示。

构建配置为 Assets/Configs/Development/Gm/RollbackGmBuildProfile.asset，类型属于 Editor-only 程序集。控制台、客户端状态和 HTTP 连接只在 .NET 工具侧运行，不进入 Unity Player。

默认 GM 地址为 http://127.0.0.1:24200/，Relay 查询为 http://127.0.0.1:24201/。只允许本机，不启用 CORS，不搜索备用地址。Build 生成两个不同的 256 bit 开发 Bearer token；不把 token 写进源码、命令行或日志。

正式配置是 Gm/GmServerManifest.json、Gm/GmConsoleManifest.json、Server/RelayQueryManifest.json。启动辅助脚本随 Gm artifact 发布。这些文件均纳入产品文件集合和 hash；Run 不生成配置、不重新构建、不修复旧产品。

默认 HTTP 消息 64 KiB，控制台在途 8 条，GM 并发 16 条，Relay 队列 32 项且每轮最多读取 2 项。历史 32 条，输出 64 条，每条最多 4096 字符；控制台超时 5 秒，GM 到 Relay 查询超时 2 秒。超时或断线不自动重发，不以缓存冒充结果。

GM 或 Relay 重启改变运行身份，旧请求不能交给新实例。GM 日志保存在当前 RunLogs 目录的 runId-gm.log，不在产物目录写日志。

## 扩展与范围

新命令实现 IGmCommandHandler.ExecuteAsync 并提供 GmCommandDefinition，在 RollbackGmCommandModule.CreateRegistry 显式注册。新状态查询补充对应合同和 Relay 查询桥，不读取 Unity Scene 或反射私有状态。

未来 UI 复用 GET /v1/service 与 POST /v1/commands，携带 Bearer、serviceInstanceId、sessionId、commandId/version、requestId 和参数；不要把命令业务搬进 UI。

本轮仍只管理一场双端。多场目录、四端启动、图形面板、采样、玩法修改均未安装；Build 仍构建完整产品，没有单独增量发布 GM 的入口。玩法修改需要另行定义执行 Tick、canonical、回放和 hash 合同。Action 和 IK 问题不在本轮修复。
