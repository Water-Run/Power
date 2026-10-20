# 弾性燃料容積の参照モデル

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · **日本語** · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## 構成則の範囲

`CompliantFuelVolume` は弾性容器内の純燃料に対する不変の Core 構成則参照モデルです。総質量と温度または蓄積エネルギーから、液体、蒸気、平衡共存を求めます。今後の二相燃料レールソルバーの閉包を提供します。既存グラフのレールは分離液体を輸送します。

## 熱力学

液体の密度と比熱は一定で、蒸気は理想擬似成分です。両相は既存の燃料膜物性基準を共有します。液体の化学ポテンシャルは `p/rho_l` を含むため、`p_0(T)` は液体排除体積を無視した物性基準であり、有限密度の純流体の飽和圧力ではありません。弾性壁の外圧はゼロで、圧力仕事を一度だけ蓄えます。

`StoredEnergyJoules` は熱量と弾性エネルギーを含みます。グラフへ接続する際、発熱量に対応する化学エネルギーは別の台帳で扱います。

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

共存状態は低密度蒸気の根を選びます。潜熱エンタルピーは `L_u + R T - p/rho_l` で、圧力導関数は Clapeyron 関係を満たします。固定質量での報告熱容量は相移動を含め、流体と弾性の合計エネルギーを微分します。壁はエントロピーを持ちません。

## Core API

`Evaluate` は温度と総質量を受け取ります。`Flash` は総質量と流体・弾性の合計蓄積エネルギーを受け取ります。`AdvanceHeat` は宣言した熱コンダクタンスと秒単位の局所時間を用い、後退 Euler 法で有限熱浴と熱交換します。属性名には SI 単位を明記します。これらは `Simulation` の有界整数時計を置き換えません。

`FuelVolumeStatus` は無効入力、温度・質量・エネルギー範囲外、ソルバー失敗を区別し、拒否時の出力は既定値です。`Flash` は指定エネルギーを保持し、浮動小数点の熱量再構成誤差を `CaloricResidualJoules` に示します。インスタンスは進行状態を持たず、成功呼び出しはマネージドメモリを割り当てません。

## 適用範囲

正の基準体積、コンプライアンス、液体密度、最大壁圧を宣言します。圧力上限は全共存曲線を覆い、`rho_l R T_min` 未満です。最低温度の共存壁膨張は基準体積に対し最低 `128 * epsilon`、`epsilon = 2^-52` とします。分解不能な壁変形と宣言したモデル範囲外の高密度蒸気分岐を除外しますが、臨界点は予測しません。

物性温度区間と `0 < M <= rho_l (V_0 + C p_max)` を守ります。真空、キャリアガス、実混合物、熱膨張、有限壁慣性、表面張力、有限速度相変化は対象外です。物性・壁パラメーターは `unverified` のままです。ポンプ充填、局所入口損失、混合物流の運動量、チョーク流、グラフの二相輸送には別の実装と証拠が必要です。

## 独立した証拠

化学ポテンシャル、Clapeyron 勾配、熱量・エントロピー導関数、弾性蓄積を含む Gibbs 関係を比較します。独立した拘束付きエントロピー最大化は飽和ソルバーを使いません。剛体容器、理想気体、示量性スケーリングの極限と有限熱浴 RK4 細分化が独立参照です。熱交換は総エネルギーを保存し、総エントロピー非減少で一次収束します。実測燃料妥当性や Unity 検証を意味しません。

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.ja.md)
