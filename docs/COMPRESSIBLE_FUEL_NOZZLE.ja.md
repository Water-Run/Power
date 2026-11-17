# 相分率を凍結した圧縮性燃料ノズル

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · **日本語** · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## 物理契約

`compressible_fuel_nozzle` は軸駆動室を含む有限の `compressible_fuel` 貯槽間に一方向の流れを与えます。液体圧縮エネルギーと理想蒸気は貯槽と同じ物性を使います。`phase_mode` を `frozen` と明示し、通過中の蒸気質量分率を一定に保ち、貯槽は再平衡させます。

面積、有効面積の流量係数、`wall_kinetic_fraction` と有限熱壁を指定します。`initial_input` は [0,1] の開度で、任意の入力チャンネルで変更します。任意の `needle_node`、`closed_position`、`full_open_position` は指令に実際の並進ニードル開度を掛けます。両ポートのバルク速度はゼロで、物性と発熱量は一致し、内部セルには接続できません。逆圧差では理想的な開弁圧差ゼロの境界が閉じます。

## エネルギーとチョーク

両相は圧力、温度、速度を共有し、相間質量移動を抑制します。エントロピー関係と冷圧縮エネルギーから等エントロピーのエンタルピー差を求めます。唯一の質量流束最大値は平衡音速とは異なる凍結音速を使います。係数は有効面積を調整します。チョーク時の出口エネルギーは受入圧力まで完全膨張した理想噴流です。供給側の全エンタルピーが流体と壁の熱を一度だけ賄います。力積は静止した熱化を表し、筐体全推力やニードル軸力を表しません。

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

保存的な一次サブステップが同じ質量とエンタルピーを移し、噴流運動エネルギーを流体と壁に配分します。物性状態と対全体のエントロピーを確認します。区間当たり最大 4096 ステップ、候補当たり 64 回の半減、初期移動量は小さい貯槽質量の 0.1 倍以下です。閉鎖判定には最大体積弾性率またはポート圧力の 128 binary64 epsilon 倍の丸め幅を許容し、出力圧力は切り詰めません。失敗はバッチ全体をロールバックします。刻み、在庫、物性範囲、行程、面積を確認してください。

## 利用と検証

`compressible-fuel-nozzle` は有限蒸気排出、`compressible-pump-needle` は管供給、軸、一方向入口、電磁ニードルを確認します。累積質量、エンタルピー、噴流運動エネルギー、壁熱、力積、エントロピーを読みます。平均流量と力は完全な刻みを覆い、喉部・出口・分率は最後に採用した通過状態です。厳密液体仕事、気体／液体の解析的チョーク、独立圧力求積、凍結エントロピーと音速微分、密な流束走査、解析的チョーク排出の細分化を比較します。保存、実運動、再生、分岐、取消、ロールバック、割り当て、asset と MCP は別々に検証します。

[VALIDATION.ja.md](VALIDATION.ja.md)

## 適用範囲と次の作業

パラメータは `unverified` です。理想蒸気と熱膨張ゼロの圧縮性液体からなる純疑似流体です。理想噴流温度を含む指定温圧範囲を守ってください。凍結通過は過熱液体を保持できますが、実測の核生成や準安定限界は予測しません。有限速度緩和、相間滑り、解像した噴霧、不凝縮キャリアガス、燃焼受入室、実測マップ、実 Unity 受入検証は残っています。保存と再生は機器の妥当性を証明しません。

有限速度と整合する境界結合を拡張し、材料の緩和実測値を取得してから、解像された噴霧と燃焼受入室を追加します。点火、吸排気、機械損失、変速機作動、ECU/TCU 協調制御を続けます。実測校正と実 Unity 検収は別々に進めます。

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
