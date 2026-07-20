# 实施基线

## 前置状态

- `refactor-float32-session-runtime-launcher-boundary`：110/110，strict validation通过，未归档。
- `refactor-server-authoritative-host-portability`：74/74，strict validation通过，未归档。
- `refactor-agent-authoring-compiler-modules`：124/124，strict validation通过，未归档。
- `add-dotrecast-authoritative-server-backend`：20/185，保持暂停，不修改其Authority Scene接线。
- current spec基线：`gameplay-simulation-session-composition` 10项、`gameplay-network-model-boundary` 8项、`character-simulation-kernel` 27项、`server-authoritative-prediction-correction-pipeline` 14项。

## 迁移前程序集与隐式所有权

- Main下存在19个asmdef、0个asmref。
- `Simulation/Unity`、`Networking/GameplayNetwork`、ServerAuthoritative Unity adapter、Character/Camera/Presentation Host与Main Editor仍由预定义程序集编译。
- 公共Composer通过Character目录中的output/diagnostics aggregate形成隐藏反向依赖。
- Runtime下存在Action Editor 1个脚本、Behavior Editor 1个脚本、Pipeline Editor 39个脚本；Main Editor另有CharacterSimulation 18个脚本。

## 序列化风险

- MonoBehaviour与ScriptableObject均使用`.meta` GUID的PPtr引用，脚本移动必须连同`.meta`移动。
- `CorinPlayableRootTree.asset`包含153个`asm: Assembly-CSharp` managed-reference记录，共11种Character节点类型；这些类型统一迁入`ThirdPersonClient.Runtime`，资产程序集名精确改写，不保留旧程序集类型。
- 未发现其它受影响`.asset`、`.prefab`或`.unity`中的`Assembly-CSharp` managed-reference typename。
- 未发现受影响模块使用`Type.GetType`、程序集扫描、字符串registry或`InternalsVisibleTo("Assembly-CSharp")`。

## 身份基线

- Corin ProgramHash与Projection ProgramHash：`6842f5788b07d0d5c3146994a2c2395334c3d789a6af2b3eec5f688cf5cb031a`。
- Standard Local Pipeline：`thirdperson.simulation.pipeline.standard-local`，revision 1。
- Preview Pipeline：`thirdperson.simulation.pipeline.preview`，revision 1。
- Prediction Pipeline：`thirdperson.simulation.pipeline.server-authoritative-prediction`，revision 1。
- Authority Pipeline：`thirdperson.simulation.pipeline.server-authoritative-authority`，revision 1。
- Local Composition：`Corin.Sandbox.Local / Sandbox.World / Sandbox.LocalLogic`。
- Preview Composition：`Corin.TimelinePreview / Sandbox.PreviewWorld / Sandbox.TimelinePreview`。
- ServerAuthoritative Model、Protocol与Endpoint身份继续由现有canonical常量构造，不把程序集名加入identity hash。

## 关键脚本GUID

- Session Composition：`e381b64b19ae421aa92161f8c0cbd5d4`。
- Network Model基类：`f0586fa83dbb43bb9d01799fd5c2a16d`。
- ServerAuthoritative Model：`144c4189ea8045ada2970d6e21e0bb7d`。
- Prediction Pipeline：`a8e5fd2e7c9b6c340829fea3c237a306`。
- Authority Pipeline：`7a20000000004d259d15000000000020`。
- Fantasy Endpoint：`5df4f7f93ef9448fa06beadfe2543c06`。
- Launch Definition：`8b4b77cc0cef4d76af32e8394a689859`。
- Unity CharacterController Solver：`5d1207bfa0884154a99582ccd5586a08`。
- DotRecast Solver：`598c058a309ccf64c8a89fec8c42d867`。
- Character Host：`58a1c723a5984971b3b2b699c52893f6`。
- Session Host：`35bdda08535c410d9ead1434ae133964`。

## 删除清单

- 删除公共Composer对Character aggregate的反向引用。
- 删除公共Unity程序集中的DotRecast与ServerAuthoritative具体源码所有权。
- 删除Runtime下Character Editor编译路径。
- 删除Character managed reference对`Assembly-CSharp`的依赖。
- 不增加asmref、friend assembly、服务定位器、反射factory、fallback或重复ScriptableObject类型。

## 实施结果

- `ThirdPersonSimulation.Unity`只引用Core与Float32，拥有公共Composition、Local/Preview、Unity CharacterController Solver及通用output/diagnostics aggregate。
- `ThirdPersonSimulation.DotRecast.Unity`独立拥有Navigation Surface资产、DotRecast state binding与Solver Definition。
- `ThirdPersonGameplay.NetworkModel.Unity`只拥有model-neutral Network Model authoring；`ThirdPersonSimulation.ServerAuthoritative.Unity`拥有具体模型Unity接线。
- `ThirdPersonClient.Runtime`拥有Character/Camera/Presentation Host；`ThirdPersonClient.Editor`拥有Main下正式Editor代码。
- Fantasy生成协议由`ThirdPersonGameplay.FantasyProtocol`唯一编译，ServerAuthoritative Unity程序集显式引用，不再依靠预定义程序集。
- 受影响脚本全部连同原`.meta`移动，关键GUID未改变；Corin RootTree的153条managed-reference typename已统一改为`ThirdPersonClient.Runtime`，资产中不再存在`asm: Assembly-CSharp`。
- ProgramHash、四份Pipeline identity、Composition字段与Model canonical identity输入未改变。
- Unity正式脚本编译和`3C_Client.sln`编译均通过；解决方案仅保留BBB参考目录的1个既有warning，0 error。
