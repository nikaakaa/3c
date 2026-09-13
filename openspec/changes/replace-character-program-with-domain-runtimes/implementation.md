# 实施记录

## 当前状态

- change：`replace-character-program-with-domain-runtimes`
- 已完成代码小步：3
- OpenSpec 任务：仍以 `tasks.md` 为准，1.3 及后续任务未完成。
- Unity Console、PlayMode 和运行时行为：尚未验证。

## 已提交的小步

- `24e63cf3f`：新增 `SimulationProgramRootKind.Ability`，并登记 `ability:` 入口身份。
- `25155d0ed`：新增 Ability 私有图发现模型、黑板声明收集、子图关系检查和资产依赖 source revision。
- `bcab5f853`：新增独立 Ability Semantic Frontend，按 Ability 根编译图操作、调用帧、黑板状态、常量、来源、能力要求和 Ability catalog。

## 当前实现边界

- Ability 前端不读取 CharacterPipelineDefinition，不生成 Character 控制、Body Motion、Equipment 或 Pose 目录。
- Ability 图通过现有 BTSMTL Skill 图编译器复用图算法；外部 Input、Gameplay Effect、Character State 只通过 provider owner 和最小 catalog 依赖接入。
- Ability 根入口直接指向私有图的 Root operation；当前仍使用统一 Semantic IR 数据结构，独立 target artifact、运行时 Ability 数据接口和旧 Character Program 清理尚未完成。

## 编译证据与阻断

- 已按项目要求运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore`。
- 当前失败原因是其他窗口迁移造成的 `Assets/GameScripts/Main/Runtime/Character/Action/ActionProfile.cs` 缺失；编译器在进入本批 Ability 源文件前已停止。
- 编译结束后已执行 `dotnet build-server shutdown`，未运行 Unity、测试或资产生成。

## 下一小步

接入 Ability 独立的 Float32／Fixed target lowering、唯一 codec 和按 Ability identity 保存的 artifact store；完成后再更新任务勾选和提交记录。
