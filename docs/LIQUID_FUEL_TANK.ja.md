# 有限液体燃料タンク

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · **日本語** · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## 契約

`liquid_fuel_tank` は対応する噴射器の密度、フィルム熱基準、発熱量を使い、有限液体質量と熱エネルギーを保存します。補給は `tank_component` で選び、`supply_temperature` を省略します。各タンクは燃料物性が一致する一つの補給に属します。

正のポンプ流量は受理した区間での残存量に制限されます。同じ有効充液排量が軸反力と圧力移送を決め、軸/流体仕事を保存します。空タンクでの前進回転は液体や流体仕事を供給せず、符号付き戻り流れは現在のレール熱エネルギーをタンクに混合します。

タンクの熱/化学エネルギーは全貯蔵台帳に入ります。内部移送は外部質量や化学供給を追加しません。明示したポンプ入口規定圧は圧力仕事境界を保持します。ガス吸排気は化学境界エネルギーを運ぶ場合があります。

`finite-tank-liquid-cylinder` と `finite-tank-needle-cylinder` はタンク ID 1513 と補給 ID 1511 を使います。`mass`、`temperature`、`internal_energy`、`chemical_energy`、`tank_state` を読み、0 は液あり、1 は空です。乾燥温度は宣言した初期基準を示します。

## タンク形状と有限ヘッドスペース

`liquid_fuel_tank.parameters.headspace` は `m3` または `l` の `capacity` と `gas_node` を指定します。気体ノードは `storage` を省略し、体積は `capacity - liquid_mass / density` です。所有者は一つで体積は正を維持します。有限気体が入口圧力を決めるため、ポンプと戻りの指定リザーバー圧力はゼロです。

[タンク形状と有限ヘッドスペース](TANK_HEADSPACE.ja.md)

## 証拠と制限

v29 はタンクと補給選択を保存し v1-v28 を読みます。各タンクは既存上限内で 4 状態を追加します。独立湿潤交換、枯渇後の圧力/軸エネルギー解析解、逆流混合、全台帳、rollback、分岐、割り当てなしステップを検査します。

剛体混合タンク、非圧縮液体と理想気体です。スロッシング/静水圧形状、相平衡、キャビテーション、実測ポンプ/弁マップ、OEM 較正と実際の Unity Editor/Play/Player/IL2CPP は未完成です。パラメーターは `unverified` です。

[VALIDATION.ja.md](VALIDATION.ja.md)

## 追跡可能な燃料リリーフ戻り流れ

`liquid_rail_return` は補給を排他的な一方向 `hydraulic_relief` に結びます。弁はレールをポンプと同じ規定入口圧へ接続します。すべての流体経路を登録し、不適合ポート、重複所有、未追跡経路は拒否します。

`fluid_heat_fraction` は戻り燃料が運ぶ弁損失の割合 [0,1] を明示します。残る熱は宣言した弁熱経路へ流れます。レール/タンク同時熱混合は質量、化学、圧力仕事、熱台帳を保存し、外部源への戻りは境界から質量/エネルギーを出します。

[LIQUID_FUEL_RETURN.ja.md](LIQUID_FUEL_RETURN.ja.md)
