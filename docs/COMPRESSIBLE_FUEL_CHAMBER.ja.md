# 軸駆動の圧縮性燃料室

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · **日本語** · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## 物理契約

`compressible_fuel_chamber` は回転軸と有限の `compressible_fuel` 貯槽を結びます。軸の運動で容積が変わり、同じ圧力仕事が流体エネルギーと軸トルクを変えます。貯槽は `equilibrium_fuel_duct` と燃料を交換し、平衡フラッシュ蒸発や凝縮を扱えます。

`node_a` は回転ポート、`node_b` の幾何所有者は一つです。`minimum_reference_volume`、`swept_volume`、`phase` を指定し、初期容積を初期軸角の幾何に合わせます。混合貯槽のバルク速度はゼロで、内部セルを兼用できません。質量のない正弦機構はピストンやカムの慣性を加えず、軸慣性を明示します。外部ケース圧力はゼロです。

## 仕事と輸送

閉じた仕事ステップは質量とエントロピーを一定にします。液体の冷圧縮エネルギーと熱内部エネルギーはダクトと同じ平衡ポテンシャルを使います。純液体は厳密な圧力積分、理想蒸気は解析的断熱線を使用します。相変化時はエントロピーを反転し最終エネルギーを確認します。採用したエネルギー差から共同機械解法の離散圧力トルクを求めます。正の `hydraulic_work` は軸から流体へのエネルギーです。

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

可動貯槽は半輸送、軸仕事全体、半輸送の順に更新します。輸送は数値拡散を伴う一次精度のままで、対称分割でも次数は上がりません。滑らかな閉液体の軸運動は二次収束します。機械区間の角度増分は 0.25 rad 以下です。CFL、物性境界、完全ロールバック、取消、独立分岐が適用されます。静止ポート／筐体への力積とピストン圧力仕事を分けて扱います。

## 利用と検証

`compressible-fuel-chamber-duct` または `electric-compressible-fuel-duct` を実行します。セル状態に加え、貯槽の質量、容積、圧力、温度、エントロピー、蒸気分率、室のトルク、動力、仕事を読みます。独立した液体仕事と理想蒸気極限、別途反転した平衡等エントロピー線と圧力求積、液体軸 ODE、理想蒸気半離散方程式の RK4 細分化を検証します。保存、電気／歯車／油圧／クラッチ合成、可搬再生、実 MCP、割り当てのない取引は別々に確認します。数値結果は検証証拠に記録します。

[VALIDATION.ja.md](VALIDATION.ja.md)

## 適用範囲と残作業

パラメータは `unverified` です。圧縮性で熱膨張ゼロの液体と理想蒸気からなる平衡純疑似流体、混合室、剛体セルを仮定します。温度・圧力範囲を守ってください。室のバルク運動エネルギーは解かず、逆止弁も自動では付きません。有限速度の核生成、相間滑り、実流体臨界挙動、実測校正、実 Unity 受入検証は残っています。

互換性のある圧縮性方向弁とニードル／ノズル境界を追加し、エンタルピー、運動エネルギー、相の仮定を明示します。その後、相緩和／滑り、点火、吸排気、機械損失、ECU/TCU 協調制御を進めます。実測校正と実 Unity 受入検証は別に行います。

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
