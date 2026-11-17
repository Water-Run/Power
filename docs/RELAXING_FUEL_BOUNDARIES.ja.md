# 相変化緩和燃料ポンプと凍結相計量

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · **日本語** · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## 物理モデル

`compressible_fuel_chamber` は `relaxing_fuel` 貯槽の体積を制御できます。可逆な軸仕事では蒸気質量分率を固定し、化学緩和はエネルギー保存の独立源項として扱います。`compressible_fuel_nozzle` は `phase_mode` が `frozen` のとき二つの緩和貯槽を結び、全質量、蒸気質量、よどみエンタルピーを輸送します。実際のソレノイド、ばね、ストッパーを備えた物理針弁で開度を計量できます。

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## 結合と数値手法

質量分率固定のエントロピー関係から体積変化後の圧力と温度を求めます。仕事は液体の冷圧縮エネルギーを含み、小変位では安定した増分関数を使います。同じ離散仕事が軸トルクを与えます。各全体区間で保存型管路・凍結ノズル輸送の前後に各ノードの相変化源項を一度ずつ適用します。区間は管路 CFL と小さいポート在庫の 0.02 比率で制限し、最大 4096 区間です。機械仕事と源項は分離し、結合輸送は一次精度で時間細分化を要します。

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## 使用、検証と制約

`relaxing-fuel-chamber` または `relaxing-pump-needle` を実行します。体積と仕事、実際と平衡の分率、相変化エントロピー、`total_vapor_delivered`、ポート在庫、全体蒸気残差を観測します。純相断熱線、独立した分率固定の圧力積分、微小仕事、可逆サイクル、独立理想蒸気軸 ODE と比較します。有限速度結合の細分化、共有貯槽の源項時間、全収支、取消、ロールバック、分岐、割当、可搬再生、実 MCP は個別に検証し、数値は検証証拠に記録します。

パラメータは `unverified` です。両ポートの領域と材料は同一とし、緩和ポートでは平衡通過を拒否します。貯槽の有限緩和は、ゼロ容積ノズル内部の化学変化や滞留時間を解像しません。相間滑り、液体負圧張力、核生成障壁則、噴霧、搬送ガス、燃焼受入室は含みません。移動貯槽は一様混合で平均速度ゼロ、管路内セルは剛体で所有者は一つです。臨界挙動から離れた希薄理想蒸気、宣言された正圧・温度範囲、正の潜熱エンタルピー、分離した共存比体積が必要です。初期蒸気分率は明示的な研究入力です。数値の一致は実測校正や実 Unity 検収を証明しません。

材料証拠に基づく有限速度のノズル内滞留を解像し、続いて噴霧と整合する燃焼受入室を加えます。点火、吸排気、機械損失、変速機作動、ECU/TCU 協調制御を進めます。実測校正と実 Unity 検収は別々に扱います。

[VALIDATION.ja.md](VALIDATION.ja.md)
