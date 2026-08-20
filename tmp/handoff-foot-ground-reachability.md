# Foot Ground Path Reachability 交接摘要

## 当前目标

用户已要求开始实施 `openspec/changes/add-character-foot-ground-path-detection/` 的下一段：

```text
Raw Contacts -> Edge -> Reachability -> Invalid Segment / Convex Hull -> Accepted Ground Envelope
```

这轮只完成 Ground Path Reachability。不要提前实施 `openspec/changes/add-ground-envelope-swing-foot-motion/`（仍为 `0/20`），不要接 Pelvis、Foot Goal 权重、FinalIK 求解、Foot Lock、第二 Grounding、fallback 或兼容路径。

## 必读入口

- `openspec/AGENTS.md`
- `openspec/project.md`
- `openspec/changes/add-character-foot-ground-path-detection/proposal.md`
- `openspec/changes/add-character-foot-ground-path-detection/design.md`
- `openspec/changes/add-character-foot-ground-path-detection/tasks.md`
- `openspec/changes/add-character-foot-ground-path-detection/specs/character-foot-placement-presentation/spec.md`
- `docs/foot-placement参考参考/gdc2016-fitting-the-world.pdf`
- `docs/foot-placement参考参考/1.md`
- `docs/foot-placement参考参考/2.md`
- `docs/foot-placement参考参考/predict-foot-ik-implementation-summary.md`

文档必须用 `Get-Content -Encoding UTF8` 读取；禁止 Unity batchmode；项目默认不新增测试，用户自己做端到端验证。

## 参考结论

GDC 原图页 33-36 已视觉检查：

- 页 33：沿脚步路径 Capsule Cast，保留位置和法线。
- 页 34：Ground Path 按 Near/Far、Bottom/Top 排序，验证法线，定义相邻地面 Edge Plane。
- 页 35：Reachability 检查全部 Edge 垂直距离并标记大变化。
- 页 36：通过 Reachability 后才做上侧 Convex Hull，得到连续、feet-only Ground Envelope。

Edge 应理解为排序后 Ground Profile 的相邻段。普通坡面因 Capsule 轴分段会得到多个小高差；台阶竖直变化会形成大的相邻 Bottom/Top 高差。不要只检测“同一纵向距离的一对点”，否则普通台阶可能检测不到。

Corin 参考支持：预测落点只给 Ground Path 两端，Ground Path 是脚步地面下界；失败时不能生成替代直线。当前阶段只发布地面事实，不修改 Pose。

## 当前代码状态

关键文件：

- `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootGroundPath.cs`
- `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootGroundEnvelope.cs`
- `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootPlacementProfile.cs`
- `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootPlacementRuntime.cs`
- `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/CharacterFootGroundPathGizmo.cs`
- `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/CharacterFootLandingPredictionSampler.cs`
- Corin profile：`3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/CorinFootPlacementProfile.asset`

已完成的上游：

- `CharacterFootPlacementRuntime` 统一拥有每脚 LastLanding / NextSwingLanding；Ground Path 不重复保存落点。
- 两落点构造唯一 Capsule 请求。
- Unity Adapter 按最大轴段长度切分真实 Capsule Cast；每段命中缓冲是 `SegmentHitCapacity`，整条 Raw Contact 页是 `ContactCapacity`。
- 纯 `CharacterFootGroundEnvelopeBuilder` 已有二维投影、稳定排序、法线平面交点、同距离最高候选、上侧 Convex Hull 和连续 Envelope。
- 左右脚已有 Committed/Pending 双页和 `Seal/Discard/Reset/Dispose`。
- 诊断已有双落点、Raw Contacts、Envelope、CSV/Gizmo。

仍未完成的 tasks：4.1-4.7、5.5、6.6-6.8、7.5-7.7。

## 本会话已发生的部分编辑

本会话只有 `CharacterFootGroundPath.cs` 的一小部分成功写入：

- `CharacterFootGroundPathRejectReason` 已追加 `UnreachableEdge = 9` 和 `EdgeCapacityExceeded = 10`。
- `CharacterFootGroundPathInputKey` 的构造参数、字段、Equals/GetHashCode 中大部分命名已从 `Current/Next` 改为 `Last/NextSwing`。

这次改名尚未收口，当前文件有编译错误风险：

- `CharacterFootGroundPathInput` 仍在使用 `CurrentLanding`、`NextLanding` 等旧属性。
- `ComputeIdentity` 仍引用 `key.CurrentLandingEventIdentity` 等旧字段。
- `CharacterFootGroundPathDiagnostics` 仍发布 `CurrentLanding*` 属性。

后续必须统一为 `LastLanding/NextSwingLanding`，不要留下两套语义名。CSV 已使用 `LastLanding` 和 `NextSwingLanding` 头。

## 当前写入阻塞

