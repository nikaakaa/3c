# 当前组合112611自动回放证据

实际测试提交721b69f，包含902019b地面缓存时机修正，骨盆仍3Hz／20毫米。使用原Record43357ff3cd384e5cba75d2c31175b116，经正式Replay、采样与Finalizer发布；没有修改代码、配置或规则。

原始run为20260831-112611-018-2429691288e6434a8588a55de100efc2。14文件ZIP及独立原字节Proof ZIP逐项SHA与原件一致，原包不删除或覆盖。

- manifest.json：版本、输入／Proof、正式420帧查询、逐文件SHA。
- raw-audit.json：对101451的1221个共同列及真实骨盆／Knee统计。
- quality-audit.json：37项规则、计数和完整score对账。

结果：1196个共同列逐值相同，差异为24个运行／Surface／Path身份和GroundPathEdgeCount；新增3个表面事实列。骨盆、Foot及Solved Knee输出均保持。世界Y绝对步超过5厘米仍33次，其中向下24次、17次触发硬Reach，420下降80.210毫米。不能将“回放完整”写成“骨盆下陷修复”。

官方Proof为baseline-created，不冒称官方A/B；另对已保存101451原字节Proof的1044完整frames、Runtime identity与输入／Body哈希逐值一致。详见仓库openspec/changes/refine-character-pelvis-response/experiments/20260831-current-integration-replay.md。
