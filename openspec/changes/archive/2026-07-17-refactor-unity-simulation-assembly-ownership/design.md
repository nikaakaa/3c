## Context

当前可移植程序集依赖已经基本正确：

```text
ThirdPersonSimulation.Core
  -> ThirdPersonSimulation.Float32
       -> ThirdPersonSimulation.ServerAuthoritative
            -> ThirdPersonSimulation.ServerAuthoritative.Transport

ThirdPersonSimulation.Core
  -> ThirdPersonSimulation.DotRecast
```

问题集中在Unity侧。`Simulation/Unity`中的公共Composer引用Character目录内的output/diagnostics aggregate，`Character/Pipeline/Unity/SimulationSessionHost`又引用Composition Definition。由于两边都在`Assembly-CSharp`，编译器无法暴露这个反向依赖。ServerAuthoritative Pipeline Definition也与公共Unity Composition放在同一目录，model-neutral `GameplayNetworkModelDefinition`与具体Fantasy Endpoint同样共享预定义程序集。

## Goals

- 用asmdef依赖图强制公共Unity Simulation不认识具体Network Model。
- 让可选DotRecast Unity Solver与公共Unity Composition物理分离。
- 让model-neutral Network Model Definition与ServerAuthoritative Unity实现物理分离。
- 保留唯一Session/Composer/Launcher/Source/Pipeline/Presentation运行链，不改变资产与行为身份。
- 让Editor程序集显式依赖Runtime程序集，不再依靠特殊目录和`Assembly-CSharp-Editor`隐式可见性。

## Non-Goals

- 不把每个Character子领域拆成独立asmdef。
- 不改变namespace来模拟模块所有权。
- 不通过公开大量内部类型解决程序集可见性；只允许暴露Host真正需要的最小入口。
- 不让ServerAuthoritative Unity程序集拥有portable Prediction/Authority算法。

## Target Assembly Graph

```text
ThirdPersonSimulation.Core
  -> ThirdPersonSimulation.Float32
       -> ThirdPersonSimulation.Unity
            -> ThirdPersonClient.Runtime
            -> ThirdPersonGameplay.NetworkModel.Unity
                 -> ThirdPersonSimulation.ServerAuthoritative.Unity

ThirdPersonSimulation.DotRecast
  -> ThirdPersonSimulation.DotRecast.Unity

ThirdPersonSimulation.ServerAuthoritative
ThirdPersonSimulation.ServerAuthoritative.Transport
ThirdPersonGameplay.FantasyClient
ThirdPersonClient.Runtime
ThirdPersonGameplay.NetworkModel.Unity
  -> ThirdPersonSimulation.ServerAuthoritative.Unity

Runtime assemblies
  -> ThirdPersonClient.Editor
```

箭头表示被依赖项指向依赖方。`ThirdPersonSimulation.Unity`不得引用`ThirdPersonClient.Runtime`、NetworkModel、ServerAuthoritative、Fantasy或DotRecast Unity实现；`ThirdPersonGameplay.NetworkModel.Unity`不得引用任何具体Model；`ThirdPersonClient.Runtime`不得引用ServerAuthoritative Unity程序集。模型Scene/Source/remote adapter全部进入模型自己的Unity程序集。

## Decision 1: 先消除公共Composer对Character实现的反向依赖

`CharacterSimulationOutputAggregate`与`CharacterSimulationDiagnosticsAggregate`只消费`IFloat32SimulationActorRegistration`和portable output/diagnostics合同，不依赖Animancer、Camera或Character authoring。它们迁入`ThirdPersonSimulation.Unity`并改为Simulation所有权命名。公共Composer只构造这两个通用roster aggregate。

`SimulationSessionHost`继续通过`SimulationSessionCompositionDefinition`唯一创建preparation。Composition只暴露一个Host需要的正式创建入口，内部Composer、Prepared Source和build request仍保持程序集内部，不通过friend到`Assembly-CSharp`开放。

### Tradeoff

- 通过`InternalsVisibleTo("Assembly-CSharp")`改动更少，但仍把公共接口绑定预定义程序集，并允许任意客户端脚本访问内部Composition。
- 将aggregate迁入其真实所有者并暴露一个窄Host入口，接口更小且依赖可由编译器验证，因此选择该方案。

