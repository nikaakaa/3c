## 1. 锁定现状与迁移边界

- [x] 1.1 读取并核对现行Session Composition、Network Model与Host Portability requirements。
- [x] 1.2 盘点`ISimulationSessionComposer`、`IFloat32SimulationSessionPreparedSource`和portable Float32 Composer调用点。
- [x] 1.3 盘点Local、Preview、Prediction与Authority Prepared Source创建点。
- [x] 1.4 盘点Standard Local、Preview、Prediction与Authority Pipeline Definition的factory lowering路径。
- [x] 1.5 记录公共Unity Composer对ServerAuthoritative namespace与具体类型的全部引用。
- [x] 1.6 记录`Float32SimulationPipelineFactorySet`中的Authority具体分支与`AuthorityCatalog`传播路径。
- [x] 1.7 记录迁移前ProgramHash、PipelineHash、Source policy hash和Composition identity形成输入。
- [x] 1.8 记录迁移前Host launch request的Authority约束清单。
- [x] 1.9 确认仓库不存在需要继续选择plain `SimulationPipelineDefinition`作为Float32 Pipeline的正式资产；存在时列入显式Definition迁移。
- [x] 1.10 锁定本change不修改协议、Solver、Program ABI、Pipeline Pass语义和Scene资产。
- [x] 1.11 锁定DotRecast change在其Host launch task前暂停，Agent Compiler change可继续并行。

## 2. 建立Portable Float32 Pipeline Runtime Package

- [x] 2.1 定义`Float32SimulationPipelineRuntimePackage`不可变合同。
- [x] 2.2 将Pipeline descriptor纳入package。
- [x] 2.3 将portable Pass factory catalog纳入package。
- [x] 2.4 将Float32 Pass runtime factory catalog纳入package。
- [x] 2.5 将Float32 Product runtime catalog纳入package。
- [x] 2.6 建立package稳定identity与diagnostics文本。
- [x] 2.7 校验package Backend identity与Float32 Pass Backend一致。
- [x] 2.8 校验descriptor Pass与portable factory identity、phase和configuration hash一致。
- [x] 2.9 校验runtime factory与portable factory集合一致。
- [x] 2.10 校验Product contract与Product runtime集合一致。
- [x] 2.11 让`Float32SimulationSessionCompositionRequest`原子持有runtime package。
- [x] 2.12 删除request中可被独立错误组合的四个Pipeline/factory参数。

## 3. 建立Runtime Launcher合同

- [x] 3.1 定义portable `IFloat32SimulationSessionRuntimeLauncher`。
- [x] 3.2 将Launcher输入限制为完整`Float32SimulationSessionCompositionRequest`。
- [x] 3.3 明确Launcher不得选择或替换五项Composition组件。
- [x] 3.4 明确Launcher不得创建第二Pipeline compiler、Backend或Runtime handle路径。
- [x] 3.5 建立`Float32StandardSessionRuntimeLauncher`。
- [x] 3.6 让Standard Launcher唯一委托portable `Float32SimulationSessionComposer`。
- [x] 3.7 建立Launcher重复调用与缺失输入的fail-closed行为。
- [x] 3.8 建立Launcher diagnostic identity且不改变网络或Composition identity。
- [x] 3.9 确认portable Launcher合同不引用Unity、Network Model或具体Source类型。

## 4. 建立Float32 Pipeline Package Provider

- [x] 4.1 定义`IFloat32SimulationPipelineRuntimePackageProvider`。
- [x] 4.2 建立pass-authored Pipeline package builder。
- [x] 4.3 让builder按四个Pipeline phase收集全部Pass runtime provider。
- [x] 4.4 让builder原子生成descriptor、portable factories、runtime factories与Product runtime。
- [x] 4.5 让Standard Local Pipeline Definition显式提供package。
- [x] 4.6 让Preview Pipeline Definition显式提供package。
- [x] 4.7 让ServerAuthoritative Prediction Pipeline Definition显式提供package。
- [x] 4.8 让ServerAuthoritative Authority Pipeline Definition将portable canonical catalog包装为neutral package。
- [x] 4.9 缺失package provider时明确失败，不按具体类型或Pass集合回退。
- [x] 4.10 核对四份正式Pipeline迁移前后descriptor hash不变。
- [x] 4.11 删除公共Factory builder中的ServerAuthoritative import与具体Pipeline判断。
- [x] 4.12 删除`AuthorityCatalog`模型专属输出槽位。

## 5. 让Prepared Source显式提供Launcher

- [x] 5.1 扩展`IFloat32SimulationSessionPreparedSource`返回非空Runtime Launcher。
- [x] 5.2 让Local Prepared Source返回Standard Launcher。
- [x] 5.3 让Preview Prepared Source返回Standard Launcher。
- [x] 5.4 让ServerAuthoritative Prediction Prepared Source返回Standard Launcher。
- [x] 5.5 建立ServerAuthoritative Authority Launcher并持有Source policy与locked roster。
- [x] 5.6 让Authority Prepared Source返回Authority Launcher接口而不暴露具体类型给公共Composer。
- [x] 5.7 校验Prepared Source descriptor、Target ABI与Launcher兼容。
- [x] 5.8 保持Source resource ownership和Dispose顺序不变。
- [x] 5.9 删除公共Composer读取Authority Prepared Source字段的路径。
- [x] 5.10 确认不存在按ModelId、PipelineId字符串或installed type选择Launcher的路径。

## 6. 收敛Unity Float32 Composition Request Builder

