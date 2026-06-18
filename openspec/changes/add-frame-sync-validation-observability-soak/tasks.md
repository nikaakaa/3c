## 1. Fake Transport Synctest
- [ ] 1.0 对照 `Ref/ggpo/src/lib/ggpo/backends/synctest.*` 梳理 synctest 参考点
- [ ] 1.1 定义 fake client fixture
- [ ] 1.2 定义 fake room fixture
- [ ] 1.3 定义 fixed seed input generator
- [ ] 1.4 定义 multi-client submit loop
- [ ] 1.5 定义 confirmed input broadcast loop
- [ ] 1.6 定义 latency injection
- [ ] 1.7 定义 reorder injection
- [ ] 1.8 定义 duplicate injection
- [ ] 1.9 定义 missing injection
- [ ] 1.10 定义 late input injection
- [ ] 1.11 定义 correction injection
- [ ] 1.12 定义 checksum mismatch injection

## 2. Soak Runner
- [ ] 2.0 对照 `Ref/ggpo/src/apps/vectorwar` 梳理最小 demo 验证参考点
- [ ] 2.1 定义 soak config
- [ ] 2.2 定义 seed
- [ ] 2.3 定义 tickCount
- [ ] 2.4 定义 clientCount
- [ ] 2.5 定义 rollbackWindow
- [ ] 2.6 定义 network chaos profile
- [ ] 2.7 定义 stopOnFailure
- [ ] 2.8 定义 checkedWindows
- [ ] 2.9 定义 pass summary
- [ ] 2.10 定义 fail summary

## 3. Motion Determinism Audit
- [ ] 3.0 对照 GGPO Developer Guide 的 game state / non-game state 分离原则审计状态边界
- [ ] 3.1 审计 MoveLoop
- [ ] 3.2 审计 TurnBack
- [ ] 3.3 审计 Dodge Directional
- [ ] 3.4 审计 Dodge Backstep
- [ ] 3.5 审计 root motion profile
- [ ] 3.6 审计 motion warping
- [ ] 3.7 审计 CharacterController collision
- [ ] 3.8 审计 moving platform
- [ ] 3.9 审计 AnimatorDirect
- [ ] 3.10 生成 strict field table
- [ ] 3.11 生成 presentation drift table
- [ ] 3.12 生成 risk table

## 4. Observability
- [ ] 4.1 定义 `FRAME_SYNC_HANDSHAKE`
- [ ] 4.2 定义 `FRAME_SYNC_CONFIRMED_INPUT`
- [ ] 4.3 定义 `FRAME_SYNC_CORRECTION`
- [ ] 4.4 定义 `FRAME_SYNC_CHECKSUM`
- [ ] 4.5 定义 `FRAME_SYNC_SOAK_RESULT`
- [ ] 4.6 定义 `FRAME_SYNC_FIRST_MISMATCH`
- [ ] 4.7 定义 `FRAME_SYNC_MOTION_AUDIT`
- [ ] 4.8 定义 debug snapshot DTO
- [ ] 4.9 定义 summary formatter
- [ ] 4.10 定义 first mismatch formatter

## 5. 自动测试
- [ ] 5.1 添加 fake no-latency pass 测试
- [ ] 5.2 添加 fake latency prediction correction 测试
- [ ] 5.3 添加 fake reorder sorting 测试
- [ ] 5.4 添加 fake duplicate diagnostic 测试
- [ ] 5.5 添加 fake missing diagnostic 测试
- [ ] 5.6 添加 fake late diagnostic 测试
- [ ] 5.7 添加 checksum mismatch diagnostic 测试
- [ ] 5.8 添加 soak deterministic seed 测试
- [ ] 5.9 添加 soak pass summary 测试
- [ ] 5.10 添加 first mismatch summary 测试
- [ ] 5.11 添加 motion strict table completeness 测试
- [ ] 5.12 添加 debug diagnostics 不进入 snapshot 测试
- [ ] 5.13 添加正式角色 prefab 不依赖 debug tooling 静态测试

## 6. 验证
- [ ] 6.1 运行相关 EditMode 测试
- [ ] 6.2 运行 overnight soak 命令或等价自动长跑入口
- [ ] 6.3 保存 `FRAME_SYNC_SOAK_RESULT`
- [ ] 6.4 若失败，保存 `FRAME_SYNC_FIRST_MISMATCH`
- [ ] 6.5 运行 `openspec validate add-frame-sync-validation-observability-soak --strict --no-interactive`
- [ ] 6.6 运行 `openspec validate --all --strict --no-interactive`
