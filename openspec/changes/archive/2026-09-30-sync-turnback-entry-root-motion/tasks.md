## 1. 恢复完整转身运动

- [x] 1.1 删除 Corin 的源入口表、选择状态及入口诊断，保持原有转身时长与出口条件
- [x] 1.2 Fixed/Float32 恢复使用统一的原始 SourceCurve 区间差求位移和 yaw
- [x] 1.3 删除入口计划、专用求值和 request 中未再消费的入口参数

## 2. 修正表现同步

- [x] 2.1 删除 SourceEntryTicks 表现事实、timeline 元数据与 Player 入口偏移
- [x] 2.2 有限 incoming 动作保持原入口，outgoing RunLoop 保持连续淡出
- [x] 2.3 有限动作接循环时只匹配一次入口，随后保持 continuation 自然推进
- [x] 2.4 保持循环到循环的既有同步与帧事务生命周期

## 3. 统一契约与说明

- [x] 3.1 更新 Control、运动绑定与表现事实版本，移除入口计划 revision 依赖
- [x] 3.2 文档统一为固定 TurnBack、匹配 RunLoop 入口，并移除旧方案的完成记录
- [x] 3.3 明确 Distance Matching 不在本次范围，保留现有运动曲线能力
