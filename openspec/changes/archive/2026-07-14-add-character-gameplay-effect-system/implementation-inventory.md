# 实施基线清单

## 前置基线

- `refactor-gameplay-network-model-boundary` 已归档为 `2026-07-14-refactor-gameplay-network-model-boundary`。
- Character 与 Network Model 的正式边界是模型无关 semantic fact；`ServerAuthoritativeHybrid` 只在 Character 外部 Adapter/Profile 中映射 packet 和 policy。
- `Runtime/Gameplay` 当前只包含统一 `GameplayTickSystem`，没有 Gameplay Effect、Attribute 或 Tag runtime。

## CharacterPipeline Logic Tick

当前顺序：

```text
ActionRuntime.BeginLogicTick
CharacterPipelineFrame.Begin
CharacterGraphContext.BeginFrame
CharacterNetworkReceiveStage.Collect
CharacterActionLifecycleInputStage.Resolve
CharacterInputStage.Update
CharacterBTSMTLPhase.Tick
CharacterMotionStage.Update
CharacterNetworkSendStage.Collect
CharacterPresentationStage.CaptureLogicSample
CharacterCameraStage.CaptureLogicSample
Diagnostics
```

目标顺序：

```text
ActionRuntime.BeginLogicTick
CharacterPipelineFrame.Begin
CharacterGraphContext.BeginFrame
CharacterNetworkReceiveStage.Collect
CharacterActionLifecycleInputStage.Resolve
CharacterGameplayEffectAdapter.BeginLogicTick
CharacterInputStage.Update
CharacterBTSMTLPhase.Tick
CharacterMotionStage.Update
CharacterGameplayEffectAdapter.CommitFacts
CharacterNetworkSendStage.Collect
CharacterPresentationStage.CaptureLogicSample
CharacterCameraStage.CaptureLogicSample
Diagnostics
```

共享输入是 `ActionLifecycleTransition`、`IncomingGameplayResult`、`GameplayEffectLifecycleFact`、`GameplayAttributeValueFact` 和固定逻辑 Tick；共享输出是 `GameplayEffectChangeSet` 投影出的 Effect/Attribute facts、`GameplayCueFact` 与 diagnostics trace。

## Gameplay Effect 与 ServerAuthoritative 映射

```text
CharacterNetworkReceiveStage
  -> GameplayEffectSyncDomainInput
  -> CharacterGameplayEffectInputMapper
  -> GameplayEffectAuthorityInput
  -> GameplayEffectRuntime
  -> GameplayEffectChangeSet
  -> CharacterGameplayEffectFactProjector
  -> GameplayEffectSyncDomainOutput
  -> CharacterNetworkSendStage
  -> CharacterServerAuthoritativeAdapter
  -> ServerAuthoritativeGameplayEffect / ServerAuthoritativeGameplayAttributeValue
```

Effect policy 只由 `ServerAuthoritativeCharacterSyncProfile` 按 Effect BehaviorId 解析。Attribute fact 使用正式 fact binding；GameplayEffectRuntime、Character Adapter 和 Network stages 不解析模型 policy。

## 当前 authoring 字段

- `ActionProfile`：ActionId、DisplayName、DebugCategory、字符串 Tags、字符串 BlockTags、字符串 CancelTags、TargetPolicy。
- `GameplayBehaviorProfile`：BehaviorId、BehaviorKind、DisplayName、DebugCategory、字符串 Tags。
- `CharacterPipelineDefinition`：RootTreeAsset、InputProfile、AnimationPresentation、ActionProfiles、BehaviorProfiles。

目标迁移：Action/Behavior 的字符串 Tag 改为 Catalog `GameplayTagId`；Definition 新增唯一 `CharacterGameplayEffectProfile`，统一 registry 同时覆盖 Action、generic Behavior 与 EffectDefinition。

## 旧 Tag 调用清单

- `Character/Action/ActionRuntime.cs`：私有 `m_Tags`、`SetTag`、`HasTag`、BlockTags 校验、Reset 清理。
- `Character/Action/ActionProfile.cs`：字符串 Tags/BlockTags/CancelTags、Contains 与字符串校验。
- `Character/Action/Editor/ActionProfileEditor.cs`：三组字符串 Tag 序列化 UI。
- `Character/Behavior/GameplayBehaviorProfile.cs`：字符串 Tags 与校验。
- `Character/Behavior/Editor/GameplayBehaviorProfileEditor.cs`：字符串 Tag 序列化 UI。
- `ActionRuntime.CanCancelActiveAction`：`nextProfile.CancelTags` 对 `m_ActiveProfile.Tags` 的事务判断。

## 旧 StateEffect 占位清单

- 定义：`CharacterNetworkSemanticFacts.cs` 的 `GameplayStateEffectFact`。
- 输入缓存：`CharacterNetworkInput.cs`、`CharacterNetworkReceiveStage.cs`。
- 输出缓存：`CharacterPipelineOutput.cs`、`CharacterNetworkSendStage.cs`。
- 模型映射：`CharacterServerAuthoritativeAdapter.cs`。
- 模型 payload/packet：`ServerAuthoritativePayloads.cs`、`ServerAuthoritativePacket.cs`。
- 模型 policy：`ServerAuthoritativePolicyResolvers.cs`、`ServerAuthoritativeCharacterSyncProfile.cs`。

该占位没有 Gameplay producer、Attribute store 或 Effect lifecycle consumer，实施时一次性删除 StateId、PayloadDigest 和 State behavior kind。

## 旧 ActionCueEvent 清单

- 定义与 Action diagnostics：`ActionOutputContracts.cs`、`ActionRuntime.cs`、`CharacterPipeline.cs`。
- Graph/Timeline 生产：`ActionRuntimeNodes.cs`、`CharacterGraphContext.cs`、`TimelinePlaybackScheduler.cs`。
- Input/Output/Send/Receive：`CharacterNetworkInput.cs`、`CharacterPipelineOutput.cs`、`CharacterNetworkReceiveStage.cs`、`CharacterNetworkSendStage.cs`。
- 模型映射：`CharacterServerAuthoritativeAdapter.cs`。
- Agent 校验：`AgentGraphValidator.cs`。

正式迁移目标是唯一 `GameplayCueFact`，不保留 Action cue 兼容类型。

## 旧 KaaKaaFramework 依赖结论

3C 工程的 Assets 与 Packages 中没有 `KaaKaaFrameWork`、`BuffHandler`、`PropertyHandler` 或旧 namespace 依赖。旧工程只作为算法参考；不会引入其 MonoBehaviour Update、Coroutine、Addressables 名称加载、`params object[]`、字符串属性依赖或旧类型名。
