# Tasks

## 1. 基线与冲突收口

- [x] 1.1 记录当前 Float32 Program、Fixed Program、Projection 的 ProgramId、SourceRevision、SemanticHash、ProgramHash、LayoutHash、NumericProfile、ABI 与 ProjectionRevision字段来源。
- [x] 1.2 枚举 `CharacterPresentationProjection` 内 Runtime payload、Editor编译、Authoring join、Target Program解码、identity、stale 与 validation职责。
- [x] 1.3 枚举 `RequireProgram`、`RequireSemanticProgram`、两个Projection Asset Load overload与全部调用方。
- [x] 1.4 枚举 Float32、Local Fixed、Rollback与Remote Presentation创建 producer identity的全部路径。
- [x] 1.5 核对 `refactor-timeline-animation-authoring-boundary` 最终安装的Analysis artifact resolver、diagnostic与revision token Interface。
- [x] 1.6 核对 `add-local-fixed-gameplay-lab` 最终安装的Fixed Program Asset、Fixed Host与Local Fixed Product入口。
- [x] 1.7 核对 `refactor-motion-warp-trajectory-solving` 完成后的Program ABI、Projection producer与生成资产字段。
- [x] 1.8 更新实施清单，标出并行工作已经修改且本change不得覆盖的文件和字段。

## 2. Presentation Semantic Contract

- [x] 2.1 定义 `CharacterPresentationSemanticContract` schema version。
- [x] 2.2 定义 ProgramId字段及其非空规范。
- [x] 2.3 定义 Gameplay SourceRevision字段及其非空规范。
- [x] 2.4 定义 SemanticHash字段及其格式规范。
- [x] 2.5 定义 producer contract entry的Index字段。
- [x] 2.6 定义 producer contract entry的Identity字段。
- [x] 2.7 定义 producer contract entry的LayerId字段。
- [x] 2.8 定义 producer contract entry的SourceIdentity字段。
- [x] 2.9 定义 producer contract entry的ChannelKind字段。
- [x] 2.10 实现producer index连续性校验。
- [x] 2.11 实现producer identity唯一性校验。
- [x] 2.12 实现producer字段规范化和空值拒绝。
- [x] 2.13 实现唯一canonical ContractHash算法。
- [x] 2.14 让构造函数拒绝调用方提供或覆盖ContractHash。
- [x] 2.15 从validated Semantic IR artifact建立Frontend contract。

## 3. Projection Compiler Module

- [x] 3.1 新建Editor-only `CharacterPresentationProjectionCompiler` Module。
- [x] 3.2 定义完整的Projection compile request。
- [x] 3.3 定义Projection compile result和结构化diagnostics。
- [x] 3.4 把Layer复制和校验移入Projection Compiler。
- [x] 3.5 把Producer有序遍历移入Projection Compiler。
- [x] 3.6 把Producer Kind解析移入Projection Compiler。
- [x] 3.7 把Producer SourceMap解析移入Projection Compiler。
- [x] 3.8 把Animation Producer binding编译移入Projection Compiler。
- [x] 3.9 把Camera Producer binding编译移入Projection Compiler。
- [x] 3.10 把Cue Producer binding编译移入Projection Compiler。
- [x] 3.11 把Marker Sync call-site收集移入Projection Compiler Implementation。
- [x] 3.12 把Marker Sync authoring校验移入Projection Compiler Implementation。
- [x] 3.13 把Equipment Visual Projection编译从Runtime partial类移入Editor Implementation。
- [x] 3.14 接入正式Animation Analysis artifact resolver输出。
- [x] 3.15 保持Animation Analysis diagnostics的stable Timeline/Track/Clip identity。
- [x] 3.16 建立窄的internal Runtime Projection payload factory。
- [x] 3.17 让payload factory只接受完成排序和校验的数据。
- [x] 3.18 删除Runtime `CharacterPresentationProjection.Build`公共入口。
- [x] 3.19 删除Runtime Projection对Animation/Equipment authoring compile类型的依赖。
- [x] 3.20 删除Runtime Projection中的AssetDatabase/stale计算职责。

## 4. Numeric-Neutral Presentation Semantic Reader

