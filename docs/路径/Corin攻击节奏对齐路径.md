# Corin 攻击节奏对齐路径

更新日期：2026-10-01。原作字段与未闭合的规则见 `docs/参考/战斗/corin-attack-feel-comparison.md`。

## 正式调用链

1. `CorinCharacterInputProfile` 定义 Attack 请求及 AttackHeld 连续输入。
2. `CorinCharacterControlModule.SubmitAbilityRequests` 提交 Attack 能力；技能内部接段由 Attack FSM 处理。
3. `CorinAttackGameplayAbilityAuthoringCode.Execute` 是本次显式资产重建入口。`Root.cs` 在第 33、25、60 帧开启 1→2、2→3、4→5 的 ComboAccept TreeClip。
4. 同一入口的 `AnimationTransitions.cs` 调用现有 BlendPolicy 正式 Configure API，按精确动画生产者身份设置上述三条转移的混合时长。它只在显式作者生成时执行，运行时仍读取同一正式 `CorinActionBlendPolicy`。
5. `btsmtl.generate_assets` 保存 `CorinAttackGameplayAbilityDefinition.asset` 及本次登记的 BlendPolicy 写入；`GameplayAbilityExecutionDataAssetPublisher.PublishSelected` 可按 Definition 和 `Attack` 选择发布 Fixed/Float32 技能数据。
6. 技能运行时消费 ComboAccept 与 Attack 请求进行 FSM 接段，Timeline 提交动画进度；Action Slot 根据生产者身份消费 BlendPolicy。

## 本轮完成状态

- 三处接招门槛：源码修改、静态编译、正式生成、两套产物发布已完成。
- 三处混合时长：作者入口、静态编译、正式保存和重导入读取检查已完成，分别为 0.05、0.1、0.100000024 秒。
- 生成后已按资产基线修正旧作者入口的节点位置和曲线值；最终 Attack 资产仅有三个窗口起点的差异。
- Fixed/Float32 正式 Load 已通过，两套语义 hash 一致；详细静态证据见 `docs/测试/Corin攻击节奏静态核对20261001.md`。
- 源 Motion、长按动态收束、切入位置、真实命中与停帧：未完成，不能把本轮结果称为完整手感复刻。
- 不运行 replay、Unity batchmode，不新增测试；未做实机手感验收。

作者入口实际属于 `ThirdPersonCharacter.ContentDefinitions.Editor.csproj`，已核对 Compile 项包含入口与本次新增 partial。最终静态编译使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，0 错误、0 警告，随后立即成功执行 build-server shutdown。此前 `ThirdPersonClient.Editor.csproj` 的编译不包含该作者程序集，不能作为作者入口已编译的证据。
