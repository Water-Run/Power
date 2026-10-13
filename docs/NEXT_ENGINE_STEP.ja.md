# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

受け側は可逆燃料相平衡と液体供給の飽和検査を実装済みです。

[燃料の相平衡と液体供給の境界](FUEL_PHASE_EQUILIBRIUM.ja.md)

## 次の開発

次に明示物性と保存的体積/仕事連成によるタンク蒸気移動、二相ポンプ/レール流れを解きます。欠落 OEM 測定は補いません。点火、吸排気、機械損失、ECU/TCU 協調は別に進めます。

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

パラメーターは `unverified` です。実測ガソリン物性、多成分または有限速度の相変化、分解液体押しのけ/噴霧、OEM 較正は提供しません。実際の Unity Editor/Play/Player/IL2CPP 受け入れは未完了です。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)
