# 有限速度の燃料相変化緩和

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · **日本語** · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## 物理モデル

`relaxing_fuel` ノードは独立した蒸気質量分率を持ちます。液体と蒸気は圧力、温度、速度を共有しますが、化学平衡には明示した有限時間を要します。`relaxing_fuel_duct` は明示セルで質量、運動量、全エネルギー、蒸気質量を輸送します。滞留時間はセル形状と実際の流速から決まります。

閉包は平衡燃料モデルの圧縮性液体の冷ポテンシャルと理想蒸気を引き継ぎます。初期質量分率は明示し、平衡状態へ投影しません。質量、体積、内部エネルギーを固定したとき、分率は一定の `phase_relaxation_time_ns` で平衡値へ指数的に近づきます。潜熱は内部のエネルギー収支に含まれます。化学親和力は液相と蒸気相の化学ポテンシャル差であり、相変化との積を温度で割ると非負のエントロピー生成になります。これは研究用緩和則で、実測核生成相関ではありません。

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## 輸送と数値手法

保存型 Rusanov 流束で四つの場を輸送します。各グローバル CFL 区間で局所相変化の半ステップ、輸送、半ステップを実行します。共有貯槽の源項は管路数にかかわらず一度だけ適用します。孤立定容状態の源項軌跡は厳密ですが、結合輸送は一次誤差を持ちます。CFL は質量分率凍結時の音速上限を使用し、相変化と管路のエントロピーを個別に検査します。範囲外の状態はバッチ全体をロールバックします。

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## 使用、検証と制約

`relaxing-fuel-evaporation` または `relaxing-fuel-duct` を実行します。実際と平衡の分率、化学親和力、相変化エントロピー、ポートの蒸気収支、全体の蒸気残差を観測できます。蒸発・凝縮履歴は受理した各刻みの正味源項を符号で分けた値で、微視的な順逆の総流量ではありません。独立指数解、エントロピー微分、凍結音響、剛な平衡極限、時間自己細分化、独立線形音響による格子細分化を確認します。トランザクション、可搬再生、実 MCP は別々に検証し、数値は検証証拠に記録します。

パラメータは `unverified` です。`initial_vapor_mass_fraction` は [0,1]、緩和時間は 1 ns から一時間までの整数で指定します。材料の温度範囲と正圧範囲を守ります。剛体水平等断面管、単一純擬似流体、液体の熱膨張ゼロ、理想蒸気を仮定します。相間滑り、界面伝熱遅れ、表面張力、噴霧、搬送ガスは含みません。緩和領域とポンプ、ノズル、移動室、燃焼受入室の直接結合は未実装です。保存、再生、細分化は実機妥当性や実 Unity 検収を証明しません。 実流体の臨界領域から離れた希薄蒸気に限り、材料は正の潜熱エンタルピーと分離した共存比体積を維持する必要があります。

有限速度と整合する境界結合を拡張し、材料の緩和実測値を取得してから、解像された噴霧と燃焼受入室を追加します。点火、吸排気、機械損失、変速機作動、ECU/TCU 協調制御を続けます。実測校正と実 Unity 検収は別々に進めます。

[VALIDATION.ja.md](VALIDATION.ja.md)