## Decision 2: 公共Unity Simulation与具体Solver adapter分层

`ThirdPersonSimulation.Unity`保存：

- Program/Backend/Pipeline/Source/Solver/Composition Definition基类。
- Float32 Runtime Definition、Backend Definition、runtime package provider与Unity request lowering。
- Local/Preview Source及标准Pipeline/Pass authoring。
- model-neutral Actor registration合同与roster output/diagnostics aggregate。
- Unity CharacterController binding、Solver Definition与Solver adapter。

DotRecast相关`NavigationSurfaceAsset`、state binding和Solver Definition进入`ThirdPersonSimulation.DotRecast.Unity`。公共Unity程序集只认识`Float32WorldSolverDefinition`，不因安装一个可选Solver而增加DotRecast依赖。

Character专属Projection wrapper与Unity Input adapter进入`ThirdPersonClient.Runtime`，因为它们分别依赖Animation Profile、Camera和Character Input authoring，不属于公共Simulation Composition。

## Decision 3: Network Model Definition与具体模型分程序集

`ThirdPersonGameplay.NetworkModel.Unity`只包含：

- `GameplayNetworkModelDefinition`。
- `GameplayNetworkModelSessionSourceDefinition`。
- Source requirements、preparation context与通用validation。

`ThirdPersonSimulation.ServerAuthoritative.Unity`包含：

- `ServerAuthoritativeHybridModelDefinition`。
- Prediction/Authority Pipeline及Pass Definition。
- Fantasy Endpoint/connection/handler adapter与Launch Definition。
- Prediction/Authority Source preparation。
- Authority Actor/Client Scene/remote presentation adapter与测试Bootstrap。

具体模型程序集可引用`ThirdPersonClient.Runtime`以连接Character Presentation，但反向引用被禁止。新增模型时只能新增自己的Unity adapter程序集，不能修改`ThirdPersonSimulation.Unity`或`ThirdPersonGameplay.NetworkModel.Unity`。

## Decision 4: 客户端Host与Editor形成显式程序集

剩余Camera、Character authoring/runtime、Presentation与Scene Host进入`ThirdPersonClient.Runtime`。嵌套在Runtime下的Action、Behavior与Pipeline Editor脚本迁入统一Editor目录并进入`ThirdPersonClient.Editor`，避免Editor API进入Player。

现有BTSMTL、Gameplay、FantasyClient、RootMotion和portable Simulation asmdef保持独立，不复制代码。Editor程序集显式引用所需Runtime与BTSMTL Editor程序集，不使用asmref把同一源码同时编入两个程序集。

## Decision 5: 序列化迁移不保留兼容类型

脚本移动必须连同`.meta`文件完成，保持MonoScript GUID。实施前必须搜索：

- 所有受影响ScriptableObject、MonoBehaviour与Editor类型的资产引用。
- `[SerializeReference]`字段及YAML中的assembly-qualified typename。
- 自定义反射、类型名、assembly name或`Type.GetType`依赖。

如果受影响runtime类型存在无法安全重写的managed-reference序列化，实施必须停止并说明缺口；不得保留旧assembly空壳、`MovedFrom`兼容链、重复类型或runtime migrator。普通PPtr脚本引用按原GUID继续指向唯一类型。

## Decision 6: 串行执行并冻结DotRecast接线

本change会移动`ServerAuthoritativeHybridModelDefinition`、Pipeline Definition、Endpoint与Editor exporter依赖，因此与`add-dotrecast-authoritative-server-backend`存在真实文件冲突。DotRecast change在本change与Prediction State模块化完成前保持暂停。完成后更新其implementation inventory与程序集引用，再从Authority Scene Manifest任务继续。

## Failure And Deletion Rules

- 任一依赖环：停止迁移并修正所有权，不合并程序集绕过。
- 公共Simulation引用具体Model/Fantasy/DotRecast：构建失败，不能以define或反射隐藏。
- Editor类型进入Player程序集：构建失败。
- 同一脚本存在两个编译所有者、旧文件副本或旧asmref：迁移失败并删除旧路径。
- 资产无法保持唯一脚本身份：停止并报告，不创建兼容类型。
