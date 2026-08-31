# Change: 建立 Rollback 服务端 GM 控制台

## Why

用户已将下一步收窄为：先做 Rollback GM 控制台，跑通命令到服务端的完整链路，之后再做玩法 GM、采样和诊断工具。本变更只保留已确认的控制台范围，也不把本地诊断面板当作 GM 服务。

当前纯.NET Relay 已保存会话配置、角色名单、输入与确认前沿、网络计数，但这些信息主要通过日志查看。首版用这些已有服务端事实实现真实可用的查询命令，建立清晰的命令扩展边界。

## What Changes

- 唯一调用链固定为：`控制台输入 -> 命令名/参数解析 -> GM 请求 -> 服务端校验 -> 命令分发 -> 独立处理器 -> 结构化结果 -> 控制台展示`。
- 客户端控制台负责打开/关闭、输入、历史、连接状态和结果展示；GM 命令目录、最终参数校验、权限及分发归服务端。
- 首批只安装四类只读命令：`help [command]`、`session.info`、`actor.list`、`runtime.status`，具体结果字段见 design。
- 每条命令以独立处理器显式注册。控制台不包含具体业务逻辑，分发器不通过越来越大的业务 switch 扩展。
- 明确开发连接身份、服务/会话运行身份、请求关联、结果状态、容量与超时，失败不能回退为本地执行。
- 游戏内输入焦点在正式设备适配边界处理，输入命令不能触发 Attack/Dodge 或相机旋转，不暂停 Rollback Session。
- 只接入当前 Rollback 开发产品。Local Float32/Fixed、Unity Authority、DotRecast Authority 和商业启动不在本轮范围。

## Scope

本轮交付的是能连到服务端、执行四类查询并展示真实结果的 GM 控制台，不是空输入框，也不提前实现后续工具。

以下内容明确不做：

- 改血量、传送、刷角色、强制切动作等玩法状态修改。
- GM 命令进入 canonical Tick、rollback history 或 replay/hash 的新合同。
- 采样开始/停止、联合录制、数据上传、Analyzer、评分与诊断查询界面。
- 移动现有 Foot 采样器、改变 RuntimeDebugSession 所有权或修改已封存数据。
- 修复当前 Action、IK、骨骼或网络算法；已有最大预测领先量 8 Tick 保持不变。
- 任意 C# 执行、反射方法调用、完整运营权限平台或未安装的占位命令。

## Impact

- 新增能力：`rollback-gm-console`。
- 新增输入合同：开发版 Rollback 控制台焦点，不改已有输入历史和请求消费语义。
- 预期实现涉及客户端控制台、GM 命令合同/处理器、服务端查询适配、开发配置及正式 Build/Run 装配。
- 不修改当前 specs 或用户正在编辑的 `openspec/project.md`；只提交本 change 的增量，不能将提案描述为已安装能力。
- 原草案中 `development-gm-console`、`runtime-diagnostic-capture`、`runtime-diagnostic-query` 以及 BTSMTL 诊断和 Foot 存储的修改增量均删除；本轮不再依赖未归档的 Foot 存储/评分 change。
- 不新增测试代码，不跑 Unity batchmode，不在实施任务中写人工验证清单。

## 与现行规格的对比

| 对比项 | 当前口径 | 本轮处理 |
| --- | --- | --- |
| 服务端职责 | Relay 只转发/确认输入，不执行 Program、KCC 或 Presentation | 查询只读 Relay 已有事实；GM 服务模块不取得 Gameplay 权威，也不向 Relay Runtime 塞业务逻辑 |
| 产品闭包和启动 | Relay 产品依赖受限，Rollback Run 固定一个 Relay 加两个客户端 | GM 同进程或独立部署都会涉及不同产品合同，选定宿主后必须补对应 delta，不能偷装服务或沿用旧 manifest |
| 运行状态 | Relay 拥有网络计数、名单和前沿，不拥有客户端骨骼或角色完整状态 | `runtime.status` 明确查询服务端事实；不伪造客户端 FPS、Pose、生命值或 Action 生命周期 |
| 输入 | 正式 Input Adapter 产出 portable input，Program 管请求历史 | 焦点只影响此后设备采集；不改已提交请求、历史或网络队列 |
| 诊断和采样 | 已有独立 Trace、Foot Writer/Analyzer 与 Editor 工作流 | 本轮保持原样；删除旧提案对这些合同的修改 |

## 尚待选择的部署项

服务端 GM 的逻辑职责已经确定，物理宿主尚未选择：可以在现有 Relay 产品进程内安装独立 GM 模块，也可以部署独立工具服务。design 已列出两者业务取舍。选定后须补齐宿主、只读状态来源、工具连接、endpoint、开发权限和产品闭包/拓扑 delta，相关实现才可开始。

这不影响本轮控制台和首批命令的范围，但结构化校验通过不代表部署决策已经完成。
