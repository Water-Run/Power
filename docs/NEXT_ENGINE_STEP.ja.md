# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

タンクは補正化学ポテンシャルと分離液体吸入による有限容積蒸気移動を実装しました。

[有限体積燃料タンクの相平衡](TANK_PHASE_EQUILIBRIUM.ja.md)

純燃料の弾性容器閉包は液体、蒸気、弾性蓄積を伴う共存を解決します。局所構成則参照であり、グラフのポンプとノズル輸送には接続が必要です。

[弾性燃料容積の参照モデル](COMPLIANT_FUEL_VOLUME.ja.md)

## 次の開発

この閉包を保存的な二相ポンプ・レール輸送に接続し、局所入口損失、混合物運動量、ノズル排出を解決します。明示した物性と完全な体積・仕事結合を保ちます。不足する OEM 測定値は補完しません。点火、吸排気、機械損失、ECU/TCU 連携は別途継続します。

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

ポンプ・伝熱・通気連成例は一次収束を示します。供給の両相は物性温度区間内、総密度は binary64 丸め余裕を含め液体未満に保ちます。一定密度/共通気体物性、瞬時平衡、平面界面は研究近似です。実混合物、表面張力、スロッシング、有限速度、実測較正と実際の Unity Editor/Play/Player/IL2CPP は未検証です。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)
