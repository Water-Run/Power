# 液体入口の運動量参照

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · **日本語** · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## 物理境界

`LiquidColumn` は水平剛体管の独立 C# 参照です。断面積、均一速度、液体密度、温度を一定とし、各局所区間の絶対ポート圧力を指定します。線形・二次抵抗は明示した研究入力です。グラフコンポーネントではなく、燃料ネットワークやノズル方程式を変更しません。

## 運動量とエネルギー

両ポートは同じ流量と速度を持ち、移流運動エネルギー流束が相殺されます。圧力仕事は蓄積運動エネルギーを変え、摩擦熱を生みます。等温浴が熱を受け取り、流体温度と内部エネルギーは一定です。エントロピー生成は摩擦熱を宣言温度で割った値です。有限熱在庫は表しません。

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

陰的中点法で単調なスカラー運動量式を解きます。逆流は同じ式でゼロ流量イベントに分割し、正逆の体積と熱を別々に保持します。成功呼出しは最大二つの内部区間を使います。定常平衡を行き過ぎると `StepTooLarge` を返すため時間を短縮します。エネルギーと力積残差は binary64 丸めで検査します。受理された粗い刻みも精度検査が必要です。

## コア API と数値証拠

SI の形状、密度、抵抗、温度、最小絶対圧力を明示して `LiquidColumn` を構築します。`LiquidColumnState` と秒単位時間を `Advance` に渡します。単位はプロパティ名に含まれます。結果は符号付き・方向別輸送、圧力仕事、運動エネルギー変化、摩擦熱、エントロピーです。拒否時は既定結果で入力不変、管理メモリ割り当てはありません。局所時間は整数シミュレーション時計を置き換えません。

独立した無損失加速、線形抵抗の指数解、二次抵抗の双曲正接解で極限を確認します。変動圧力の独立 RK4 は逆流を含む滑らかな二次収束を検証します。受動減衰、定常流、完全エネルギー収支、非負エントロピー、拒否、不変共有、割り当てを両アセンブリで検査します。

## 次の結合

パラメーターは `unverified` です。最低圧力は呼出側が宣言する液相有効境界で、蒸気圧や NPSH を予測しません。圧縮性、音波、重力、移動形状、熱蓄積、キャビテーション、相間滑り、相緩和は対象外です。二相燃料リンクは準静的のままです。実 Unity 受け入れと実測校正は別です。

`fuel_inertial_pipe` は二つの有限燃料ノードと有限熱壁を接続します。水平剛体管は両容器に加えて独自の液体質量、熱内部エネルギー、運動エネルギーを持ちます。密度一定で温度は完全混合により変化します。ポートと管路全体が液相である必要があり、既存の準静的リンクは別の二相挙動を保ちます。 [有限液体燃料管路の運動量](FUEL_INERTIAL_PIPE.ja.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
