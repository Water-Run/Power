# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

燃料域ポンプは物理ニードルと凍結蒸気質量分率の流出を介して、有限液体体積の燃焼受容室へ供給します。全在庫に相変化、排除仕事、噴流運動エネルギー、静止壁力積を含みます。

[軸駆動の相変化燃料ポンプ](FUEL_CHAMBER_PUMP.ja.md)

`LiquidColumn` は水平剛体管の独立 C# 参照です。断面積、均一速度、液体密度、温度を一定とし、各局所区間の絶対ポート圧力を指定します。線形・二次抵抗は明示した研究入力です。グラフコンポーネントではなく、燃料ネットワークやノズル方程式を変更しません。 [液体入口の運動量参照](LIQUID_COLUMN.ja.md)

`fuel_inertial_pipe` は二つの有限燃料ノードと有限熱壁を接続します。水平剛体管は両容器に加えて独自の液体質量、熱内部エネルギー、運動エネルギーを持ちます。密度一定で温度は完全混合により変化します。ポートと管路全体が液相である必要があり、既存の準静的リンクは別の二相挙動を保ちます。 [有限液体燃料管路の運動量](FUEL_INERTIAL_PIPE.ja.md)

`equilibrium_fuel_duct` は明示的な `compressible_fuel` セル間で質量、運動量、全エネルギーを輸送します。液体は圧縮性、蒸気は理想気体で、両相の圧力、温度、速度は共通です。局所平衡により凝縮とフラッシュ蒸発が生じます。有限の静止貯槽 2 個と有限熱壁が収支を閉じます。 [圧縮性平衡燃料ダクト](EQUILIBRIUM_FUEL_DUCT.ja.md)

## 次の開発

圧縮性ダクトを互換性のある軸駆動室とニードル／ノズル境界に接続し、圧力仕事と相の仮定を明示します。その後、相緩和／滑り、点火、吸排気、機械損失、ECU/TCU 協調制御を進めます。実測校正と実 Unity の受入検証は別に行います。 [圧縮性平衡燃料ダクト](EQUILIBRIUM_FUEL_DUCT.ja.md)

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

パラメータは `unverified` のままです。指定温度・圧力範囲、正の潜エンタルピー、分離した共存比体積、希薄蒸気の条件を守ってください。剛体・等断面・水平管で瞬時均質平衡を仮定します。有限速度の核生成、相間滑り、表面張力、重力、管弾性、実流体の臨界挙動は含みません。一次数値拡散は相変化と音響の前線をぼかします。保存と再生だけでは空間精度や実測機器への妥当性を証明できません。実 Unity の受入検証は未完了です。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)

[蒸気質量分率を凍結した燃料流出と有限受容室](FUEL_DISCHARGE.ja.md)
