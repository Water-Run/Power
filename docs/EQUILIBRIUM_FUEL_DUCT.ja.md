# 圧縮性平衡燃料ダクト

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · **日本語** · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## モデルと物性

`equilibrium_fuel_duct` は明示的な `compressible_fuel` セル間で質量、運動量、全エネルギーを輸送します。液体は圧縮性、蒸気は理想気体で、両相の圧力、温度、速度は共通です。局所平衡により凝縮とフラッシュ蒸発が生じます。有限の静止貯槽 2 個と有限熱壁が収支を閉じます。

液体は熱膨張ゼロ、比熱一定です。`K` は圧力ゼロでの体積弾性率で、実際の値は `K+p` です。冷圧縮エネルギー、化学ポテンシャル、エントロピー、平衡音速は同じポテンシャルから導きます。`a` は既存の液相エネルギーオフセット、`p0(T)` は液体体積を無視した飽和曲線、`x` は蒸気質量分率、`Cv_eq` は比体積一定での混合物内部エネルギーの温度微分です。`Tr`、`Pref`、`Lu_ref` は基準温度、圧力、潜内部エネルギーです。単位は SI です。

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## 輸送と境界

`liquid_bulk_modulus` を指定し、`compliance` を省略して、各ノードの `initial_velocity` を明示します。順序付きで重複しない内部 `cells` を 1–24 個指定し、各セルは一つのダクトに所属します。セル長は体積を `area` で割った値です。貯槽の速度はゼロで、内部セルを兼用できません。全ポートで同じ物性を使用します。`linear_resistance` と `quadratic_resistance` は管全体の抵抗を表し、セル体積に応じて配分します。`fluid_heat_fraction` は摩擦熱を流体と壁に配分します。直接の開度入力はありません。

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

保存形 Rusanov フラックスは同じサブステップ状態から全界面を更新します。速度上限は最大絶対速度と物性の音速上限の和です。CFL 制限は最小ポート体積の 0.2 倍を断面積と速度で割った値で、区間当たり最大 4096 サブステップです。輸送と局所的な厳密抵抗減衰は一次分割です。数値拡散は全エントロピー変化に含まれ、報告する摩擦熱とは区別します。静止境界が機械仕事ゼロで貯槽の運動量を受け、流入運動エネルギーは有限貯槽内で内部エネルギーになります。不正状態または丸め誤差を超えるエントロピー減少はバッチ全体を拒否します。刻みを小さくし、物性範囲とセル解像度を確認してください。

## 利用と検証

`equilibrium-fuel-condensing-duct` または `equilibrium-fuel-flashing-duct` を実行します。各セルの密度、速度、相分率、音速、温度、エネルギーを取得できます。ポートの質量・エネルギー、壁への力積、全在庫も観測可能です。独立検証は液体圧縮仕事、理想蒸気極限、化学ポテンシャル一致、Clapeyron と Gibbs の関係、エントロピー反転による音速、質量エネルギー反転、保存、有限壁の摩擦を扱います。セル平均の音響誤差は 8、16、24 セルで減少します。完全再生、ロールバック、取消、分岐、割り当てなしのステップ、可搬 asset、実 MCP は別々に検証し、詳細は検証証拠に記録します。

[VALIDATION.ja.md](VALIDATION.ja.md)

## 適用範囲と次の作業

パラメータは `unverified` のままです。指定温度・圧力範囲、正の潜エンタルピー、分離した共存比体積、希薄蒸気の条件を守ってください。剛体・等断面・水平管で瞬時均質平衡を仮定します。有限速度の核生成、相間滑り、表面張力、重力、管弾性、実流体の臨界挙動は含みません。一次数値拡散は相変化と音響の前線をぼかします。保存と再生だけでは空間精度や実測機器への妥当性を証明できません。実 Unity の受入検証は未完了です。

圧縮性ダクトを互換性のある軸駆動室とニードル／ノズル境界に接続し、圧力仕事と相の仮定を明示します。その後、相緩和／滑り、点火、吸排気、機械損失、ECU/TCU 協調制御を進めます。実測校正と実 Unity の受入検証は別に行います。

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
