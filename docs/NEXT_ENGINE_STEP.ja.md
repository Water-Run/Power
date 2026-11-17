# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

燃料域ポンプは物理ニードルと凍結蒸気質量分率の流出を介して、有限液体体積の燃焼受容室へ供給します。全在庫に相変化、排除仕事、噴流運動エネルギー、静止壁力積を含みます。

[軸駆動の相変化燃料ポンプ](FUEL_CHAMBER_PUMP.ja.md)

`LiquidColumn` は水平剛体管の独立 C# 参照です。断面積、均一速度、液体密度、温度を一定とし、各局所区間の絶対ポート圧力を指定します。線形・二次抵抗は明示した研究入力です。グラフコンポーネントではなく、燃料ネットワークやノズル方程式を変更しません。 [液体入口の運動量参照](LIQUID_COLUMN.ja.md)

`fuel_inertial_pipe` は二つの有限燃料ノードと有限熱壁を接続します。水平剛体管は両容器に加えて独自の液体質量、熱内部エネルギー、運動エネルギーを持ちます。密度一定で温度は完全混合により変化します。ポートと管路全体が液相である必要があり、既存の準静的リンクは別の二相挙動を保ちます。 [有限液体燃料管路の運動量](FUEL_INERTIAL_PIPE.ja.md)

`equilibrium_fuel_duct` は明示的な `compressible_fuel` セル間で質量、運動量、全エネルギーを輸送します。液体は圧縮性、蒸気は理想気体で、両相の圧力、温度、速度は共通です。局所平衡により凝縮とフラッシュ蒸発が生じます。有限の静止貯槽 2 個と有限熱壁が収支を閉じます。 [圧縮性平衡燃料ダクト](EQUILIBRIUM_FUEL_DUCT.ja.md)

`compressible_fuel_chamber` は回転軸と有限の `compressible_fuel` 貯槽を結びます。軸の運動で容積が変わり、同じ圧力仕事が流体エネルギーと軸トルクを変えます。貯槽は `equilibrium_fuel_duct` と燃料を交換し、平衡フラッシュ蒸発や凝縮を扱えます。 [軸駆動の圧縮性燃料室](COMPRESSIBLE_FUEL_CHAMBER.ja.md)

`compressible_fuel_nozzle` は軸駆動室を含む有限の `compressible_fuel` 貯槽間に一方向の流れを与えます。液体圧縮エネルギーと理想蒸気は貯槽と同じ物性を使います。`phase_mode` を `frozen` と明示し、通過中の蒸気質量分率を一定に保ち、貯槽は再平衡させます。 [相分率を凍結した圧縮性燃料ノズル](COMPRESSIBLE_FUEL_NOZZLE.ja.md)

`compressible_fuel_nozzle` の `phase_mode` を `equilibrium` にすると、通過中に瞬時の均質相平衡を仮定します。液体と蒸気は圧力、温度、速度を共有し、等エントロピー線に沿って蒸気分率が変化します。有限貯槽とノズルは同じ圧縮性液体ポテンシャルと理想蒸気基準を使います。`frozen` も独立した仮定として引き続き使えます。 [燃料ノズルの平衡相変化輸送](EQUILIBRIUM_FUEL_NOZZLE.ja.md)

`relaxing_fuel` ノードは独立した蒸気質量分率を持ちます。液体と蒸気は圧力、温度、速度を共有しますが、化学平衡には明示した有限時間を要します。`relaxing_fuel_duct` は明示セルで質量、運動量、全エネルギー、蒸気質量を輸送します。滞留時間はセル形状と実際の流速から決まります。 [有限速度の燃料相変化緩和](RELAXING_FUEL.ja.md)

`compressible_fuel_chamber` は `relaxing_fuel` 貯槽の体積を制御できます。可逆な軸仕事では蒸気質量分率を固定し、化学緩和はエネルギー保存の独立源項として扱います。`compressible_fuel_nozzle` は `phase_mode` が `frozen` のとき二つの緩和貯槽を結び、全質量、蒸気質量、よどみエンタルピーを輸送します。実際のソレノイド、ばね、ストッパーを備えた物理針弁で開度を計量できます。 [相変化緩和燃料ポンプと凍結相計量](RELAXING_FUEL_BOUNDARIES.ja.md)

## 次の開発

材料証拠に基づく有限速度のノズル内滞留を解像し、続いて噴霧と整合する燃焼受入室を加えます。点火、吸排気、機械損失、変速機作動、ECU/TCU 協調制御を進めます。実測校正と実 Unity 検収は別々に扱います。 [圧縮性平衡燃料ダクト](EQUILIBRIUM_FUEL_DUCT.ja.md)

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

パラメータは `unverified` です。両ポートの領域と材料は同一とし、緩和ポートでは平衡通過を拒否します。貯槽の有限緩和は、ゼロ容積ノズル内部の化学変化や滞留時間を解像しません。相間滑り、液体負圧張力、核生成障壁則、噴霧、搬送ガス、燃焼受入室は含みません。移動貯槽は一様混合で平均速度ゼロ、管路内セルは剛体で所有者は一つです。臨界挙動から離れた希薄理想蒸気、宣言された正圧・温度範囲、正の潜熱エンタルピー、分離した共存比体積が必要です。初期蒸気分率は明示的な研究入力です。数値の一致は実測校正や実 Unity 検収を証明しません。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)

[蒸気質量分率を凍結した燃料流出と有限受容室](FUEL_DISCHARGE.ja.md)
