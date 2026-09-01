# 版本化 Rollback GM 文本控制台

Rollback GM 是每个 Deterministic Rollback Run 自己携带和启动的独立工具，不是游戏内面板。终端前端与 GM HTTP 服务位于同一个可见进程；Unity Player 不安装 GM 组件、不处理 GM 输入，也不持有工具访问凭据。

## 启动和进程

在 `Tools/3C/Launcher` 的 Network Test Control Center 中先用显式 CandidateLabel 构建 Deterministic Rollback Candidate，再选择 Candidate 和 `rollback-a` 或 `rollback-b` Slot 启动 Session。

一个运行中的 Rollback Session 包含五个进程：后台 Orchestrator、Dedicated Relay、可见 GM 控制台、Client A 和 Client B。两个 Slot 的端口与窗口资源固定且互不重叠，因此两份不同 Candidate 可以并行运行。Orchestrator 只管理本 Run 的配置、状态和子进程，不进入 Gameplay。

- Enter：提交当前命令。
- ↑/↓：浏览命令历史。
- PgUp/PgDn：查看结果的前后页。
- F5：显式重新连接本 Run 的 GM 服务。
- Ctrl+L：清屏，不取消在途请求、不删除服务端日志。
- Ctrl+C 或关闭 GM 窗口：退出 GM 工具，不停止 Relay 和 Player。

## 命令

| 命令 | 返回 |
| --- | --- |
| help | 当前 Candidate 实际安装的命令目录 |
| help actor.list | 对应说明、版本和用法 |
| session.info | Candidate、Run、Session、Model、Protocol、Program、TickRate和预测上限 |
| actor.list | 预期角色、实际握手、名单锁定和输入前沿 |
| runtime.status | Relay 网络计数、canonical/confirmed、可靠消息与队列 |

命令名区分大小写，以小写字母开头，可含数字、点、横线和下划线。参数以空白分隔，支持引号和引号内转义，不求值 C# 或脚本。

角色存在于配置不等于持续在线；`runtime.status` 不包含客户端 FPS、最终骨骼、IK 或完整 Action 状态。发送请求不代表执行完成，只有 CandidateId、RunId、SessionId、工具身份、请求 Id 和命令版本全部匹配的真实响应才完成记录。

## Candidate、工具和 Run 配置

构建期静态配置为 `Assets/Configs/Development/Gm/RollbackGmToolProfile.asset`。它只保存容量和超时，不保存端口或 token。

Candidate 发布并锁定 `thirdperson.rollback-gm/1` 工具包。工具身份包含 ToolVersion、ProtocolVersion 和 CommandCatalogHash；可执行文件、依赖、静态策略、Tool Manifest 与启动 adapter 都由 Candidate 文件闭包和 BundleHash 约束。运行时不会从仓库 `Tools` 目录寻找新版工具。

Orchestrator 根据所选 Slot 在 `Build/Network/RunLogs/DeterministicRollback/<RunId>/Config` 生成 Relay、Relay Query、GM Server、GM Console 和两个 Peer 的运行配置。端口、RunId、SessionId 和两份 256 bit Bearer token 只属于本 Run；token 不进入 Candidate、命令行或普通日志。

日志位于同一个 Run 的 `Logs` 目录。GM 或 Relay 重启会改变实例身份，旧请求不能交给新实例。GM 退出只使本 Run 的工具不可用；Relay 退出仍按 Gameplay Session 的失败语义处理。

## 扩展边界

新命令实现 `IGmCommandHandler.ExecuteAsync` 并提供 `GmCommandDefinition`，在 `RollbackGmCommandModule.CreateRegistry` 中显式注册。新增状态查询需要补充对应合同和 Relay 查询桥，不读取 Unity Scene 或反射私有状态。

未来图形前端复用 `GET /v1/service` 与 `POST /v1/commands`，继续携带 Bearer、CandidateId、RunId、SessionId、service/relay instance、commandId/version、requestId 和参数；命令业务不进入 UI。

本能力仍只提供只读命令，不包含玩法修改、四客户端、IK遥测、性能采集或场景编排。
