## 1. 正式方案与对照

- [x] 1.1 对比current specs及active Foot change，记录用户批准的三步顺序、公式与冲突边界
- [x] 1.2 固定193957与已逐值恢复的221050，保留212054失败证据及原Record身份

## 2. 脚位置目标有效性

- [x] 2.1 删除Correction米域幅度决定PositionWeight的门，保持合法目标、作者权重、Unavailable/Suppress及旋转原语义；df0c956按正式目标业务保留，不称整体质量已完成
- [x] 2.2 将唯一Diagnostics的正式权重不变量迁移到facts59/diagnosis28，不改质量评分或给旧包补字段；0550308已独立提交，Editor规定flags构建0错误
- [x] 2.3 独立构建加载并执行同Record Replay，核对近零修正、L339/L515/R611、全部脚与骨盆输出，保存结论及Proof；230331恢复71个有效目标，固定525Contact保持，骨盆711增加2.938毫米及其它外溢单列，详见experiments/20260830-step1-goal-validity.md

## 3. 双脚共同骨盆目标

- [x] 3.1 以同帧Resolved有效目标与原动画双脚最低高度差替换旧地形相对高度加正向补偿，清理旧目标字段与命名；Runtime候选构建27个既有警告0错误，效果待3.3独立Replay
- [x] 3.2 迁移唯一Diagnostics的目标输入/公式事实，保持真实输出与质量口径；628e293发布facts60/d29，1156列，19个新标量替换旧6列并严格复算，Editor构建0错误
- [x] 3.3 独立构建加载并同Record Replay，对照第1步及193957，保存骨盆和脚部变化；235033公式455帧生效，脚部对第一步保持，骨盆>5cm31→30但322/466/675仍硬压、711及膝盖移帧代价保留，详见experiments/20260830-step2-common-height.md

## 4. Reach与骨盆统一响应

- [x] 4.1 将原动画弯曲程度分为目标偏好，实际腿长及正式安全余量形成统一硬区间；Runtime发布独立PosturePreference与左右Reach/公共选择事实，效果待4.4
- [x] 4.2 将边界与响应收进一次Pelvis处理，删除Module后置二次改写，保留typed不可达与唯一Goal链；仅AdvancePelvisResponse持有一次积分与最终硬夹紧，未引入新速率/脚目标改动
- [x] 4.3 同步Runtime事实、Diagnostics与active规格，不保留旧字段/旧路径兼容；725d795发布facts61/d30，1216列，7旧列替换为67事实，Editor57既有警告0错误并shutdown，全量strict95/95
- [x] 4.4 独立构建加载并同Record Replay，检查322/466/675、全包骨盆大步及193957脚部收益，保存结果与未覆盖边界；010821已matched1044、脚保护保持，675/711改善但新增266/591/789大步及R826膝盖峰扩大，详见experiments/20260831-step3-unified-reach-response.md，不裁为全局通过

## 5. 收尾

- [x] 5.1 按实际效果保留或精确撤销失败小步，保存全部本地原包、Proof和独立提交；本次三步保留为带明确代价的可对照候选，不替换193957质量基线，不自动回退或追加变量
- [x] 5.2 全量strict校验并交付代码链路、对照结果和当前可测试状态，不自动归档；95/95通过，Unity已加载725d795组合并回到Edit、Console0错误，本次候选/原包/Proof独立保存，不把结构验收写成整体质量通过

## 6. 持续Goal：真实骨盆观测闭合

- [x] 6.1 在原Physical Writer同Completion冻结最终World Pelvis点，并补齐合法Pelvis Goal的源Pose有效性，不修改运动；3个Runtime文件完成，规定flags构建27既有警告0错误并shutdown，实际零行为待6.3
- [x] 6.2 唯一Diagnostics迁移真实World点和Goal残差有效性，删除默认零原Pose的假误差，不改变既有质量规则；928a7be发布facts62/d31，5新列/1221列，Editor57既有警告0错误并shutdown，strict95/95
- [x] 6.3 同Record回放证明观测修复的零行为变化，保留新包/Proof及原始世界与相对修正对照；020243官方matched1044，1184个非身份/非预期观测共同列逐值相同，37质量Target不变，ZIP/Proof独立保存，详见experiments/20260831-pelvis-world-observation.md；持续质量Goal未完成

## 7. 持续Goal：Corin正式频率独立实验

- [x] 7.1 以ae10348独立试验Corin正式频率3→2并重建Float32/Fixed产品，保持算法、Foot、Bend与TrainingEnemy不变；Runtime27既有警告0错误并shutdown
- [x] 7.2 完成023618同Record回放与原facts62/diagnosis31封口，确认1043帧实际消费2Hz；官方Proof因7个产品身份字段不同保持matched=false，1044完整输入/Body帧逐值相同
- [ ] 7.3 2Hz因回正迟滞208→270及实际Solved Knee超过10厘米160→174被拒绝，保留原包/Proof并只撤销ae10348；恢复3Hz后同Record回放确认尚待完成