- [x] 6.1 将公共Unity Composer的通用request lowering提取为单一职责实现。
- [x] 6.2 保留Program Runtime Definition一致性校验。
- [x] 6.3 保留Float32 Actor registration与Program Runtime创建。
- [x] 6.4 保留Backend、Solver descriptor与Solver runtime校验。
- [x] 6.5 保留initial Character/World state创建。
- [x] 6.6 保留Committer、diagnostics和output route创建。
- [x] 6.7 从所选Pipeline provider取得neutral runtime package。
- [x] 6.8 构造唯一portable Float32 Composition Request。
- [x] 6.9 调用Prepared Source提供的Runtime Launcher。
- [x] 6.10 保持成功时资源转移和失败时Source/Solver释放顺序。
- [x] 6.11 删除`ServerAuthoritativeAuthorityPreparedSource`具体转换。
- [x] 6.12 删除公共Composer中的Authority条件分支。
- [x] 6.13 删除公共Composer对ServerAuthoritative namespace的引用。

## 7. 迁移ServerAuthoritative Authority Host Launch

- [x] 7.1 让Authority Launcher调用既有Host launch request校验对象。
- [x] 7.2 让Host launch request消费neutral Float32 runtime package。
- [x] 7.3 保留Authority Backend与Pipeline identity校验。
- [x] 7.4 保留package与canonical Authority Pass/Product合同校验。
- [x] 7.5 保留Source policy、TickRate与execution support校验。
- [x] 7.6 保留四个Authority Source port精确校验。
- [x] 7.7 保留locked roster、Program roster、initial state与World body一致性校验。
- [x] 7.8 保留output route、Solver、Committer与diagnostics校验。
- [x] 7.9 让Authority校验成功后唯一委托portable Float32 Composer。
- [x] 7.10 禁止Authority Launcher创建第二份Pipeline compile或Backend request。
- [x] 7.11 保持Unity Authority Prepared Source只负责交付policy、roster、ports和资源。
- [x] 7.12 保持Fantasy DotRecast Authority Scene可构造同一个Authority Launcher/Host launch输入。

## 8. 删除旧路径并同步后续change

- [x] 8.1 删除旧`Float32SimulationPipelineFactorySet`模型专属字段与分支。
- [x] 8.2 删除公共Unity Composer中的所有具体Network Model依赖。
- [x] 8.3 删除旧Authority Prepared Source具体读取入口。
- [x] 8.4 删除任何为迁移保留的旧Composer overload、fallback Launcher或兼容adapter。
- [x] 8.5 搜索并确认公共Simulation/Unity Composition代码不引用ServerAuthoritative具体Source或Pipeline类型。
- [x] 8.6 搜索并确认只有一个portable Float32 Composer创建Backend Runtime与LaunchPlan。
- [x] 8.7 搜索并确认Authority Launcher最终进入同一个portable Composer。
- [x] 8.8 更新`add-dotrecast-authoritative-server-backend`依赖顺序。
- [x] 8.9 更新DotRecast task 3.12为消费新的Authority Launcher合同。
- [x] 8.10 确认DotRecast Authority Scene不新增第二Composer或旧Host launch调用路径。

## 9. 文档、编译与严格校验

- [x] 9.1 更新`openspec/project.md`记录neutral runtime package与显式Launcher边界。
- [x] 9.2 更新受影响current spec口径并删除公共Composer可识别具体模型的旧描述。
- [x] 9.3 编译portable Core、Float32与ServerAuthoritative工程并带规定参数。
- [x] 9.4 编译Unity Runtime与Editor相关工程并带规定参数。
- [x] 9.5 每次编译后立即执行`dotnet build-server shutdown`。
- [x] 9.6 运行`openspec validate refactor-float32-session-runtime-launcher-boundary --strict --no-interactive`。
- [x] 9.7 运行`openspec validate --all --strict --no-interactive`并解决本change引入的冲突。
- [x] 9.8 核对tasks勾选与实际统一代码链路一致。
- [x] 9.9 将pass-authored package builder文件名迁为`Float32SimulationPipelineRuntimePackageBuilder.cs`并保留Unity meta identity。
- [x] 9.10 将implementation inventory明确拆分为迁移前与迁移后调用链。
- [x] 9.11 明确DotRecast Authority Scene manifest只由其独立change按2.1至2.20完整迁移，不建立半迁移别名。

## 10. 修正Authority身份锁定与失败清理

- [x] 10.1 让Authority Runtime Launcher显式接收Source握手或manifest锁定的完整Authority PipelineIdentity。
- [x] 10.2 将expected Authority PipelineIdentity纳入Launcher diagnostic identity并提升其semantic version。
- [x] 10.3 让Host launch request在调用Composer前核对Pipeline id、revision与schema。
- [x] 10.4 让唯一Float32 Composer在同一次Pipeline编译后、创建RuntimeHandle前精确核对expected PipelineHash。
- [x] 10.5 让Unity Authority Prepared Source从Compatibility传入实际Authority PipelineIdentity。
- [x] 10.6 让Unity Float32 Composer失败时分别尝试释放Solver与Prepared Source。
- [x] 10.7 保留原始异常类型、结构化failure与堆栈，并将清理异常附着为诊断数据。
- [x] 10.8 同步Launcher与DotRecast Authority Scene文档，不再描述外部普通.NET Worker接线。
- [x] 10.9 编译portable与Unity相关程序集并按规定关闭build server。
- [x] 10.10 运行本change、DotRecast change与全量OpenSpec strict validation。
