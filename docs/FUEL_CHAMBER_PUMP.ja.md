# 軸駆動の相変化燃料ポンプ

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · **日本語** · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## 物理契約

`fuel_chamber` は実際の軸と有限燃料室を結合します。入口・出口の `fuel_check_valve` が圧力差で充填と供給を行います。液体、蒸気、弾性壁エネルギーは同じ閉包を使います。入口不足では吸入中にフラッシュ蒸発でき、供給量は計算した在庫と圧力に従います。

`node_a` は回転軸、`node_b` は幾何所有者が一つの燃料ノードです。`minimum_reference_volume`、`swept_volume` を `m3` または `l`、`phase` を `rad` または `deg` で宣言します。初期体積は binary64 丸め内で初期軸角の幾何と一致します。正弦機構にはピストン・カム慣性を追加せず、軸慣性は明示します。

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## 仕事とポート

閉じた機械部分ステップは質量とエントロピーを保ちます。流体と弾性壁を含む蓄積エネルギーのため、軸仕事には幾何基準体積変化を使います。同じ採用エネルギー変化が歯車、コンバーター、油圧、クラッチ試行を含む連成解のトルクを決めます。膨張は軸を押し、正の `hydraulic_work` は軸から室への仕事です。外部圧力源は追加しません。

`fuel_check_valve` は `m3_s_pa` 単位の明示したコンダクタンスと任意の割合開度を使います。A が高圧のときだけ均質供給流体を A から B へ通します。質量なし・開弁圧差ゼロの理想逆止弁です。保存的エンタルピー輸送と有限壁熱交換は燃料ネットワーク契約に従います。

## 使用と証拠

MCP で `equilibrium-fuel-pump` または `electric-equilibrium-fuel-pump` を要求するか JSON を CLI で実行します。室の質量、温度、圧力、蒸気割合、エネルギーを弁供給と軸速度に並べて確認します。コンポーネント `volume` は幾何基準、ノード `volume` は壁膨張込みです。`torque`、`hydraulic_power`、`hydraulic_work` が内部軸交換を示します。

独立した液体圧力・仕事、理想蒸気断熱線、二相エントロピー反転で閉包を検証します。閉液体の軸運動は二次、相変化を伴う充填・排出は独立反転 RK4 参照に対して一次収束します。入口不足、十分な液体、電動、歯車・油圧・クラッチ組合せ、厳密再生、ロールバック、割当検証で統合を確認します。資産 v34 は幾何を保存し v1-v33 を読みます。実 Unity Editor/Play/Player/IL2CPP は未検証です。

## 残る作業

パラメーターは `unverified` です。温度、圧力、分解可能な体積限界は全サイクルで適用します。非圧縮液体、理想擬似成分蒸気、瞬時共通温度平衡、外部ケース圧力ゼロを仮定します。局所入口慣性/NPSH、チョーク流、核生成遅延、弁慣性・ヒステリシス、実混合燃料、実測校正は残ります。

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ja.md)

燃料域ポンプは物理ニードルと凍結蒸気質量分率の流出を介して、有限液体体積の燃焼受容室へ供給します。全在庫に相変化、排除仕事、噴流運動エネルギー、静止壁力積を含みます。 [蒸気質量分率を凍結した燃料流出と有限受容室](FUEL_DISCHARGE.ja.md)