- [x] 4.1 新建Editor-only `CharacterPresentationSemanticReader`。
- [x] 4.2 从Semantic IR producer table读取ordered producer。
- [x] 4.3 从Semantic IR source map读取唯一producer source。
- [x] 4.4 从Semantic IR reference table定位唯一producer source operation。
- [x] 4.5 复用`CameraProgramOperationSchema`校验Camera operation code与payload version。
- [x] 4.6 从`SemanticLiteral`读取Int32字段。
- [x] 4.7 从`SemanticLiteral`读取String字段。
- [x] 4.8 从`SemanticLiteral`读取numeric-neutral Number字段。
- [x] 4.9 在Presentation数值Seam显式把Number转换成Unity float。
- [x] 4.10 为数值转换错误保留source identity、字段名和原始literal。
- [x] 4.11 拒绝一个producer关联多个Camera source operation。
- [x] 4.12 拒绝缺少Camera source operation或literal的Graph producer。
- [x] 4.13 保持Timeline Camera Clip只从Timeline authoring inventory编译。
- [x] 4.14 删除Projection编译对Float32 `SimulationOperation`读取。
- [x] 4.15 删除Projection编译对Float32 `ProgramConstant.ToSingle()`读取。

## 5. Projection Identity与Revision

- [x] 5.1 从Projection payload删除ProgramHash字段。
- [x] 5.2 从Projection payload删除NumericProfileId字段。
- [x] 5.3 从Projection payload删除TargetAbiVersion字段。
- [x] 5.4 将Presentation ContractHash写入Projection payload。
- [x] 5.5 保留ProgramId诊断字段并与Contract一致。
- [x] 5.6 保留Gameplay SourceRevision诊断字段并与Contract一致。
- [x] 5.7 保留SemanticHash诊断字段并与Contract一致。
- [x] 5.8 提升Projection schema/revision identity。
- [x] 5.9 从ProjectionRevision token删除Float32 ProgramHash。
- [x] 5.10 把ContractHash纳入ProjectionRevision。
- [x] 5.11 把Animation Presentation dependency token纳入ProjectionRevision。
- [x] 5.12 把Equipment Presentation dependency token纳入ProjectionRevision。
- [x] 5.13 把Analysis artifact identity与content hash纳入ProjectionRevision。
- [x] 5.14 保持纯Presentation变化不改变任何Numeric ProgramHash。
- [x] 5.15 让Projection IsValid只校验Target-Neutral字段和Runtime payload完整性。

## 6. Runtime Contract与Target Adapter

- [x] 6.1 新建Float32 Presentation contract Adapter。
- [x] 6.2 让Float32 Adapter只接受已严格加载的Float32 Program。
- [x] 6.3 让Float32 Adapter复用唯一canonical contract builder。
- [x] 6.4 新建Fixed Presentation contract Adapter。
- [x] 6.5 让Fixed Adapter只接受已严格加载的Fixed Program。
- [x] 6.6 让Fixed Adapter复用唯一canonical contract builder。
- [x] 6.7 让Frontend、Float32与Fixed contract生成相同ContractHash。
- [x] 6.8 将Remote Presentation的semantic producer manifest接入同一contract builder。
- [x] 6.9 删除`CharacterPresentationProgramIdentity`旧类型或将其完整替换为新Contract。
- [x] 6.10 删除`CharacterPresentationProgramIdentity.From(Float32 Program)`。
- [x] 6.11 删除`CharacterPresentationProjection.RequireProgram`。
- [x] 6.12 删除`CharacterPresentationProjection.RequireSemanticProgram`。
- [x] 6.13 新增唯一`CharacterPresentationProjection.RequireContract`。
- [x] 6.14 将`CharacterPresentationProjectionAsset`收敛为单一Load Interface。
- [x] 6.15 删除Projection Asset的ProgramHash元数据出口。
- [x] 6.16 删除Projection Asset的NumericProfile元数据出口。
- [x] 6.17 删除Projection Asset的Target ABI元数据出口。
- [x] 6.18 将Animation Presentation Binding Index切到唯一Contract Interface。
- [x] 6.19 将Preview Playback切到Float32 Adapter加唯一Contract Interface。
- [x] 6.20 将Character Presentation Runtime Factory切到唯一Contract Interface。

