## Context

见[proposal.md](proposal.md)。通用框架当前已在3C本地package中实现并通过Foot双输入Extractor与Unity编译验证，但其物理Owner仍是3C目录，正式identity也仍为`ThirdPerson.*`。`pik`已经固定使用`KK`、`com.kk.pik`与MIT许可证，并已成为独立Git／Unity项目；若直接复制现有package，会立即产生两个Generator binary、两套Schema identity和两个维护Owner。

当前约束：框架正式目标仍是Unity 2022.3与IL2CPP AOT；Generator固定Roslyn 3.8／.NET Standard 2.0；3C Foot change正在持续增加领域字段；`pik`不得反向依赖3C；项目不接受wrapper、fallback、vendor snapshot或同步脚本。

## Goals / Non-Goals

**Goals:**

- 建立一个可被3C、`pik`和未来Unity项目共同消费的独立KK package仓库。
- 保持现有框架ABI、AOT调用图、packet协议与Host边界，只迁移Owner和正式公开identity。
- 让源码、Analyzer、Schema／Program identity、版本、许可证和文档形成一个可发布闭包。
- 在一次破坏性迁移中删除3C旧Owner，避免任何旧新并行路径。

**Non-Goals:**

- 不把Foot、FinalIK、PoseGraph或Performance工作流迁入独立框架。
- 不在本change为`pik`发明新的领域Sampler或采样字段。
- 不建立NuGet、远程Collector、数据库、线上遥测或商业配置服务。
- 不保留ThirdPerson namespace兼容层，也不提供历史packet兼容Reader。

## Decisions

### Decision 1: 建立第三个独立仓库，而不是把框架归属给3C或pik

正式目录固定为：

```text
D:\Unity_Project_1\generated-diagnostic-sampling
├─ Packages/com.kk.generated-diagnostic-sampling
│  ├─ Runtime
│  ├─ Editor/Host
│  ├─ RoslynAnalyzers
│  └─ package.json
├─ Tools/KK.GeneratedDiagnosticSampling
│  ├─ Generator
│  ├─ Host
│  └─ Probe
├─ openspec
├─ README.md
└─ LICENSE
```

选择独立仓库，是因为框架语义既不属于3C Gameplay，也不属于FinalIK Foot Placement。把它放进`pik` monorepo虽然比留在3C更容易共享，但会让通用Camera、Simulation或AI采样反向归属于FinalIK产品。独立仓库增加一个版本发布点，换来清晰Owner与第三方可消费性。

### Decision 2: 正式身份一次改为KK，不保留ThirdPerson兼容

固定映射：

| 当前身份 | 正式身份 |
|---|---|
| `com.thirdperson.generated-diagnostic-sampling` | `com.kk.generated-diagnostic-sampling` |
| `ThirdPerson.GeneratedDiagnosticSampling` | `KK.GeneratedDiagnosticSampling` |
| `ThirdPerson.GeneratedDiagnosticSampling.Host` | `KK.GeneratedDiagnosticSampling.Host` |
| `ThirdPerson.GeneratedDiagnosticSampling.Generator` | `KK.GeneratedDiagnosticSampling.Generator` |
| `Tools/ThirdPersonGeneratedDiagnosticSampling` | `Tools/KK.GeneratedDiagnosticSampling` |

选择现在破坏性重命名，是因为当前只有3C Foot一个正式消费者，迁移面仍可控。保留ThirdPerson identity虽然改动少，但会把3C项目品牌永久泄露进独立公共API；wrapper或type forwarder则会让两个assembly identity同时长期存在。

### Decision 3: 本地消费者使用唯一file dependency，package声明正式版本依赖

本地开发路径固定为：

```text
3C manifest:
file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling

pik manifest:
file:../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling
```

路径相对于各自`Packages/manifest.json`解析。`com.kk.pik/package.json`声明`com.kk.generated-diagnostic-sampling: 0.1.0`，项目manifest的file dependency提供本地正式实现。以后发布Git tag时只改变消费者的正式依赖来源，不在同一项目保留file／Git双配置。

