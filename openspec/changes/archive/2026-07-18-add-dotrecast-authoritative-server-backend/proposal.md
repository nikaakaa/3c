# Change: 集成Fantasy Authority Scene内的DotRecast权威端与双客户端环境

## Why

当前ServerAuthoritativeHybrid已经交付Unity CharacterController Authority环境；共享DotRecast Solver与Authority Host portable边界分别由`add-shared-dotrecast-navigation-solver`和`refactor-server-authoritative-host-portability`负责。最终仍缺一个正式集成：Fantasy Server内的独立Authority Scene必须加载同一Corin Program、Authority Pipeline和NavigationSurfaceArtifact，两个Unity客户端必须用同一DotRecast Solver进行Prediction，并通过既有Fantasy控制面与UDP数据面完成权威checkpoint、restore/replay和remote presentation。

当前集成运行后暴露出一个明确的世界求解缺口：`DotRecastWorldSolver.ResolveBatch`虽然一次接收完整Actor roster，却逐Actor独立执行`MoveAlongSurface`，只裁决静态Navigation Surface，不裁决Actor之间的硬接触。结果是墙体可以阻挡角色，但两个玩家可以互相穿透。该缺口不能交给Presentation、客户端Transform修正、RVO或网络层补偿；它必须在同一次正式World ResolveBatch内产生唯一FinalBody，Prediction与Authority再共同消费同一结果。

原提案把DotRecast Authority设计为连接Fantasy Gate的外部普通.NET Worker，因此要求第四个进程和Fantasy Console Client adapter。对照`Ref/94.移动同步前后端完整代码`后确认，该部署没有业务必要：纯C# DotRecast、Float32 Program和portable Authority Host可以直接运行在Fantasy Server的独立Authority Scene中。参考工程同样由Map Scene拥有权威移动，Gate只负责外部连接和路由。外部Worker形式只对必须运行Unity CharacterController的Unity Authority环境成立，不应反向约束DotRecast环境。

本change不复制参考工程上传客户端position、100ms移动段或服务端Transform插值的业务实现。DotRecast客户端仍只发送canonical input与tick identity，Authority Scene从自己的committed Character/World state独立执行Corin Program和共享DotRecast Solver。

## Dependencies

- `refactor-server-authoritative-hybrid-runtime` MUST已归档，现有Prediction/Authority Pipeline、Fantasy Room、direct UDP endpoint、Network Checkpoint和correction history为current truth。
- `add-shared-dotrecast-navigation-solver` MUST先完成，交付抽象WorldBodyBinding、state-only DotRecast binding、唯一DotRecast源码、NavigationSurfaceArtifact、共享Solver和Unity Solver Definition。
- `refactor-server-authoritative-host-portability`与`refactor-float32-session-runtime-launcher-boundary` MUST先完成，交付portable Authority Pipeline catalog、neutral Runtime Package、Source runtime、Authority Runtime Launcher、control transport和Host launch request。
- 本change MUST不复制Source、Pipeline、Composer或datagram codec；依赖缺失时停止。对既有DotRecast能力的修改只限于将静态Surface候选解与Actor硬接触组合进同一个`DotRecastWorldSolver.ResolveBatch`，Navigation artifact、Recast query和surface约束算法继续保持唯一实现。

## What Changes

- 保持`ServerAuthoritativeHybridModelDefinition`只拥有网络协议、Prediction/Authority Pipeline Pair、同步策略和能力要求；具体Program Runtime、Backend与Solver继续由Client Composition或Authority Scene manifest选择。
- 将现有`DotNetAuthorityWorkerManifest`迁移为`DotRecastAuthoritySceneManifest` canonical格式，删除Control endpoint、WorkerId和外部process role，改为锁定Fantasy process/Authority Scene、Room、Data endpoint、Program、Pipeline、Source policy、roster、Host、Solver、World、Navigation artifact、QueryProfile、clock、Transport和diagnostics身份。
- 在Fantasy Server配置中新增独立DotRecast Authority Scene。该Scene构造portable Authority Source与其Runtime Launcher，由Launcher通过Host launch request进入唯一Float32 Composer创建runtime，不引用Unity、UnityEngine、CharacterController或Unity Hotfix gameplay实现。
- 将Fantasy Gate Room从“只保存外部Worker Session”重构为“锁定唯一Authority Host route”。Unity环境使用现有外部Unity Worker route；DotRecast环境使用Fantasy Inner/Address消息连接Gate Scene与Authority Scene，不增加Worker到Gate的Console Client连接。
- 新增Fantasy Server内的host-neutral control adapter，实现既有`IServerAuthoritativeAuthorityControlTransport`。Adapter只把注册、roster、ticket、heartbeat、reliable event、full checkpoint、leave和failure映射为portable Source产品；routine command/snapshot继续复用既有direct UDP endpoint。
- 扩展正式Outer客户端协议与Inner Scene协议，锁定AuthorityHostProfile、Solver、World、Map、NavigationSurfaceArtifact和QueryProfile身份；只通过正式ProtocolExportTool生成代码。
- 保持Client command只包含canonical input、sequence、target tick和route identity，不包含position、Transform、Body、applied displacement或DotRecast查询结果。
- 新增DotRecast Prediction Composition和独立DotRecast Client Scene。Client A/B以不同launch identity复用同一Scene；owner使用state-only binding与共享DotRecast Solver，remote actor只消费权威replication。
- 将DotRecast客户端、Unity Authority客户端与Unity Authority Worker使用的测试地图收口为同一canonical环境Prefab；Navigation authoring直接读取该Prefab并发布NavigationSurfaceArtifact，删除仅含平面的旧导航源Scene。
- 为DotRecast Actor binding增加显式且进入WorldConfigurationHash的接触形状配置；角色半径、高度、skin、固定迭代次数和最大去穿透距离不得从Scene Collider、默认值或网络包猜测。
- 将`DotRecastWorldSolver.ResolveBatch`收敛为唯一两阶段求解：先为全部Actor生成受Navigation Surface约束的候选位移，再由portable `ActorContactSolver`按稳定ActorId pair顺序执行连续圆盘扫掠、垂直区间过滤、切向滑动和有界去穿透，最后重新约束到Navigation Surface并一次性生成全部FinalBody与WorldSolveResult。
- Actor硬接触采用运动学body-block语义：静止Actor不会仅因另一个Actor主动移入而被通用接触层推走；双方同时移动时只裁剪彼此的闭合分量并保留合法切向分量。攻击、冲刺、击退或霸体需要改变该语义时，后续必须通过正式Gameplay Motion/Action authoring扩展接触策略，不得在Solver内按状态名硬编码。
- Prediction与Authority必须加载相同接触配置并形成相同Solver/World identity；网络仍只同步canonical input与最终权威checkpoint，不新增客户端碰撞结果、接触对或位姿权威字段。
- 为接触候选、TOI、pair顺序、裁剪量、去穿透次数、surface重新约束和失败原因增加只读结构化诊断；诊断不得反向驱动求解。
- 保留现有Unity CC Authority Scene与四进程启动方式。DotRecast环境改为Fantasy Server、Client A、Client B三个OS进程；两种环境使用独立server/player build目录、Fantasy配置和日志目录。
- 删除外部DotRecast Worker executable、Fantasy Console Client adapter、Worker publish目录、DotRecast外部Worker register路径及其旧manifest命名，不保留兼容reader或桥接。

