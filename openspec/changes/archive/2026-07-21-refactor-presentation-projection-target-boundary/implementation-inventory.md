# 实施清单

## 1. 身份字段来源

| 字段 | Frontend / Projection | Float32 Program | Fixed Program | Session / Network |
|---|---|---|---|---|
| ProgramId | Semantic IR header进入Contract与Projection诊断字段 | Program manifest | Program manifest | Program binding |
| SourceRevision | Semantic IR header进入Contract与Projection诊断字段 | Program manifest | Program manifest | diagnostics与stale |
| SemanticHash | Semantic IR header进入Contract与Projection诊断字段 | Program manifest | Program manifest | Program compatibility |
| ContractHash | schema、上述三项与ordered producer contract规范计算 | Adapter从严格加载Program重建 | Adapter从严格加载Program重建 | 只用于Presentation加载与registration诊断 |
| ProgramHash / LayoutHash | 不进入Projection | Float32 canonical payload | Fixed canonical payload | Session、Snapshot与Network Program binding |
| NumericProfile / ABI | 不进入Projection | Float32 target identity | Fixed target identity | Session compatibility |
| ProjectionRevision | Projection schema、ContractHash、Presentation依赖与Analysis artifact identity/content hash | 不进入Program | 不进入Program | Presentation stale与资源身份 |

## 2. 最终职责

| 模块 | 输入 | 输出 | 不负责 |
|---|---|---|---|
| Semantic Frontend | Character Definition与可达authoring | validated Semantic IR artifact | Unity表现资源、Target数值 |
| Presentation Semantic Contract | Semantic IR header与ordered producers | immutable ContractHash | ProgramHash、ABI、资源绑定 |
| Presentation Semantic Reader | validated Semantic IR | producer、source map、reference与numeric-neutral literal视图 | Float32/Fixed constant解码 |
| Projection Compiler | Semantic IR、Presentation authoring、Analysis artifacts | 唯一target-neutral Projection | Numeric Target Program生成 |
| Float32 / Fixed Target Adapter | validated Semantic IR与Definition identity | Target Program、严格重载结果、同一Presentation contract | Projection编译 |
| Build Orchestrator | 显式Build Request | 一次Frontend、一次Projection、请求Target集合与原子发布组 | 具体Target lowering细节 |
| Runtime Contract Adapter | 严格加载的Target Program或正式remote manifest | Presentation contract | 放宽Program校验 |
| Runtime Projection Asset | Presentation contract | 已校验Projection payload | authoring编译、stale计算、fallback |

## 3. 删除的旧路径

- `CharacterPresentationProgramIdentity`、`RequireProgram`、`RequireSemanticProgram`与Program型Animation Binding入口。
- Runtime `CharacterPresentationProjection.Build`、Runtime authoring join、Float32 operation/constant反读与Equipment compile partial。
- Projection中的ProgramHash、NumericProfile与Target ABI字段及两个Load口径。
- Rollback复制的Fixed compiler、文件写入与手工Presentation identity校验。
- Fixed-only产品为生成Projection而隐式调用Float32 Build的顺序依赖。

## 4. 并行文件边界

- Timeline / Foot Placement任务拥有Animation Analysis artifact、Foot feature payload与`CharacterAnimationPlaybackRuntime`的Visual time scale及循环时间修复。
- 本change只接入其正式resolver、diagnostic与revision token，不修改Foot Planner、IK或采样算法。
- MotionWarp任务拥有Program ABI与Motion Modifier payload；本change只消费最终Semantic IR与Target Program producer contract，不恢复旧ABI。

## 5. 发布顺序

```text
CharacterSimulationBuildRequest
  -> Semantic Frontend once
  -> canonical Semantic IR round-trip and stage
  -> Presentation Semantic Contract
  -> Analysis Artifact Resolver once
  -> Projection Compiler once
  -> ordered Target Adapters
  -> ContractHash cross-validation
  -> stage all Target canonical bytes and Unity wrappers
  -> publish one Projection and Definition references
  -> complete publication group
```

任一Semantic IR、Projection、Target、wrapper或Definition引用写入失败都会恢复旧发布组。Fixed-only请求只经过Fixed Adapter；Float32-only请求不创建空Fixed产物。

## 6. Corin 最终产物身份

| 产物 | ProgramId | SourceRevision | SemanticHash | Contract / Program / Layout identity |
|---|---|---|---|---|
| Presentation Projection | `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e` | `7e3b3866f0f2416366a7685cbad14db9faa0206991f0dcfc5520c64ceee1222f` | `297acbd80d0a62e377d4059263717293ba713e976311a7fc63b5d5b549f5a45f` | ContractHash `49d38eb31810874131fc941461036416c7c686876b49ca7d1ac57c693e6d97ed`；ProjectionRevision `5ed9f469bb522fbc9943769a15cd7bea25b90b9dca11ca9609676a76e4974cd0` |
| Float32 Program | 同上 | 同上 | 同上 | NumericProfile `float32-ieee754`，ABI `7`；ProgramHash `a2ad26b6e45a21e86f14a6ef9d7a72af64a5f28bfd597ce9505f4df065abc36b`；LayoutHash `c570af36b8f06ef0140554f1fe049679213377a562317695be4f73a9c4e06f96` |
| Fixed Q32.32 Program | 同上 | 同上 | 同上 | NumericProfile `fixed-q32.32`，ABI `6`；ProgramHash `3906feb1bb6befa60fc90014a323bfedf79ab602f21cd8db2de8456c4f59b4a6`；LayoutHash `038720d8270ca41fad1795c1a2958643946d5b4ae47c3ab20319791bd16e70f3`；CanonicalBytesHash `4cbc8ac82aa9eae03e9685b655dd9379371aede59e52179a570e181020bbc34a` |

`ContractHash`只证明三个输入端拥有相同的Presentation producer合同；两个`ProgramHash`与`LayoutHash`继续证明各自Numeric Target的精确执行产物。它们不互相替代，也不互相进入hash输入。