## 7. Host与Presentation调用链迁移

- [x] 7.1 将Float32 CharacterPipelineHost切到Float32 Contract Adapter。
- [x] 7.2 将Float32 local owner Presentation创建切到唯一Projection Load。
- [x] 7.3 将Float32 remote Presentation创建切到唯一Projection Load。
- [x] 7.4 将ServerAuthoritative remote Presentation site切到唯一Contract builder。
- [x] 7.5 将Fixed CharacterHost切到Fixed Contract Adapter。
- [x] 7.6 删除Fixed CharacterHost手工producer identity数组。
- [x] 7.7 将Local Fixed owner Presentation创建切到唯一Projection Load。
- [x] 7.8 将Local Fixed neutral Presentation创建切到唯一Projection Load。
- [x] 7.9 将DeterministicRollback CharacterHost切到Fixed Contract Adapter。
- [x] 7.10 删除Rollback Host手工producer identity数组。
- [x] 7.11 将Rollback local Presentation创建切到唯一Projection Load。
- [x] 7.12 将Rollback remote Presentation创建切到唯一Projection Load。
- [x] 7.13 将Animation Playback Runtime构造路径切到唯一Contract。
- [x] 7.14 将Equipment Visual Runtime的Projection入口切到唯一Contract。
- [x] 7.15 让Actor registration分别保存精确Program identity与独立Presentation identity。
- [x] 7.16 保持ProgramHash/LayoutHash继续进入Session Program binding。
- [x] 7.17 保持Presentation ContractHash不进入Gameplay Snapshot与Network hash。

## 8. Build Request与Target Build Adapter

- [x] 8.1 定义显式`CharacterSimulationBuildRequest`。
- [x] 8.2 让Build Request必须提供Definition。
- [x] 8.3 让Build Request必须声明publish或dry-run。
- [x] 8.4 让Build Request必须提供有序且非空的Target Adapter集合。
- [x] 8.5 定义Target Build Adapter的NumericProfile identity。
- [x] 8.6 定义Target Build Adapter的compile Interface。
- [x] 8.7 定义Target Build Adapter的artifact staging Interface。
- [x] 8.8 定义Target Build Adapter的Unity wrapper destination。
- [x] 8.9 实现Float32 Target Build Adapter。
- [x] 8.10 将现有Float32 target compile/round-trip逻辑移入Float32 Adapter Implementation。
- [x] 8.11 实现Fixed Target Build Adapter。
- [x] 8.12 将Fixed target compile/round-trip逻辑移入Fixed Adapter Implementation。
- [x] 8.13 让Orchestrator只运行一次Semantic Frontend。
- [x] 8.14 让Orchestrator只解析一次Animation Analysis artifact set。
- [x] 8.15 让Orchestrator只编译一次Projection。
- [x] 8.16 让每个Target Adapter只消费validated Semantic IR artifact。
- [x] 8.17 让Orchestrator比较Frontend contract与每个Target contract。
- [x] 8.18 让dry-run复用完全相同的Frontend、Projection Compiler与Target Adapter。
- [x] 8.19 删除Orchestrator `CompileProjection`的Float32 Program参数。
- [x] 8.20 删除任何Target Adapter调用Projection Compiler的能力。

## 9. 原子发布与Stale检测

- [x] 9.1 扩展发布事务以stage唯一Projection和全部请求Target artifacts。
- [x] 9.2 stage每个Target canonical bytes并完成exact reload。
- [x] 9.3 stage每个Target Unity wrapper metadata。
- [x] 9.4 stageProjection payload与ContractHash。
- [x] 9.5 stageDefinition/generated references变更。
- [x] 9.6 提交前校验全部Target与Frontend contract一致。
- [x] 9.7 提交前校验Projection与Frontend contract一致。
- [x] 9.8 提交前校验ProjectionRevision canonical输入。
- [x] 9.9 让任一Target失败时不发布Projection或其它Target。
- [x] 9.10 让Projection失败时不发布任何Target。
- [x] 9.11 让generated reference写入失败时恢复完整旧发布组。
- [x] 9.12 从`CharacterSimulationProgramBuildService.IsStale`删除Projection/Float ProgramHash比较。
- [x] 9.13 将Projection stale改为ContractHash比较。
- [x] 9.14 保持Target Program stale由各自artifact expectation判断。
- [x] 9.15 将published ProjectionRevision重算改为不接收Float32 Program。
- [x] 9.16 更新compiled asset Inspector的Projection identity展示。
- [x] 9.17 删除Inspector中的Projection NumericProfile与Target ABI展示。
- [x] 9.18 发布完成后从正式Projection asset按Semantic Contract重载并返回落盘对象。
- [x] 9.19 让Projection producer payload以Kind作为Unity inline serialized tagged union的唯一判别器。
- [x] 9.20 删除域重载、资产导入和退出Play Mode触发的自动stale扫描与构建，只保留显式Build入口。

