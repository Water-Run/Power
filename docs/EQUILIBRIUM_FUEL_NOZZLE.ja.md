# 燃料ノズルの平衡相変化輸送

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · **日本語** · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## 物理モデル

`compressible_fuel_nozzle` の `phase_mode` を `equilibrium` にすると、通過中に瞬時の均質相平衡を仮定します。液体と蒸気は圧力、温度、速度を共有し、等エントロピー線に沿って蒸気分率が変化します。有限貯槽とノズルは同じ圧縮性液体ポテンシャルと理想蒸気基準を使います。`frozen` も独立した仮定として引き続き使えます。

圧力と比エントロピーから安定な液体、蒸気、共存状態を求めます。化学ポテンシャル一致と既存の温圧範囲を使い、温度端点では binary64 の丸め近傍だけを許容します。面積、壁熱配分、開度、実ニードルは共通契約に従います。噴流は受入圧力まで理想完全膨張し、静止力積はニードル軸力や機械仕事を追加しません。

## 流束の大域的包絡

平衡音速の二乗が正なので、等エントロピー密度は圧力とともに増え、エンタルピー降下は減ります。この単調性から相境界や複数極値を含む各区間の流束上限を求めます。分枝限定法は標本下限と残存上限の相対幅を 1e-4 以下にします。最大 512 個のスタック区間と 8192 分割です。熱量差の丸め余裕と単調圧力積分の界を用い、最大 256 Riemann 区間、80 回の閉合反復を行います。解決できなければバッチを拒否します。滑らかな内部最大値は音速条件を満たし、相境界最大値は非平滑になり得ます。

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

保存的な一次サブステップが同じ質量とエンタルピーを移し、噴流運動エネルギーを流体と壁に配分します。物性状態と対全体のエントロピーを確認します。区間当たり最大 4096 ステップ、候補当たり 64 回の半減、初期移動量は小さい貯槽質量の 0.1 倍以下です。閉鎖判定には最大体積弾性率またはポート圧力の 128 binary64 epsilon 倍の丸め幅を許容し、出力圧力は切り詰めません。失敗はバッチ全体をロールバックします。刻み、在庫、物性範囲、行程、面積を確認してください。

## 利用と検証

`equilibrium-flashing-nozzle` または `equilibrium-pump-needle` を実行します。`flux_relative_uncertainty` は最後に採用した局所構成則の流束包絡で、平均流量、送出質量、一次時間積分、校正誤差の界ではありません。独立した飽和／エントロピー反転、気体・液体の解析的チョーク、別実装の密な大域探索、エネルギー／エントロピー恒等式、解析蒸気排出の細分化を比較します。在庫、軸／管／ニードル、ロールバック、取消、分岐、割り当て、可搬 asset、実 MCP は別々に検証します。数値と旧境界は検証証拠に残します。

[VALIDATION.ja.md](VALIDATION.ja.md)

## 適用範囲と次の作業

パラメータは `unverified` です。純疑似流体、理想蒸気、液体熱膨張ゼロ、共通速度を仮定します。有限の核生成・緩和時間、相間滑り、噴霧、キャリアガス、燃焼受入室は含みません。包絡は宣言した binary64 余裕を用い、方向付き丸めによる形式的区間証明ではありません。臨界遷移付近の喉部分類は流束包絡と併せて読みます。実測妥当性と実 Unity 受入検証は未完了です。

滞留時間と物性の明示的な証拠を伴う有限速度相緩和を追加し、その後に解像した噴霧と互換性のある燃焼受入室を接続します。点火、吸排気、機械損失、変速機駆動、ECU/TCU 協調を続けます。実測校正と実 Unity 検証は別に行います。

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
