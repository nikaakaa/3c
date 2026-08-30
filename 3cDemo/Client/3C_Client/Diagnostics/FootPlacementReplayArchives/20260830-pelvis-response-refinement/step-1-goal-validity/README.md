# 第1步有效Foot目标证据

- Runtime `df0c956`，Diagnostics `0550308`，facts59/diagnosis28。
- 原始包`20260830-230331-636-4bb8583ea8c04db495cd6e9668ecbb86`及历史采样均不删除、不覆盖。ZIP含原包12个文件，逐项SHA256核对；`replay-proof.json`是正式Proof原字节副本，清单见`manifest.json`。
- 同Record、同输入与调度，对比193957；官方Proof匹配恢复221238的1044帧，不伪造193957的官方Proof。
- 71个原Ready/Formal1/Pos0的Swing目标全部恢复，Physical到有效目标最大偏差184.941毫米→3.703微米。固定525 Contact质量和三个指定Foot保护窗保持。
- 同时保存骨盆711步长增加2.938毫米、348/852新增Reach下拉、Knee翻侧时序变化及R122的Path局部增加。不是全局无回归证明，也不是骨盆最终版。
- 新Goal有效性按用户授权保留；后续骨盆目标及Reach步骤必须独立采样，与本包和193957分别比较。完整说明在`openspec/changes/refine-character-pelvis-response/experiments/20260830-step1-goal-validity.md`。
