## 1. 锁定迁移基线

- [x] 1.1 读取并核对Session Composition、Network Model、DotRecast与ServerAuthoritative current specs。
- [x] 1.2 确认三个依赖change已完成并通过strict validation，记录current spec版本与未归档状态。
- [x] 1.3 暂停`add-dotrecast-authoritative-server-backend`并记录其当前已完成任务与工作区状态。
- [x] 1.4 盘点全部Main Runtime/Editor asmdef、asmref、程序集引用和预定义程序集脚本。
- [x] 1.5 盘点`Simulation/Unity`对Character、Camera、Network Model、Fantasy和DotRecast的全部引用。
- [x] 1.6 盘点ServerAuthoritative Unity目录对公共Composition、Character Presentation、Fantasy和portable模型程序集的全部引用。
- [x] 1.7 盘点Main Runtime下全部嵌套Editor目录和Editor-only API。
- [x] 1.8 盘点受影响ScriptableObject、MonoBehaviour、`.asset`、`.unity`和`.prefab`的MonoScript GUID引用。
- [x] 1.9 搜索受影响类型的`SerializeReference`、assembly-qualified typename、反射和assembly name依赖。
- [x] 1.10 记录迁移前ProgramHash、PipelineHash、Composition identity、Model identity与脚本GUID清单。
- [x] 1.11 锁定不修改运行语义、协议、资产字段和namespace的删除清单。

## 2. 建立公共Unity Simulation程序集

- [x] 2.1 建立`ThirdPersonSimulation.Unity`asmdef及明确依赖列表。
- [x] 2.2 将model-neutral Program/Backend/Pipeline/Source/Solver/Composition Definition迁入该程序集。
- [x] 2.3 将Float32 Unity Runtime Definition、Backend Definition、package provider和request lowering迁入该程序集。
- [x] 2.4 将Local/Preview Source、Pipeline与Pass Definition迁入该程序集。
- [x] 2.5 将通用Actor registration合同迁入该程序集。
- [x] 2.6 将output aggregate迁入Simulation程序集并改为model-neutral命名。
- [x] 2.7 将diagnostics aggregate迁入Simulation程序集并改为model-neutral命名。
- [x] 2.8 删除公共Composer对Character Host aggregate实现的引用。
- [x] 2.9 为Session Host提供唯一窄preparation创建入口。
- [x] 2.10 保持Composer、Prepared Source、build request和resource ownership内部合同不公开。
- [x] 2.11 保持Unity CharacterController binding、Solver Definition与Solver adapter由该程序集唯一拥有。
- [x] 2.12 确认公共程序集不引用Character Presentation、Camera、Animancer、Network Model、Fantasy或DotRecast Unity实现。

## 3. 分离DotRecast Unity Solver adapter

- [x] 3.1 建立`ThirdPersonSimulation.DotRecast.Unity`asmdef。
- [x] 3.2 迁移`NavigationSurfaceAsset`及canonical artifact wrapper。
- [x] 3.3 迁移DotRecast state-only body binding。
- [x] 3.4 迁移DotRecast WorldSolver Definition。
- [x] 3.5 保持portable DotRecast Solver与artifact codec继续由`ThirdPersonSimulation.DotRecast`唯一拥有。
- [x] 3.6 删除公共Unity程序集对DotRecast具体程序集的引用。
- [x] 3.7 核对Unity CharacterController组合不加载或引用DotRecast Unity程序集类型。

## 4. 建立model-neutral Network Model Unity程序集

- [x] 4.1 建立`ThirdPersonGameplay.NetworkModel.Unity`asmdef。
- [x] 4.2 迁移`GameplayNetworkModelDefinition`与Session Source Definition基类。
- [x] 4.3 迁移Source requirements、preparation context与通用validation。
- [x] 4.4 将Simulation内部准备合同通过最小正式接口提供给Network Model程序集。
- [x] 4.5 保持通用validation不识别ServerAuthoritative、Rollback或其它具体Model。
- [x] 4.6 确认该程序集不引用Fantasy、Character Presentation或具体Solver实现。

## 5. 建立ServerAuthoritative Unity模型程序集

