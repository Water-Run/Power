# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

タンクは補正化学ポテンシャルと分離液体吸入による有限容積蒸気移動を実装しました。

[有限体積燃料タンクの相平衡](TANK_PHASE_EQUILIBRIUM.ja.md)

## 次の開発

次に明示物性と体積・運動量・仕事の保存連成で二相輸送と局所入口損失を解きます。欠落 OEM 測定は補いません。点火、吸排気、機械損失、ECU/TCU は別に進めます。

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

ポンプ・伝熱・通気連成例は一次収束を示します。供給の両相は物性温度区間内、総密度は binary64 丸め余裕を含め液体未満に保ちます。一定密度/共通気体物性、瞬時平衡、平面界面は研究近似です。実混合物、表面張力、スロッシング、有限速度、実測較正と実際の Unity Editor/Play/Player/IL2CPP は未検証です。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)
