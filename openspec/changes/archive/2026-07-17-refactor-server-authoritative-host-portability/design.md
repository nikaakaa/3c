## Context

普通.NET Host已经可以调用portable Float32 Composer，但ServerAuthoritative Authority的上游输入尚未完全portable。当前Unity Definition负责降低Pipeline，Unity Source preparation创建queue/clock/transport，并在Unity工程中拼接factory。若直接新增DotNet Worker会复制这些职责。

## Parallel Ownership

本change唯一拥有：

- portable ServerAuthoritative Authority Pipeline catalog与factory catalog。
- portable Authority Source runtime、policy、queue、clock和output lowering。
- host-neutral control transport与Authority Host launch request。
- Unity Authority Definition/Source/Fantasy adapter向portable实现的迁移。

本change不得修改：

- WorldBodyBinding、Actor registration签名和具体Solver。
- DotRecast依赖、artifact、query profile或NavigationSurfaceAsset。
- Outer proto、generated代码、Room Handler、Worker manifest和网络Scene。

与共享DotRecast change发生编译接口交汇时，binding迁移由共享Solver线拥有；本change只消费最终registration合同，不编辑其声明文件。

## Target Chain

```text
Unity Authority Definition
  -> portable Authority Pipeline descriptor/catalog

Unity Fantasy adapter
  -> IServerAuthoritativeAuthorityControlTransport

Unity UDP endpoint
  -> portable Authority Source runtime ports

ServerAuthoritativeAuthorityHostLaunchRequest
  -> portable Float32 Composer
  -> runtime handle
```

后续普通.NET Worker只替换Definition读取和transport adapter：

```text
Worker manifest
  -> same descriptor/catalog/policy

Fantasy.Net adapter
  -> same control transport

same UDP endpoint
  -> same Source runtime

same HostLaunchRequest
  -> same Float32 Composer
```

## Authority Pipeline Ownership

Pass顺序、PassId/version、config lowering和factory identity只有portable catalog一份。Unity ScriptableObject只保存authoring字段并调用catalog，不再自行组装descriptor或factory集合。迁移前后的PipelineHash必须完全一致，否则说明语义发生变化，本change不得继续。

## Authority Source Ownership

Portable Source runtime拥有：

- locked Actor route与每Actor command queue。
- fixed authority clock状态与missing-input policy。
- 每Client checkpoint baseline与snapshot sequence。
- reliable event/full checkpoint有界output queue。
- typed Source ports与AuthorityReplicationBatch lowering。

Transport callback只把已解码、已路由的model产品写入有界queue。Source在单一outer tick边界消费。Transport不执行Program、correction或checkpoint policy。

## Control Transport

Control transport只暴露typed host-neutral消息：register result、roster、ticket、heartbeat、reliable event、full checkpoint request/response、leave和failure。它不定义新的DTO codec；Unity Fantasy adapter继续使用现有generated消息映射。

Command/snapshot仍使用现有portable datagram endpoint，不经过control transport抽象。

## Launch Request

Host launch request是内存合同，不是manifest格式。它要求调用方显式提供Program、Backend、Pipeline descriptor/factories、Source policy/runtime ports、roster、WorldSolver、initial state、Committer、diagnostics和output route。缺失任一项直接失败，不选择默认项。

## Migration Rule

迁移按“portable实现先成立 -> Unity adapter切换 -> 删除旧实现”完成，不保留双写。现有Unity Authority Demo的ProgramHash、PipelineHash、Source policy hash、packet bytes和checkpoint bytes必须保持不变。

## Failure Policy

- descriptor或factory不一致：preparation失败。
- queue/clock/transport资源缺失：Source preparation失败。
- Unity adapter未完整映射portable合同：Unity Worker不可用。
- 不回退旧Unity Source、Standard Local或第二factory catalog。

## Implementation Order

1. 盘点现有Unity与portable Authority职责。
2. 迁移Pipeline catalog/factory。
3. 迁移Source policy/runtime与queues。
4. 建立control transport合同。
5. 建立host launch request。
6. 切换Unity adapter并核对identity。
7. 删除旧Unity实现。
