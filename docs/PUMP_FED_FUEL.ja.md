# ポンプ供給の液体燃料レール

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · **日本語** · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## 契約

`liquid_rail_feed` は液体噴射器を既存の容積ポンプと明示的な物質/熱境界に結びます。油圧出口ノードはレールのコンプライアンスと初期絶対圧に一致する必要があります。ポンプと噴射器がこの圧力ノードを所有し、未追跡の他の流体経路は拒否されます。

圧力エネルギーは油圧ノードに一度だけ保存されます。流量と軸反力は保存則を満たす連成解に従います。流入燃料は熱と化学境界エネルギーを持ち込み、レールの熱貯蔵が温度を混合します。符号付き逆流は現在のレール温度で燃料を戻します。ノズル排出、壁加熱、蒸気供給、規定燃焼は別々に扱います。

`pump-fed-liquid-cylinder` と `pump-fed-needle-cylinder` は物理噴射と任意のニードル運動を保持します。圧力 KPI は明示的単位で宣言したポンプのみの上限を使います。補給 ID 1511 の `total_fuel_delivered`、`reservoir_enthalpy`、`fuel_energy_in` を読みます。ポンプ ID 1510 は実際の軸から流体への仕事を示します。

## 証拠と制限

v28 は補給接続と源温度を保存し v1-v27 を読みます。軸/圧力の解析交換、独立した同時 ODE 細化、熱混合、質量/燃料/エネルギー/体積台帳、逆流と完全 rollback は別々に検査します。

形状容量、通気/気相空間/揺動、キャビテーション、実測充液/効率/調圧、解像噴霧は未完了です。パラメーターは `unverified` で、実際の Unity Editor/Play/Player/IL2CPP と OEM 校正は未検証です。

## 有限液体燃料タンク

`liquid_fuel_tank` は対応する噴射器の密度、フィルム熱基準、発熱量を使い、有限液体質量と熱エネルギーを保存します。補給は `tank_component` で選び、`supply_temperature` を省略します。各タンクは燃料物性が一致する一つの補給に属します。

タンクの熱/化学エネルギーは全貯蔵台帳に入ります。内部移送は外部質量や化学供給を追加しません。明示したポンプ入口規定圧は圧力仕事境界を保持します。ガス吸排気は化学境界エネルギーを運ぶ場合があります。

形状容量、通気/気相空間/揺動、キャビテーション、実測充液/効率/調圧、解像噴霧は未完了です。パラメーターは `unverified` で、実際の Unity Editor/Play/Player/IL2CPP と OEM 校正は未検証です。

[LIQUID_FUEL_TANK.ja.md](LIQUID_FUEL_TANK.ja.md)

## 追跡可能な燃料リリーフ戻り流れ

`liquid_rail_return` は補給を排他的な一方向 `hydraulic_relief` に結びます。弁はレールをポンプと同じ規定入口圧へ接続します。すべての流体経路を登録し、不適合ポート、重複所有、未追跡経路は拒否します。

`fluid_heat_fraction` は戻り燃料が運ぶ弁損失の割合 [0,1] を明示します。残る熱は宣言した弁熱経路へ流れます。レール/タンク同時熱混合は質量、化学、圧力仕事、熱台帳を保存し、外部源への戻りは境界から質量/エネルギーを出します。

[LIQUID_FUEL_RETURN.ja.md](LIQUID_FUEL_RETURN.ja.md)