不使用Git submodule，是因为它会在每个消费者仓库内再次出现package工作树并增加submodule状态管理；不使用复制脚本，是因为脚本无法防止消费者直接修改镜像。独立file dependency让两个Unity项目实时解析同一目录。

### Decision 4: Source与Analyzer是同一个发布原子

独立仓库保留Generator源码、portable工程和package内Analyzer DLL。发布前必须用规定参数构建Generator，关闭build server，把唯一Release DLL复制到package，然后核对SHA-256与MVID完全一致。package version、Generator identity、assembly binding、生成source hash、Schema identity与Program identity一起变化。

选择提交Analyzer binary，是因为Unity package导入需要可直接加载的Roslyn Analyzer，不能要求每个Unity消费者先构建Generator。代价是仓库必须把“源码／DLL一致性”作为强制发布Gate。

### Decision 5: 三仓迁移按一个migration identity协调提交

迁移使用同一文字identity记录在独立仓库、3C和`pik`提交中，顺序为：

```text
建立并验证独立仓库
-> 更新3C using/asmdef/manifest并删除旧Owner
-> 更新pik manifest/package依赖
-> 分别portable build
-> 依次Unity refresh 3C与pik
-> 严格校验三仓OpenSpec与源码唯一性
-> 提交三个仓库的闭合状态
```

工作区中可以在提交前短暂同时存在新旧目录用于机械迁移，但任何可提交状态都不得让Unity同时解析两个Analyzer，也不得让3C Foot同时生成旧新Program。回退必须三个仓库一起回到迁移前提交，不保留运行时选择开关。

### Decision 6: 独立仓库安装框架真相，3C只保存消费者事实

独立仓库的`openspec/project.md`与current specs拥有Generated Diagnostic Sampling Framework和package distribution真相。3C的`add-generated-diagnostic-sampling-framework`保留历史实施证据，并在本迁移完成后把Owner指向独立仓库；3C current specs只描述Foot／Performance如何消费Capability，不复制框架内部需求。`pik`只描述自身领域接入和package依赖。

## Risks / Trade-offs

- [Foot change正在增加字段，重命名可能产生冲突] → 在迁移窗口冻结Foot插件文件，先完成机械namespace／asmdef迁移，再由原任务继续新增字段。
- [三个Git仓库不能事务提交] → 使用同一migration identity和精确前置commit，所有编译与Unity refresh通过后才提交最终状态；失败时三仓一起回退。
- [file dependency依赖固定本机目录布局] → 当前三个项目都位于`D:\Unity_Project_1`并以此作为正式本地工作区；公开发布时使用单一版本来源替换，不保留双配置。
- [Generator重命名导致所有identity变化] → 将变化视为一次明确breaking migration，删除旧请求与未完成产物，重建Program／manifest，不实现旧packet兼容。
- [独立仓库过早承载未使用能力] → 只迁移已经由3C Foot实际使用并验证的框架，不新增Camera、AI、远程传输或通用查询语言。

## Migration Plan

1. 在`D:\Unity_Project_1\generated-diagnostic-sampling`初始化独立Git、OpenSpec、MIT许可证和固定目录结构，记录3C来源commit。
2. 迁移package与Tools并完成KK全量重命名；更新Generator硬编码Attribute／生成类型引用，重建Analyzer并核对源码／binary identity。
3. 在独立仓库用Probe验证具体View／Metadata双输入、table、lease、codec与compile closure，严格校验OpenSpec。
4. 冻结3C Foot诊断接入窗口，更新Unity manifest、packages lock、using、asmdef、portable工程与Repository Policy；删除3C旧package和Tools。
5. 重建3C Foot Generated Program与Capability identity，按规定参数构建portable工程并执行3C Unity refresh。
6. 更新`pik`项目manifest与`com.kk.pik`依赖，执行`pik` Unity refresh，确认项目只消费独立package。
7. 搜索三个仓库，确认ThirdPerson采样identity、第二package源码、同步脚本和旧Analyzer为零；更新各自OpenSpec Owner事实并提交同一migration identity。

回退时，独立仓库保留未发布提交，3C与`pik`同时恢复迁移前manifest／namespace；不得在任一消费者中临时恢复复制package。