## Non-Goals

- 不实现或修改DotRecast查询、Navigation artifact codec、WorldBodyBinding或Solver Definition。
- 不实现或修改Authority Pipeline catalog、Source queue/clock、checkpoint baseline、portable Host launch request或通用UDP codec。
- 不实现完整通用KCC、刚体动力学、动态障碍、DetourCrowd/RVO、Deterministic Rollback、命中判定或combat rewind。
- 不在本change实现攻击推人、击退、霸体、ghost、队伍穿透或基于Action状态的动态接触策略；这些业务语义后续只能通过正式Program/Motion合同进入同一ActorContactSolver，不能新增Action专属碰撞路径。
- 不把Program、Source、Solver或权威state放进Gate Scene；同一OS进程不等于同一Scene职责。
- 不建立普通.NET Worker executable、Fantasy Console Client runtime、Worker到Gate的自定义IPC或原始Socket协议栈。
- 不复制`Ref/94`的客户端position权威、100ms移动段、服务端MoveComponent或Transform gameplay真值。
- 不建立DotRecast专属Network Model、History、Correction、Checkpoint、Presentation或gameplay packet链。

## Current Spec Comparison

- current `server-authoritative-hybrid-sync-model`把Fantasy进程整体写成只负责Room，并把Authority具体写成外部Unity Worker。本change将该进程级约束改为Scene级所有权：Gate Scene继续不执行Gameplay，独立Authority Scene可以在同一Fantasy Server进程内托管portable Authority Host。
- current `fantasy-unity-authoritative-session`只描述既有Unity CC四进程环境，继续保持不变；DotRecast三进程环境不修改或伪装该能力。
- current `openspec/project.md`仍写着DotRecast Authority Worker是普通.NET外部进程、Fantasy普通.NET进程不得执行Solver。本change实施时 MUST同步更新为Gate Scene与Authority Scene分离的口径。
- `add-shared-dotrecast-navigation-solver`拥有Solver、Artifact与Binding requirements；本change只引用其稳定identity，不重复定义。
- current `dotrecast-navigation-world-solver`只要求显式静态几何，未要求运行时可见地图与烘焙几何同源。本change补充canonical地图Prefab合同，消除纯平面导航Scene与客户端地图分裂。
- current `dotrecast-navigation-world-solver`还明确禁止actor collision，这与固定2v2vE roster的硬接触需求冲突。本change删除该旧要求，保留Recast查询只负责静态Surface的边界，并新增`DotRecastWorldSolver`在唯一`ResolveBatch`内组合portable Actor硬接触的要求。
- `refactor-server-authoritative-host-portability`拥有Source/Pipeline/Launch requirements；Fantasy Authority Scene是新的普通.NET Host外壳，继续调用同一launch request，不复制Host运行语义。

## Impact

- 新能力：`dotrecast-authoritative-server-backend`。
- 修改能力：`server-authoritative-hybrid-sync-model`。
- 新服务端所有权：Fantasy DotRecast Authority Scene、Gate到Authority Scene的正式Inner控制路由。
- 新交付：Authority Scene manifest、DotRecast Client Scene、Prediction Composition、隔离server/player build profiles和三进程启动脚本。
- 地图交付：客户端Scene与Navigation authoring共同引用的canonical测试地图Prefab，以及由该Prefab生成的唯一NavigationSurfaceArtifact。
- 世界求解交付：显式Actor接触形状、portable ActorContactSolver、静态Surface候选与同批Actor硬接触组成的唯一DotRecast WorldSolver链。
- 保留交付：Unity Authority Worker、Unity四进程环境和现有direct UDP gameplay数据面。
- 协议：Outer客户端协议和Inner Scene协议共同携带同一portable identity；不增加DotRecast专属Gameplay数据面。
- 删除：外部DotRecast Worker、Fantasy Console adapter、Worker发布目录、旧Worker manifest字段与分裂同步路径。
