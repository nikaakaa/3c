## 1. 锁定迁移边界

- [x] 1.1 确认前置ServerAuthoritative代码完成并冻结。
- [x] 1.2 盘点Authority Pipeline descriptor、Pass config和factory创建点。
- [x] 1.3 盘点Authority Source queue、clock、baseline和output创建点。
- [x] 1.4 盘点Unity Fantasy control adapter与portable UDP endpoint边界。
- [x] 1.5 记录迁移前PipelineHash、Source policy hash和组件identity。
- [x] 1.6 记录旧Unity专属实现删除清单。
- [x] 1.7 锁定本change不修改协议、generated代码、Solver和Actor binding。

## 2. 迁移Authority Pipeline Catalog

- [x] 2.1 将稳定Pass顺序迁入portable ServerAuthoritative source set。
- [x] 2.2 将Pass config canonical lowering迁入portable catalog。
- [x] 2.3 建立唯一Authority Pipeline descriptor factory。
- [x] 2.4 建立唯一Authority Pipeline Pass factory catalog。
- [x] 2.5 注册既有Authority Pass runtime factory。
- [x] 2.6 注册既有Authority product runtime factory。
- [x] 2.7 让Unity Pipeline Definition只降低authoring字段。
- [x] 2.8 让Unity Float32 factory set复用portable catalog。
- [x] 2.9 核对迁移后PipelineId、Revision和Hash不变。
- [x] 2.10 删除Unity专属descriptor与factory拼装。

## 3. 迁移Authority Source Policy

- [x] 3.1 定义portable Authority Source policy。
- [x] 3.2 迁移missing-input hold策略。
- [x] 3.3 迁移input lead/lag窗口。
- [x] 3.4 迁移command/snapshot rate。
- [x] 3.5 迁移datagram budget与queue bounds。
- [x] 3.6 迁移MaxCatchUpTicksPerPump与MaxClockLagTicks。
- [x] 3.7 建立policy canonical codec与hash。
- [x] 3.8 让Unity Source Definition只降低authoring字段。
- [x] 3.9 核对迁移后policy hash不变。

## 4. 建立Portable Authority Source Runtime

- [x] 4.1 建立host-neutral Source runtime lifecycle。
- [x] 4.2 迁移locked Actor route。
- [x] 4.3 迁移每Actor command queue。
- [x] 4.4 迁移authority clock state。
- [x] 4.5 迁移missing-input选择。
- [x] 4.6 迁移每Client checkpoint baseline。
- [x] 4.7 迁移snapshot sequence与ack cursor。
- [x] 4.8 迁移reliable event有界queue。
- [x] 4.9 迁移full checkpoint有界queue。
- [x] 4.10 建立Source typed runtime ports。
- [x] 4.11 保持AuthorityReplicationBatch lowering唯一。
- [x] 4.12 保持Network Checkpoint codec唯一。
- [x] 4.13 保持portable UDP endpoint codec唯一。
- [x] 4.14 建立Source dispose顺序。

## 5. 建立Host-Neutral Control Transport

- [x] 5.1 定义control transport lifecycle。
- [x] 5.2 定义worker register result输入。
- [x] 5.3 定义roster与ticket输入。
- [x] 5.4 定义heartbeat输入输出。
- [x] 5.5 定义reliable event输出。
- [x] 5.6 定义full checkpoint request/response。
- [x] 5.7 定义leave与failure传播。
- [x] 5.8 禁止control transport承载routine command/snapshot。
- [x] 5.9 禁止control transport拥有Program和Pipeline策略。

## 6. 建立Authority Host Launch Request

- [x] 6.1 定义显式Program Runtime输入。
- [x] 6.2 定义Backend与Authority Pipeline输入。
- [x] 6.3 定义Source policy、runtime ports和restore source输入。
- [x] 6.4 定义locked roster与initial Character/World state输入。
- [x] 6.5 定义WorldSolver与descriptor输入。
- [x] 6.6 定义Committer、diagnostics和output routes。
- [x] 6.7 调用唯一portable Float32 Composer。
- [x] 6.8 缺失任一输入时fail-closed。
- [x] 6.9 禁止Host launch request选择默认Backend、Pipeline或Solver。

## 7. 切换Unity Authority Adapter

- [x] 7.1 让Unity Authority Source preparation创建portable Source runtime。
- [x] 7.2 让Unity Fantasy adapter实现host-neutral control transport。
- [x] 7.3 让Unity UDP adapter继续提供同一portable endpoint。
- [x] 7.4 让Unity Worker通过Host launch request创建runtime。
- [x] 7.5 保持现有Actor roster与WorldSolver输入。
- [x] 7.6 核对Program、Backend、Pipeline和Source identity不变。
- [x] 7.7 核对command/snapshot/reliable/full checkpoint bytes不变。
- [x] 7.8 删除旧Unity Authority queue与clock实现。
- [x] 7.9 删除旧Unity Pipeline factory集合。
- [x] 7.10 删除重复packet mapper和临时adapter。

## 8. 文档、编译与校验

- [x] 8.1 更新project.md记录Authority Host portable边界。
- [x] 8.2 编译portable Core、Float32和ServerAuthoritative工程并带规定参数。
- [x] 8.3 编译Unity Runtime/Editor相关工程并带规定参数。
- [x] 8.4 编译后立即执行`dotnet build-server shutdown`。
- [x] 8.5 运行`openspec validate refactor-server-authoritative-host-portability --strict --no-interactive`。
- [x] 8.6 运行`openspec validate --all --strict --no-interactive`并解决本change冲突。
