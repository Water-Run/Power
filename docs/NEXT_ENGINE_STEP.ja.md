# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

燃料ドメイン容器は保存的な散逸抵抗と有限壁熱リンクを通じ液体・蒸気混合物を交換します。ポンプ充填、軸結合、噴射器排出の接続が残ります。

[相平衡燃料輸送ネットワーク](EQUILIBRIUM_FUEL_NETWORK.ja.md)

## 次の開発

燃料ドメイン容器を軸駆動ポンプ充填と物理噴射器排出へ接続し、物質・運動量・仕事を保存的に結合します。局所入口慣性とチョーク流を独立参照で解決します。これを完成したパワートレインとせず、点火、吸排気、損失、変速機作動、ECU/TCU 連携を継続します。

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

構成則の物性・分解可能な壁変形限界が引き続き適用されます。パラメーターは `unverified` です。ポンプ充填と軸仕事、噴射器接続、局所慣性、チョーク流、核生成遅延、相間滑り、実混合物測定は別途必要です。既存のポンプ供給噴射レールは接続実装まで分離液体経路を使います。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)
