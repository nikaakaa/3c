# Change: 将ServerAuthoritative Authority Host运行合同迁为Portable

## Why

当前ServerAuthoritativeHybrid的模型产品、Prediction/Authority Pipeline与网络语义已经成立，但Authority Pipeline descriptor、Pass factory集合、Source queue/clock和Fantasy control外层仍有Unity Definition或Unity runtime创建点。普通.NET Worker若直接接入，只能复制Unity Authority Source与Pipeline拼装，形成第二条权威路径。

本change在不改变现有Unity四进程Demo行为和协议bytes的前提下，把Authority Host的纯C#运行职责收敛到现有portable ServerAuthoritative source set。Unity Definition继续作为authoring/transport adapter，普通.NET Host以后可构造同一portable launch request。

该工作与DotRecast Solver无关，可以和`add-shared-dotrecast-navigation-solver`并行实施。

## Dependencies

- `refactor-gameplay-session-composition-boundary` MUST已归档。
- `refactor-server-authoritative-hybrid-runtime`代码 MUST完成并冻结；本change可在其归档前实施，但最终spec合并前必须先归档前置change。
- 本change MUST保持现有PipelineId/Revision/Hash、Source policy hash、protocol bytes和Unity Demo外部行为不变。

## What Changes

- 将Authority Pipeline descriptor构造、Pass config lowering与factory catalog迁入portable ServerAuthoritative source set。
- 建立host-neutral `ServerAuthoritativeAuthoritySourceRuntime`，唯一拥有每Actor command queue、authority clock、每Client checkpoint baseline、reliable output queue与Source ports。
- 建立host-neutral `IServerAuthoritativeAuthorityControlTransport`，只交换既有roster、ticket、heartbeat、reliable event、full checkpoint与failure产品。
- 建立`ServerAuthoritativeAuthorityHostLaunchRequest`，接收Program Runtime、Backend、Authority Pipeline、Source policy、roster、WorldSolver、initial state、transport ports和output ports，并调用唯一portable Float32 Composer。
- 让现有Unity Authority Source/Definition/Fantasy adapter委托portable catalog、runtime和launch request。
- 保持direct UDP command/snapshot endpoint、Network Checkpoint codec和model products不变。
- 删除Unity专属Authority queue、clock、Pipeline拼装、factory集合与重复packet mapper。

## Non-Goals

- 不新增DotRecast、Navigation artifact、WorldBodyBinding抽象或任何Solver实现。
- 不新增普通.NET Worker executable、manifest、Fantasy.Net adapter或协议字段。
- 不修改Room Handler、generated `.g.cs`、command/snapshot wire format或checkpoint schema。
- 不修改Prediction Pipeline、correction history或双客户端Scene。
- 不引入兼容Source、双写queue或Unity/portable两套factory fallback。

## Current Spec Comparison

- `gameplay-simulation-session-composition`已要求普通.NET Host调用同一portable Float32 Composer，本change补齐Authority Source/Pipeline到Composer之间缺失的host-neutral装配输入。
- `server-authoritative-hybrid-sync-model`当前将Authority Worker具体写成Unity外层。本change保持现有Unity Worker作为唯一已安装Host，但把其内部Authority运行合同改成可由其它Host复用。
- 本change不提前宣称DotNet Worker可运行；最终Worker、manifest和握手由`add-dotrecast-authoritative-server-backend`交付。

## Impact

- 新能力：`server-authoritative-host-portability`。
- 修改能力：`server-authoritative-hybrid-sync-model`。
- Portable：Authority Pipeline catalog、Source runtime、control transport与host launch request。
- Unity：Definition、Source preparation与Fantasy adapter降为正式adapter。
- 删除：Unity专属Authority pipeline/source运行实现和重复factory/queue路径。