对目标 C# 文件连续使用 `apply_patch` 都返回 `Failed to write file`。已检查：目标文件可写、不是只读；Codex 的 `thread-writer-locks` 文件是当前会话正常持有，不是额外残留锁；Unity、VS Code 和 Codex 进程都在运行。对 `tmp` 和其它代码文件的 `apply_patch` 可以成功，所以是目标 C# 文件的文件级写回问题。下一会话先重新尝试小补丁；不要用 `Set-Content`、Python 或破坏性 git 命令绕过规则。若仍失败，先确认 Unity/语言服务是否暂时持有该文件，再向用户说明需要关闭编辑器后重试。

## 建议实现

### Profile

在 `CharacterFootGroundDetectionAuthoringSettings` 增加正式米制字段 `MaximumReachableVerticalEdge`。建议 Corin 初始值 `0.30m`，共享环境已有 Low 0.14m、High 0.24m、OverLimit 0.40m 对照。该值是 Foot Placement 独立配置，不读取 KCC Step 高度、CastAbove/CastBelow 或腿长；加入 Profile JSON/hash，自动进入 Profile Revision 和 Query Identity。Corin asset 显式写 `m_MaximumReachableVerticalEdge: 0.3`；不要修改 TrainingEnemy asset。Profile schema 从 `v18-ground-path-capacities` 升到下一明确版本。

### Edge 页

在现有每脚 `CharacterFootGroundPathPage` 中新增预分配 Edge 页，不新增 owner。保存有序 Edge 的 Bottom、Top、VerticalDistance、稳定 identity/索引，随同一 Committed/Pending 页清空、Seal、Discard。公开诊断只增加 EdgeCount、FirstInvalidSegmentIndex、首个无效段 Bottom/Top/VerticalDistance、MaximumReachableVerticalEdge。不要把整页 Edge 按值塞进已有大型 `CharacterFootLandingPredictionDiagnostics`，避免之前的大 Struct Mono `InvalidProgramException` 风险。

### Builder

修改 `CharacterFootGroundEnvelopeBuilder.TryBuild`，增加 Edge 页和 Reachability 限值：

1. 清空 workspace、output、edges。
2. 现有投影、排序、Surface Profile 建立后，在 `TryCollapseDistances` 之前逐个检查 Profile 相邻段。
3. 每段把候选转换为世界 Bottom/Top，计算 `abs(dot(Top - Bottom, ComponentUp))`。
4. 全部 Edge 写入预分配页；不要遇到第一个超限就返回，只记首个 Invalid Segment。
5. 任一超限返回 `UnreachableEdge`，不执行 Collapse、不执行 Hull、不发布 Envelope；Raw Contacts 与 Edge 页保留。
6. Edge 容量溢出用明确 typed rejection，不沿用旧 Envelope。
7. 全部通过后继续现有 Collapse + Upper Hull。

建议用纯数据 `CharacterFootGroundInvalidSegment` 返回首个无效段，Builder 不引用 Unity Physics/Editor/Gizmo。查询已执行时 `SetRejected` 必须保留 Contacts/Edges；未执行查询才清空。

### Runtime / Diagnostics / Gizmo / CSV

- `PrepareGroundPath` 从 `m_Settings.GroundDetection.MaximumReachableVerticalEdge` 传入 Builder。
- Builder 失败时把 reject reason、首个 invalid segment、Edge 页和 Raw Contacts 一起写入 Pending Page；Seal 后从同一只读页发布。
- Accepted 继续画最近一次成功 Seal 的 Ground Envelope。
- `UnreachableEdge` 只额外画首个 Invalid Segment Bottom -> Top 红色细线，不画伪 Envelope。
- CSV 增加 EdgeCount、FirstInvalidSegmentIndex、Bottom/Top、VerticalDistance、MaximumReachableVerticalEdge、最终 Ground Path 状态。
- Pelvis/左右 Goal 权重保持 0。

## 收尾校验

完成后才勾 `tasks.md` 对应项：

```powershell
dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false
dotnet build-server shutdown
openspec validate add-character-foot-ground-path-detection --strict --no-interactive
git diff --check -- <本轮涉及文件>
```

不跑 Unity batchmode，不新增自动测试，不把手动验证写入 tasks。建议单独中文 git commit，说明“Ground Path Reachability 与 Invalid Segment 接入”，不要 `git add .`。

## 用户验证

Reachability 编译和文档校验通过后，先让用户测：平地、Low/High 楼梯 Edge 全部在 0.30m 内且有 Envelope；OverLimit 0.40m 或墙体稳定 `UnreachableEdge`，只显示红色首个 Invalid Segment；同一 Landing Event 多帧不重复查询、不在 Accepted/Rejected 间抖动；CSV/Gizmo 与同一 Seal 页一致。用户验证通过后才进入 `add-ground-envelope-swing-foot-motion`。
