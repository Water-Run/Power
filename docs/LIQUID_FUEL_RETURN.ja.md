# 追跡可能な燃料リリーフ戻り流れ

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · **日本語** · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## 契約

`liquid_rail_return` は補給を排他的な一方向 `hydraulic_relief` に結びます。弁はレールをポンプと同じ規定入口圧へ接続します。すべての流体経路を登録し、不適合ポート、重複所有、未追跡経路は拒否します。

`fluid_heat_fraction` は戻り燃料が運ぶ弁損失の割合 [0,1] を明示します。残る熱は宣言した弁熱経路へ流れます。レール/タンク同時熱混合は質量、化学、圧力仕事、熱台帳を保存し、外部源への戻りは境界から質量/エネルギーを出します。

戻り ID 1515 の `total_fuel_delivered`、`reservoir_enthalpy`、`fuel_energy_in`、`fluid_heat`、`mass_flow` を読みます。補給 ID 1511 はポンプ総移送を示します。総循環は初期在庫を超えられ、現在在庫は初期在庫からポンプ移送を引き戻りを足した量です。

## 証拠と制限

`recirculating-liquid-cylinder` と `recirculating-needle-cylinder` は有限燃料、実噴射、蒸発、任意のニードル動特性を保持します。v28 は接続/熱割合を保存し v1-v27 を読みます。各戻りは既存上限内で 8 状態を追加します。

独立リリーフ減衰/仕事、機械/圧力/熱同時細化、熱割合、複数経路、外部境界、replay、rollback、割り当て検査は合格します。沸騰や未解像移送区間は全バッチを失敗させます。タンク形状/通気、実測弁/ポンプ、キャビテーション、噴霧、OEM 校正、実 Unity Editor/Play/Player/IL2CPP は未完了です。

[VALIDATION.ja.md](VALIDATION.ja.md)
