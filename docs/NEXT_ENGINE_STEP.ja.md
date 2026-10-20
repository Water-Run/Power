# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

燃料域ポンプは物理ニードルと凍結蒸気質量分率の流出を介して、有限液体体積の燃焼受容室へ供給します。全在庫に相変化、排除仕事、噴流運動エネルギー、静止壁力積を含みます。

[軸駆動の相変化燃料ポンプ](FUEL_CHAMBER_PUMP.ja.md)

`LiquidColumn` は水平剛体管の独立 C# 参照です。断面積、均一速度、液体密度、温度を一定とし、各局所区間の絶対ポート圧力を指定します。線形・二次抵抗は明示した研究入力です。グラフコンポーネントではなく、燃料ネットワークやノズル方程式を変更しません。 [液体入口の運動量参照](LIQUID_COLUMN.ja.md)

## 次の開発

明示した管路エネルギー在庫と整合するポートエンタルピーで、液体運動量参照を有限燃料ネットワークへ結合します。キャビテーション入口を主張する前に変密度・相変化の有効性を確立します。相緩和、点火、吸排気、損失、ECU/TCU 協調を続けます。 [液体入口の運動量参照](LIQUID_COLUMN.ja.md)

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

パラメーターは `unverified` です。宣言温度、正の気体体積、希薄蒸気領域を守ります。入口局所慣性、有限速度核生成、相間滑り、解像噴霧、実流体臨界挙動、実測校正を含みません。規定燃焼は化学平衡や反応エントロピーの精度を示しません。入口動特性、点火、吸排気、損失、ECU/TCU 協調へ進みます。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)

[蒸気質量分率を凍結した燃料流出と有限受容室](FUEL_DISCHARGE.ja.md)
