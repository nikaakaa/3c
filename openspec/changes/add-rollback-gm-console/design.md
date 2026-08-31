## Context

本轮交付独立进程的 Rollback GM 文本控制台。用户明确不把输入窗口放在游戏里，图形 UI 留待后续。

## Goals / Non-Goals

- 一个额外 .NET 进程同时拥有文本前端和 GM HTTP 服务，作者在该进程的终端窗口输入命令。
- 前端通过同一服务 API 提交请求，不另开直接执行处理器的路径；未来 GUI 复用合同。
- 只做当前一场双端的四个只读命令，不做多场调度、四端产品、玩法修改或采样。
- 不改 Unity 玩家输入、Prefab、Action、IK、8 Tick 配置或 Gameplay 同步。

## Decisions

### 1. 模块责任

| 模块 | 输入 | 输出 | 不负责 |
| --- | --- | --- | --- |
| 文本控制台 | 终端按键、文本、服务响应 | 请求、历史、结果、连接状态 | 命令业务、Unity 场景 |
| GM 客户端状态层 | 解析结果、连接和响应 | 关联、超时、有界输出 | 最终授权、本地替代执行 |
| GM 服务 | 已认证请求、目录、目标会话 | 准入结果和处理器调用 | Unity 角色内存 |
| Relay 查询桥 | session/actors/runtime 查询 | Relay 线程生成的快照 | GM 命令、Gameplay 写入 |

### 2. 首批命令

| 命令 | 参数 | 返回 |
| --- | --- | --- |
| help [command] | 可选命令名 | 实际目录、说明、版本、用法 |
| session.info | 无 | Build/Session/Model/Protocol/Program、TickRate、预测上限 |
| actor.list | 无 | 预期 Peer/Player/Actor、实际握手、名单锁定、输入前沿 |
| runtime.status | 无 | Relay 收发、转发、去重、非法输入、canonical/confirmed、可靠消息及队列 |

名单中的角色不等于持续在线。服务端没有客户端 FPS、骨骼、IK 或完整 Action 状态，不能伪造结果。

### 3. 终端交互和清理

GM 使用可见的独立控制台窗口，Relay 在后台运行。Enter 提交，↑/↓ 历史，PgUp/PgDn 结果翻页，F5 显式重连，Ctrl+L 本地清屏，Ctrl+C 退出。关闭 GM 不停止 Relay 或 Player。

输入最多 2048 字符、16 个参数，支持引号及引号内转义，不执行脚本。终端绘制只读取缓存，不等待网络。

删除 Unity GmConsoleView、UnityGmHttpConnection、RollbackGmConsoleBootstrap、GM InputAction 和输入焦点改动，不保留游戏内开关。控制台状态层迁到 Server/Shared/Development/Gm.Client。Player HTTP 配置恢复原值，不给 Player 下发工具凭据。

### 4. 正式配置和身份

本机 IPv4 HTTP/JSON：GM 默认 24200，Relay 查询默认 24201；不监听局域网、不启用 CORS。控制台访问 GM、GM 访问 Relay 分别使用 Build 随机生成的 256 bit Bearer token，不复用 gameplay peer 身份，不作为商业账号系统。

Editor-only 配置为 Assets/Configs/Development/Gm/RollbackGmBuildProfile.asset，不引用 InputAction 或字体。Build 发布 Gm/GmServerManifest.json、Gm/GmConsoleManifest.json、Server/RelayQueryManifest.json；全部进入 exact closure，Run 不生成或修复配置。Player 不包含工具连接配置。

请求携带唯一请求 Id、GM 与 Relay 组合运行身份、SessionId、commandId/version 和参数。服务端最终校验权限、身份、版本和参数；日志记录接受/拒绝及完成，不包含凭据。未知命令、参数错误、无权限、目标结束、超时和执行失败明确区分。

GM 或 Relay 重启改变运行身份。旧请求不得交给新实例，超时和断线不自动重发，不回退到其它服务或本地执行。服务不可用不能用缓存冒充本次结果。

### 5. 有界异步查询

HTTP 消息上限 64 KiB；GM 默认并发 16 条；Relay 队列 32 项、每轮 Pump 最多处理 2 项。控制台在途 8 条、历史 32 条、输出 64 条且每条最多 4096 字符。控制台超时 5 秒，GM 服务超时 4 秒，GM 到 Relay 查询超时 2 秒。

处理器使用异步查询端口。Relay 网络线程只入队，读取在 Relay 所有者线程完成，续体不在 Relay 线程等待。查询不保存第二份可变状态。

### 6. 产品和生命周期

ThirdPerson.Development.Gm.Service 是额外的控制台 executable，文本前端和 HTTP 服务同进程。GM 只依赖工具合同、命令和 HTTP 组件，不依赖 Endpoint runtime 或 Unity。Relay 只增加查询桥，不加载 GM 处理器。

产品发布 Player、Server、Gm 三个 artifact，topology 为 thirdperson.runtime-topology.deterministic-rollback.gm-relay-two-peers.v1。Run 验证文件后启动 Relay 并检查查询身份，再启动可见 GM 控制台并检查服务身份，最后启动两个 Player。辅助脚本随 GM artifact 发布并参与 hash，启动失败只回收本次进程。

GM 停止只让工具不可用；Relay 停止保持既有 Session 失败语义。首版 Build 仍构建完整产品，不宣称已实现单独增量发布 GM 或多场调度。

## 与现行规格的对比

产品和 Demo 的三进程条款提供对应 delta；canonical、rollback history、Gameplay hash、Presentation 和共享 GameplayLab 入口不改变。用户纠正后删除 character-input-pipeline delta，已加入的游戏内组件和输入改动须完全撤回。

## 后续工作

图形 UI、多场目录、批量调度、四端、玩法 GM、客户端采样与诊断分别作为后续能力。Action playback command has no matching Select 保持已知未修复。