- [x] 5.1 建立`ThirdPersonSimulation.ServerAuthoritative.Unity`asmdef及明确依赖列表。
- [x] 5.2 迁移`ServerAuthoritativeHybridModelDefinition`。
- [x] 5.3 迁移Prediction/Authority Pipeline Definition和全部模型Pass Definition。
- [x] 5.4 迁移Endpoint contract、Fantasy connection/handler adapter与Launch Definition。
- [x] 5.5 迁移Prediction/Authority Source Definition、preparation与prepared source。
- [x] 5.6 迁移Authority Actor Host/registration和Client Scene binding。
- [x] 5.7 迁移remote presentation site、registration与frame target。
- [x] 5.8 迁移ServerAuthoritative测试Bootstrap。
- [x] 5.9 保持模型算法、history、correction、Authority queue与codec继续由portable模型程序集唯一拥有。
- [x] 5.10 确认模型程序集可引用客户端Host合同，但`ThirdPersonClient.Runtime`不反向引用模型程序集。
- [x] 5.11 删除`Simulation/Unity`中全部ServerAuthoritative具体Definition旧文件路径。
- [x] 5.12 删除预定义程序集中的ServerAuthoritative脚本副本和旧类型所有权。

## 6. 建立客户端Runtime与Editor程序集

- [x] 6.1 建立`ThirdPersonClient.Runtime`asmdef并显式引用现有BTSMTL、Gameplay、Simulation与Presentation依赖。
- [x] 6.2 将Character专属Program/Projection wrapper与Unity Input adapter迁入客户端Runtime所有权。
- [x] 6.3 保持Character/Camera/Animation/Presentation/Session Host进入客户端Runtime程序集。
- [x] 6.4 确认客户端Runtime只通过公共Composition入口创建Session，不访问内部Composer或Prepared Source。
- [x] 6.5 将Runtime目录下Action、Behavior与Pipeline Editor脚本迁出Player编译范围。
- [x] 6.6 建立`ThirdPersonClient.Editor`asmdef及显式Editor依赖。
- [x] 6.7 将Main Editor与迁出的嵌套Editor脚本收敛到唯一Editor程序集。
- [x] 6.8 确认Editor程序集不形成Runtime反向依赖。
- [x] 6.9 确认`Assembly-CSharp`与`Assembly-CSharp-Editor`不再承担正式Gameplay Runtime/Editor模块所有权。

## 7. 迁移资产并删除旧路径

- [x] 7.1 移动脚本时保留每个`.meta` GUID。
- [x] 7.2 核对全部Composition、Pipeline、Pass、Model、Endpoint、Launch、Solver与Scene脚本引用仍指向原GUID。
- [x] 7.3 核对不存在受影响的未迁移managed-reference assembly typename。
- [x] 7.4 删除旧脚本路径、重复类型、临时asmref和预定义程序集桥接。
- [x] 7.5 删除无用`using`、friend assembly、反射registry和字符串类型加载。
- [x] 7.6 搜索并确认`ThirdPersonSimulation.Unity`源码不出现ServerAuthoritative、Fantasy或DotRecast具体类型。
- [x] 7.7 搜索并确认model-neutral Network Model程序集不出现具体Model类型。
- [x] 7.8 核对迁移后Program/Pipeline/Composition/Model identity形成输入未变化。

## 8. 同步依赖文档

- [x] 8.1 更新`openspec/project.md`的Code Organization与程序集依赖图。
- [x] 8.2 更新`add-dotrecast-authoritative-server-backend`implementation inventory中的Unity程序集所有权。
- [x] 8.3 更新DotRecast change的Client Composition、Editor exporter与Server引用任务依赖说明。
- [x] 8.4 记录`refactor-server-authoritative-prediction-state-modules`为下一串行change。
- [x] 8.5 搜索并删除把公共Unity Composition描述为`Assembly-CSharp`实现的过时文档。

## 9. 编译与严格校验

- [x] 9.1 编译portable Core、Float32、ServerAuthoritative、Transport与DotRecast工程。
- [x] 9.2 编译新增Unity Runtime程序集及客户端Runtime程序集。
- [x] 9.3 编译新增ServerAuthoritative Unity与Editor程序集。
- [x] 9.4 编译`Assembly-CSharp.csproj`和`Assembly-CSharp-Editor.csproj`确认无残留所有权引用。
- [x] 9.5 所有dotnet build/msbuild命令带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 9.6 每轮编译后立即执行`dotnet build-server shutdown`。
- [x] 9.7 运行`openspec validate refactor-unity-simulation-assembly-ownership --strict --no-interactive`。
- [x] 9.8 运行`openspec validate --all --strict --no-interactive`并解决本change引入的冲突。
- [x] 9.9 核对全部task勾选与真实程序集、资产和删除状态一致。
