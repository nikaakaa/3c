## 1. 输入合同
- [ ] 1.1 盘点 `PredictionInputFrame` 现有字段
- [ ] 1.2 标记可同步字段
- [ ] 1.3 标记 local-only 字段
- [ ] 1.4 标记 replay 派生字段
- [ ] 1.5 定义 `FrameSyncPlayerId`
- [ ] 1.6 定义 `FrameSyncUnitId`
- [ ] 1.7 定义 `FrameSyncInputFrame`
- [ ] 1.8 定义 `FrameSyncButtonFact`
- [ ] 1.9 定义 move intent 坐标空间
- [ ] 1.10 定义 look/aim intent 坐标空间
- [ ] 1.11 定义 local input sequence
- [ ] 1.12 定义 target intent stable id

## 2. Action Request 合同
- [ ] 2.1 确认 action stable id 来源
- [ ] 2.2 定义 Dodge request facts
- [ ] 2.3 定义 Attack request facts
- [ ] 2.4 定义 Jump request facts
- [ ] 2.5 定义 Interact request facts
- [ ] 2.6 定义 pressed/held/released 合法组合
- [ ] 2.7 定义 held 不重复生成 pressed
- [ ] 2.8 定义 released 不生成新 request
- [ ] 2.9 定义 request sequence 去重
- [ ] 2.10 定义 request 到 `InputRequestBuffer` 的映射
- [ ] 2.11 定义 accepted/rejected facts 的 snapshot 归属

## 3. Confirmed Input Set
- [ ] 3.1 定义 `ConfirmedInputSet`
- [ ] 3.2 定义 `ConfirmedPlayerInput`
- [ ] 3.3 定义 server sequence
- [ ] 3.4 定义 confirmed tick
- [ ] 3.5 定义 player/unit 排序规则
- [ ] 3.6 定义 duplicate input 诊断
- [ ] 3.7 定义 missing input 诊断
- [ ] 3.8 定义 late input 诊断
- [ ] 3.9 定义 wrong tick 诊断
- [ ] 3.10 定义 confirmed tick 裁剪边界
- [ ] 3.11 定义 set 不包含角色状态的静态边界

## 4. Version Handshake
- [ ] 4.1 定义 protocol version
- [ ] 4.2 定义 frame sync input schema version
- [ ] 4.3 定义 checksum schema version
- [ ] 4.4 定义 action catalog hash
- [ ] 4.5 定义 locomotion config hash
- [ ] 4.6 定义 state machine config hash
- [ ] 4.7 定义 motion profile hash
- [ ] 4.8 定义 input mapping version
- [ ] 4.9 定义 handshake success result
- [ ] 4.10 定义 handshake failure reason
- [ ] 4.11 定义缺失 hash 失败

## 5. Converters
- [ ] 5.1 定义 `PredictionInputFrame -> FrameSyncInputFrame`
- [ ] 5.2 定义 `FrameSyncInputFrame -> PredictionInputFrame`
- [ ] 5.3 定义 `FrameSyncInputFrame -> PredictionInputFrame` 后复用现有 replay adapter 进入 `CharacterFrameInput`
- [ ] 5.4 定义 action facts 到 input buffer 的回灌
- [ ] 5.5 定义 camera basis 派生事实的映射
- [ ] 5.6 定义 target intent 缺失诊断

## 6. 自动测试
- [ ] 6.1 添加输入 DTO 纯数据测试
- [ ] 6.2 添加 converter round-trip 测试
- [ ] 6.3 添加 move intent clamp 测试
- [ ] 6.4 添加 button facts round-trip 测试
- [ ] 6.5 添加 Dodge pressed 生成 request 测试
- [ ] 6.6 添加 held 不重复生成 pressed 测试
- [ ] 6.7 添加 released 不生成 request 测试
- [ ] 6.8 添加 confirmed input 排序测试
- [ ] 6.9 添加 duplicate input 诊断测试
- [ ] 6.10 添加 missing input 诊断测试
- [ ] 6.11 添加 late input 诊断测试
- [ ] 6.12 添加 wrong tick 诊断测试
- [ ] 6.13 添加 handshake hash 一致通过测试
- [ ] 6.14 添加 handshake hash 不一致失败测试
- [ ] 6.15 添加 no camera sync 静态测试
- [ ] 6.16 添加 no Unity Object 静态测试
- [ ] 6.17 添加 no action result in input history 测试

## 7. 验证
- [ ] 7.1 运行相关 EditMode 测试
- [ ] 7.2 运行 `openspec validate add-frame-sync-input-authority-foundation --strict --no-interactive`
- [ ] 7.3 运行 `openspec validate --all --strict --no-interactive`
