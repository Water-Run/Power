# ポンプ供給の液体燃料レール

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · **日本語** · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## 契約

`liquid_rail_feed` は液体噴射器を既存の容積ポンプと明示的な物質/熱境界に結びます。油圧出口ノードはレールのコンプライアンスと初期絶対圧に一致する必要があります。ポンプと噴射器がこの圧力ノードを所有し、未追跡の他の流体経路は拒否されます。

圧力エネルギーは油圧ノードに一度だけ保存されます。流量と軸反力は保存則を満たす連成解に従います。流入燃料は熱と化学境界エネルギーを持ち込み、レールの熱貯蔵が温度を混合します。符号付き逆流は現在のレール温度で燃料を戻します。ノズル排出、壁加熱、蒸気供給、規定燃焼は別々に扱います。

`pump-fed-liquid-cylinder` と `pump-fed-needle-cylinder` は物理噴射と任意のニードル運動を保持します。圧力 KPI は明示的単位で宣言したポンプのみの上限を使います。補給 ID 1511 の `total_fuel_delivered`、`reservoir_enthalpy`、`fuel_energy_in` を読みます。ポンプ ID 1510 は実際の軸から流体への仕事を示します。

## 証拠と制限

v26 は補給接続と源温度を保存し v1-v25 を読みます。軸/圧力の解析交換、独立した同時 ODE 細化、熱混合、質量/燃料/エネルギー/体積台帳、逆流と完全 rollback は別々に検査します。

源は明示的な外部境界であり、有限燃料タンクのモデルではありません。タンク枯渇、ポンプ効率/調圧、配管損失、キャビテーション、圧力依存物性、有限体積噴霧は未完成です。パラメーターは `unverified` であり、OEM 校正や実際の Unity Editor/Play/Player/IL2CPP 受入を証明しません。