## 10. Product Build与重复路径清理

- [x] 10.1 让默认Editor角色编译入口使用显式安装Target catalog。
- [x] 10.2 让Float32 Local Product显式请求Float32 Target Adapter。
- [x] 10.3 让ServerAuthoritative Product显式请求Float32 Target Adapter。
- [x] 10.4 让Local Fixed Product显式请求Fixed Target Adapter。
- [x] 10.5 让DeterministicRollback Product显式请求Fixed Target Adapter。
- [x] 10.6 禁止Fixed-only Product请求或生成Float32 Program作为Projection前置产物。
- [x] 10.7 禁止Float32-only Product生成空Fixed artifact或wrapper。
- [x] 10.8 将`FixedCharacterSimulationProgramBuildService`接入正式Fixed Target Adapter。
- [x] 10.9 删除Rollback workflow内复制的Fixed compile/文件写入实现。
- [x] 10.10 删除Rollback workflow内手工`RequirePresentationIdentity`实现。
- [x] 10.11 删除Local Fixed workflow内手工Projection identity实现。
- [x] 10.12 删除所有直接调用Target Compiler并自行发布文件的Product路径。
- [x] 10.13 保持Network Test Product Build Workflow只协调正式Adapter。
- [x] 10.14 保持Run路径只读取已发布产物且不触发编译或修复。

## 11. 生成资产迁移与废弃删除

- [x] 11.1 提升Projection serialized schema/revision版本。
- [x] 11.2 删除旧Projection ProgramHash serialized field。
- [x] 11.3 删除旧Projection NumericProfileId serialized field。
- [x] 11.4 删除旧Projection TargetAbiVersion serialized field。
- [x] 11.5 删除旧ProjectionRevision v2算法。
- [x] 11.6 删除旧Projection Asset Load overload。
- [x] 11.7 删除旧semantic bypass命名和异常信息。
- [x] 11.8 删除旧Float32 exact Projection匹配命名和异常信息。
- [x] 11.9 重新生成Corin target-neutral Presentation Projection asset。
- [x] 11.10 重新生成Corin Float32 Program canonical artifact与wrapper。
- [x] 11.11 重新生成Corin Fixed Program canonical artifact与wrapper。
- [x] 11.12 更新Definition和Fixed Product对新generated assets的正式引用。
- [x] 11.13 删除旧generated Projection payload中的目标字段数据。
- [x] 11.14 删除迁移后无调用方的helper、using、partial build代码和手工identity数组。
- [x] 11.15 确认仓库不存在旧Projection reader、fallback、字段猜测或兼容转换。

## 12. 架构文档与规格统一

- [x] 12.1 更新`openspec/project.md`中的Authoring编译链为Projection与Numeric Target独立分支。
- [x] 12.2 更新`openspec/project.md`中的Program/Projection identity职责。
- [x] 12.3 更新`openspec/project.md`中的Editor CharacterSimulation Module归属。
- [x] 12.4 更新`character-animation-layer-runtime`中旧ProgramHash匹配口径。
- [x] 12.5 更新`add-local-fixed-gameplay-lab`仍引用旧semantic bypass的设计和实施清单。
- [x] 12.6 更新`refactor-timeline-animation-authoring-boundary`最终文档中的Projection build输入图。
- [x] 12.7 更新Rollback Build文档，明确Fixed-only产品不生成Float32 Program。
- [x] 12.8 记录最终ContractHash、ProjectionRevision、Float32 ProgramHash与Fixed ProgramHash的职责对照。
