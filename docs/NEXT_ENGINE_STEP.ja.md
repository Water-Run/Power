# エンジン開発再開ノート

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · **日本語** · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 再開位置

有限燃料室は軸と圧力仕事を交換し、理想一方向抵抗を通じ充填・供給します。入口不足時のバルク相変化も扱います。物理噴射器・受容側結合は残ります。

[軸駆動の相変化燃料ポンプ](FUEL_CHAMBER_PUMP.ja.md)

## 次の開発

燃料ドメインポンプを物理噴射器とシリンダー・膜受容側へ接続し、二相輸送、排除体積、運動仕事を明示します。局所入口慣性とチョーク流を独立参照で解決します。点火、吸排気、機械損失、変速機作動、ECU/TCU を完全な目標へ進めます。

## 受け入れ

新方程式に適した独立解析/極限参照、完全な質量/エネルギー収支と刻み幅収束を求めます。全バッチのロールバック、取消、分岐、安定チャンネル、移植再生と旧リーダーを維持します。その後、点火、吸排気、機械損失、変速機駆動と ECU/TCU 協調を進めます。

パラメーターは `unverified` です。温度、圧力、分解可能な体積限界は全サイクルで適用します。非圧縮液体、理想擬似成分蒸気、瞬時共通温度平衡、外部ケース圧力ゼロを仮定します。局所入口慣性/NPSH、チョーク流、核生成遅延、弁慣性・ヒステリシス、実混合燃料、実測校正は残ります。物理噴射器接続と受容側の排除体積・運動仕事も別要件です。

[DEVELOPMENT_STATUS.ja.md](DEVELOPMENT_STATUS.ja.md) · [ROADMAP.ja.md](ROADMAP.ja.md) · [VALIDATION.ja.md](VALIDATION.ja.md)
