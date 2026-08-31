# Change: 建立独立进程的 Rollback GM 文本控制台

## Why

用户确认首版是额外进程里的文本控制台，之后再做图形 UI。GM 命令在独立 .NET 工具进程执行，通过正式只读连接查询 Relay；Unity 客户端不安装游戏内控制台。

## What Changes

- 唯一链路：独立 GM 窗口输入 → 文本解析 → GM HTTP API → 服务端校验和独立处理器 → Relay 查询桥 → Relay 线程快照 → 结果展示。
- 文本前端和 HTTP 服务位于同一个额外进程，未来图形 UI 复用同一 API。
- 显式安装 help、session.info、actor.list、runtime.status 四个只读命令，不反射或执行任意脚本。
- 提供请求身份、开发访问令牌、容量、超时、历史、翻页、清屏和重连。
- 正式产品包含 Player、Relay Server、GM Server 三个 artifact，Run 启动两个 Player、一个 Relay 和一个可交互 GM 控制台，共四个进程。
- 删除误装的 Unity 窗口、InputAction、Prefab 引用和专门输入焦点改动；恢复原 Player HTTP 设置，不保留另一入口。

## Scope

本轮只管理当前一场双端测试。多场目录、四端启动、图形业务面板、玩法 GM、canonical GM 帧命令、采样和 Analyzer 不在范围内。Action、Foot、IK、骨骼及网络算法不修改，最大预测领先量保持 8 Tick。不新增测试代码，不跑 Unity batchmode，不自动归档。

## Impact

新增 rollback-gm-console，产品 delta 涉及 deterministic-rollback-relay-product、network-test-runtime-product-boundary、deterministic-rollback-two-client-demo。删除 character-input-pipeline delta；不修改用户正在编辑的 project.md 或其它 active change。

## 与现行规格的对比

现行三进程拓扑和 Player/Relay 精确清单需要增加独立 GM artifact。Relay 只增加纯 .NET 查询桥，不增加角色模拟权威。共享 GameplayLab Variant 入口保持原样。

GM 不加载 Relay runtime、Unity、Character Program、KCC 或 Presentation；Relay 不安装 GM 命令处理器。原游戏内控制台方案已被用户纠正，全部移除。
