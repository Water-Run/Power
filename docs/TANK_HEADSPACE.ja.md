# タンク形状と有限ヘッドスペース

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · **日本語** · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## 契約

`liquid_fuel_tank.parameters.headspace` は `m3` または `l` の `capacity` と `gas_node` を指定します。気体ノードは `storage` を省略し、体積は `capacity - liquid_mass / density` です。所有者は一つで体積は正を維持します。有限気体が入口圧力を決めるため、ポンプと戻りの指定リザーバー圧力はゼロです。

連成計算は外部圧力源を加えず、軸・レール・気体間で圧力仕事を交換します。気体オリフィスと熱接続が明示的な通気/伝熱を提供します。`pressure`、`fill_fraction`、符号付き累積 `hydraulic_work` と気体質量・エネルギー・体積を読みます。投与、液体供給、蒸発、燃焼は分離されます。

## 証拠と制限

`vented-tank-liquid-cylinder` と `vented-tank-needle-cylinder` はタンク 1513、気体 1520、通気入力 960 を使います。アセット v29 は形状を保存し v1-v28 を読みます。解析仕事/導関数、独立 ODE 収束、収支、portable/MCP 再生、ロールバックとゼロ割当ステップに合格しました。

剛体混合タンク、非圧縮液体と理想気体です。スロッシング/静水圧形状、相平衡、キャビテーション、実測ポンプ/弁マップ、OEM 較正と実際の Unity Editor/Play/Player/IL2CPP は未完成です。パラメーターは `unverified` です。

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ja.md)
