# 第1步：有效Foot位置权重

## 代码与证据

Runtime `df0c956`只删除`BuildOutput`中Correction长度超过0.1毫米才启用PositionWeight的门；目标合法性仍由原Support/Sole/Ankle/Rotation解析决定。Diagnostics `0550308`只迁移正式权重不变量及版本，facts59/diagnosis28，无CSV列变化。

新包`20260830-230331-636-4bb8583ea8c04db495cd6e9668ecbb86`，2086脚行、1043采样帧、1143列。精确Record仍为`43357ff3cd384e5cba75d2c31175b116`。Proof230552官方matched1044对恢复221238；固定效果对照为193957，不伪造其官方Proof。

218个共同Body/InputFormal/动画源/时钟/程序/Profile字段逐值一致；50195行geometry仅四个实例身份列不同。已通过Editor规定flags构建，57个既有警告、0错误并shutdown；随后显式Unity Refresh、确认域重载结束和Console无错误，再完成一次Replay及正式发布。结束后停止本任务Play。

原包、12文件ZIP与Proof保存于`Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-1-goal-validity`，逐文件SHA256一致，不覆盖193957、221050或212054。

## 有效性目标已达到

本包2086行均Ready/FormalWeight=1。原193957的71个Ready/Formal1/Pos0帧全部是Swing，本包三层PositionWeight全部为1；真实Physical Sole到Resolved Effective Sole误差中位20.389毫米→1.314微米，最大184.941毫米→3.703微米。67帧实际Ankle改变超过1毫米，全部来自这71个新启用的目标帧，原已有Goal帧没有超过1毫米Ankle改变。

新71帧全部有真实Reach，Available/Evaluated从1947增至2018；全包GoalClamp仍0。旧无Goal时的Solver残差占位0不是测量，不能据此称新旧Solver误差都为0。

L339/L515/R611在193957本来Pos1，本包仍Pos1，其Response、Resolved、Physical Foot及Pelvis逐值相同；不能把212054卸载尖峰不存在说成本对照消除了三个尖峰。L322/L466/L675的Foot/Pelvis也逐值相同。

## 脚部保护与局部变化

固定原525 Contact帧全部保留：Gap超过1/2/5/10厘米仍178/118/44/11，端点接触平面负距仍77/41/6/1，P90与最大值相同，逐帧差最大0.47微米。Anchor/State/Contact历史、位置响应与旋转政策没有变化。

37 Target的rules/eventKinds/scorePolicy一致、eligible集合不变。Stable命中144→143，Path205→199，Contact435→433，无新增超过2厘米命中；Path amplification是同批Path辅证。Path最大额外步131.164→111.223毫米；Stable最大36.401毫米保持。R121→122仍有4.787→12.452毫米的真实增加，不因未越2厘米而删除记录。

总分保持61.9，只是摘要。formal-goal-weight-policy的合同已按授权改变，旧/新0错误不是权重行为相同。

## 骨盆与膝盖外溢

新增Goal也启用了其Reach约束，不是单纯增加测量覆盖。348和852骨盆由原约8毫米下降变为约40毫米；711的下降49.401→52.339毫米，使超过5厘米计数30→31。711最终Output/Target与旧版相同，差来自710位置高2.938毫米；这是实际单步增量，不是分母变化。骨盆最大步89.362毫米未变。

SolvedKnee额外offset步超过5厘米415→422，超过10厘米仍103、全包最大值保持；R866原约30厘米翻侧后移至867约26.7厘米。只有Solved Knee事实，不冒称最终Physical Knee测量，也不称反弯已消失。

作者0、极小正权重、Unavailable/Suppress只有静态合同保留，本包无动态覆盖；Action和重入几何均无eligible，Landing腿姿态只有2个eligible。

## 本步裁决

按用户已明确批准的业务定义保留第1步：合法目标不能因Correction趋零撤销，71帧已证明新合同生效；固定接触和指定Foot保护项守住。此结论不等于整体无回归，骨盆、膝盖及R122真实增量全部保留。

继续用户已批准的第2步骨盆目标公式时，必须同时对照本包与固定193957；后续承担骨盆目标/可达响应的改进，不在脚侧添加反向补偿、改阈值或恢复卸载。若后续无法接受这些代价，按各自独立提交精确处置，不改写本包结果。
