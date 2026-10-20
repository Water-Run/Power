# 蒸気質量分率を凍結した燃料流出と有限受容室

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · **日本語** · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## 物理モデル

`fuel_discharge` は有限燃料源を `finite_fuel_film` の気体ノードへ接続します。`phase_mode` を `frozen` と宣言し、ノズル通過中の蒸気質量分率を固定します。両端のバルクは再平衡化します。均質な両相は温度と速度を共有し、液体は非圧縮、蒸気は理想擬似種です。材料特性と液体密度は一致する必要があります。

上流よどみ圧力と温度に対し、受容圧力から上流圧力まで次の質量流束を最大化します。唯一の内部極大がチョーク流量を制限します。純液体極限はベルヌーイ、純蒸気極限は理想気体の音速関係です。流量係数は有効スロート面積を調整します。

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## エネルギー、体積、運動量

供給源は輸送するよどみエンタルピーを失います。`wall_kinetic_fraction` が理想完全膨張噴流の運動エネルギーの一部を有限静止壁へ配分し、残りは受容流体へ入ります。この参考速度は受容圧力までの理想膨張を仮定し、実測出口速度を予測しません。噴流力積は静止受容壁へ、逆反力は筐体へ伝わります。静止反力は仕事をせず、ピストンやニードル力を追加しません。

`finite_volume_density` を明示します。気体 `volume` は空洞体積から液体質量/密度を引いた値、膜 `volume` は液体排除体積です。移動シリンダーは受容系全体の質量・エントロピーを保って圧力仕事を計算します。排除仕事は内部に留まります。固定空洞とクランクシリンダーを支持し、直動気体ピストンには対応しません。

## 使用と証拠

`finite-volume-fuel-discharge` で混相源のチョーク、`pump-fed-finite-volume-cylinder` で電動ポンプ、実際のソレノイドニードル、規定燃焼を実行します。任意の `needle` と `valve_timing` は開度に乗算されます。新例は明示電圧パルスを使い、要求サイクル質量やサンプル式投与制御はありません。`jet_kinetic_energy`、`jet_impulse`、`force`、`throat_pressure`、`exit_temperature`、`exit_velocity`、`choked` を全在庫と読みます。最後の四値は停止時も最後に採用された流動状態を保持します。

独立したベルヌーイ・理想気体極限、別の混相流束最大化、エネルギー/エントロピー、独立積分したノズル/受容室 ODE で検証します。時間収束は一次です。可搬再生、実 MCP、ロールバック、キャンセル、独立分岐、割り当て検査が統合を確認します。実 Unity Editor/Play/Player/IL2CPP は未検証です。

## 適用範囲と次の作業

パラメーターは `unverified` です。宣言温度、正の気体体積、希薄蒸気領域を守ります。入口局所慣性、有限速度核生成、相間滑り、解像噴霧、実流体臨界挙動、実測校正を含みません。規定燃焼は化学平衡や反応エントロピーの精度を示しません。入口動特性、点火、吸排気、損失、ECU/TCU 協調へ進みます。

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
