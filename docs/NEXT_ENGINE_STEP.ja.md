# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

燃料供給には有限タンク、保存的戻り、形状ヘッドスペースと明示的通気があります。ポンプ・レール・気体は内部仕事を交換し、液体供給、蒸発、規定反応は分離されています。現在の契約と検証証拠から再開します。

[タンク形状と有限ヘッドスペース](TANK_HEADSPACE.ja.md)

## 次の開発

次の燃料開発は、明示的物性による圧力依存相平衡とキャビテーションに集中します。欠落 OEM 測定は欠落のまま、研究パラメーターは未検証のまま維持します。実測ポンプ/弁充填・調圧、磁気/電子駆動と分解噴霧は今後の課題です。

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

剛体混合タンク、非圧縮液体と理想気体です。スロッシング/静水圧形状、相平衡、キャビテーション、実測ポンプ/弁マップ、OEM 較正と実際の Unity Editor/Play/Player/IL2CPP は未完成です。パラメーターは `unverified` です。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)
