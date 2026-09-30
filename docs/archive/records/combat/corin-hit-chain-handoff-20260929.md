# 命中链分工交接（2026-09-29）

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 当前分工

用户最新指令：命中链已分给其他窗口，本窗口只做相机配置。下列命中数据改动保留在共享工作区供负责窗口接续；本窗口停止修改碰撞、命中运行时及攻击窗口。

## 已修改但未提交的文件

前缀：`3cDemo/Client/3C_Client/Assets/GameScripts/Main/`。

- `Editor/CharacterPipeline/Authoring/Ability/CorinAttackPropertyImportWorkflow.cs`：FanWithHeight 从 dump 的大写 `Height` 读取高度；导入 CameraShake 的资源 ID 与 ShakeOnNotHit；只保存本次 Effect 和 Profile，不调用全局 SaveAssets/Refresh。最新源码对 null/空 shakeConfigKey 不创建震动组件。
- `Runtime/Gameplay/Effects/GameplayEffectDefinition.cs`：新增 `GameplayAttackCameraShakeComponentDefinition`，只保存 ResourceId、ShakeOnNotHit。
- `Runtime/Character/Pipeline/Runtime/CharacterGameplayEffectRuntimeBinding.cs`：编码上述组件。
- `Runtime/Simulation/Core/Fixed/Execution/FixedGameplayEffectRuntimeCatalog.cs` 与 Float32 对应文件：解码上述组件。
- `Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/`：正式 Import 已保存 108 份配置；其中 103 份属于 Git 已跟踪资产。5 份本机资产没有强制加入 Git。

## 检查与待收口

第一次正式导入后按磁盘文本与原 JSON 比较：108 份配置、50 份 Fan 高度、104 份非空震动引用及 ShakeOnNotHit 均一致。对 103 份已跟踪资产，忽略 managed reference ID 重排后，除高度及新增震动组件外没有其他业务参数差异。

最后一项空资源处理源码尚未再次 Import：磁盘四份空资源配置仍含空震动组件。它们是 ParryAid_Auto、ParryAid_H、ParryAid_L、BloodDebuff。负责命中链的窗口可在完成自身修改后统一调用正式 `CorinAttackPropertyImportWorkflow.Import()`，不要直接改 YAML。

字段类型、编码和解码曾完成 Unity 编译，Console 0 错误；随后其他工作区图程序集改动引发 Graph 命名空间缺失、域重载，未完成本批最新源码及完整技能产物的最终发布。没有 Play、replay 或新增测试。

## 实际执行缺口

- 当前 Attack / RushAttack / BranchAttack 把攻击 Effect 登记进 Ability 目录，但当前生成作者源码没有执行它们的 ApplyGameplayEffect 节点。须按 dump 事件帧补正式 TreeClip 内调用。
- `PortableAttackCollisionComponent`、`PortableAttackPropertyComponent` 当前仅有数据声明、编码/解码和准备期检查；没有碰撞求解消费者。
- Fixed/Float32 `GameplayEffectMapping.DescribeComponent` 不支持这两个攻击组件。不能把普通 Effect 对自身应用成功当成命中确认，也不能添加空执行来压住错误。
- 当前正式链为 Evaluate → WorldResolveBatch → Finalize → 原子 Commit。命中请求、跨 Actor 求解、计数/间隔、结果身份及事实输出应进入这条链；相机只消费已提交结果。
- 目标受击体、阵营、连续攻击实例的结束/中断与快照语义仍需实现。没有创建临时 Physics 查询、伤害结算或目标装配。

## 相机端合同

资源键直接使用 dump 的 `CameraShake.shakeConfigKey`。当前三个技能所需 A 类键为：Normal_01 A_01、Normal_02 A_01、Normal_03 A_01、Normal_04 A_01/A_02、Normal_05 A_01、Rush A_01、Branch_02 A_01/A_02。

本窗口继续补齐这些 CameraShakeAsset 及 Profile 登记。`ShakeOnNotHit=false` 由命中链保留；不得转换为动作时间点无条件触发。原命中强度允许实体变量 ShakeStrength 覆盖默认 1，证据见 `corin-camera-shake-source-parity-20260928.md`。
