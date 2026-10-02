# 分解された Ravigneaux 遊星運動

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · **日本語** · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

分解アセンブリは、両方の内部遊星セットの絶対自転と、キャリアまわりの軌道質量慣性を含みます。4 つの物理的な噛み合い拘束が 6 つのローターを結びます。5 つのレンジクラッチ/ブレーキと外部コンバーターは、通常のコンポーネントのままです。4 部材の縮約は、別の宣言された簡略化として引き続き使えます。遊星自転のエビデンスは供給しません。

噛み合いの接続とピッチ関係には、別の[構造上の参考文献](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html)があります。Power! は独自の保存的なローターグラフと、独立した質量行列の検査を導き、実装します。ベンダーの実装やモデルパッケージは含まれません。

## 幾何とエネルギー

リングのピッチ半径 `R`、大/小サンの比 `kL` と `kS` について、剛体のピッチ幾何は次のとおりです。

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` は、SI のリング半径、正の遊星ごとの質量と自転慣性、1..32 の同期した等しい遊星の対を要求します。等間隔は、ピッチ円が重ならずに両方のセットが収まる必要があります。幾何と集約慣性は表現可能なままでなければなりません。これらの入力はすべて明示的な研究特性です。補助は実測値を供給しません。

`n` 組の等しい対について、キャリアは軌道慣性 `n (mInner orbitInner^2 + mOuter orbitOuter^2)` を受け取ります。既存の `CarrierInertia` パラメーターは、この分解経路ではキャリア構造の慣性です。新しい各ローターは、遊星ごとの自転慣性の `n` 倍を持ちます。それらの速度は絶対角速度なので、運動エネルギーは通常の `J omega^2/2` です。共回転は遊星の自転エネルギーを保ちます。この対角ストレージで相対自転を使うと、キャリアとの連成が省かれます。

## 噛み合い契約

`carrier_gear` は `A - ratio B + (ratio-1) C = 0` を課します。C は実際に動くキャリアです。外部噛み合いは負のピッチ半径比を使い、リング/外側遊星の内歯噛み合いは正の比を使います。1 を含む、有限で非零の符号付き比がサポートされます。3 つの別々の回転ポートと、適合する初期速度が必要です。

4 つの噛み合いは、大サン/外側遊星、小サン/内側遊星、リング/外側遊星、内側遊星/外側遊星です。3 つの反力トルクはすべて同じ中点射影に入り、合計したポート動力は零、トルクの和も零です。キャリア反力は、黙って静止した接地へ送られません。有界な相対残差の改良が、小さな力の応答を改善します。中点求解は次の端点の速度残差を零にし、先行する丸めの繰り返し反射を避けます。両方の操作は実際の拘束力の応答を使い、補正乗数を実際の反力履歴に残します。作業領域は各シミュレーションに属します。コンパイル済み因子は不変のままです。正規化された行、補償座標、完全な反力履歴は、位相、分岐、キャンセル、バッチロールバックを保ちます。

独立した自由参照は、リング/キャリア座標を使います。`aOuter = R/outerRadius`、`aInner = R/innerRadius` として:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

これには両方の自転エネルギーと、別に加えられた軌道慣性が含まれます。独立した一般化荷重、すべての前進/リバース経路の換算慣性、角運動量、キャリア捕捉の力積/熱が、組み立てられた求解を検査します。

## 共有グラフとエビデンス

`CreateResolvedGraph` は、元のポート、4 つの別々の遊星ノード/噛み合い ID、宣言された遊星特性を取ります。不変の通常定義を返します。6 つの内部ローター、4 つのキャリア噛み合い、ファイナルドライブ、5 つの摩擦要素です。フラットな JSON は合計ローター慣性と符号付き噛み合い比を保ちます。例の説明は、生成に使った幾何と遊星ごとの特性を記録します。ソースダイジェストは、その宣言されたオーサリングのエビデンスを保ちます。

`resolved-ravigneaux-transmission` は、すべての前進の昇/降の引き継ぎを行使します。`fired-resolved-ravigneaux-converter` は、エンジン、符号付きコンバーターマップ、ロックアップを加えます。どちらも 3 対、R=0.1 m、内側/外側の質量 0.3/1 kg、遊星ごとの自転慣性 0.000015/0.0005 kg m2 を宣言します。キャリア構造は 0.03 kg m2 です。明示的な軌道の加算は 0.0184375 kg m2 です。これらは研究入力です。

移植可能アセット v24 は符号付きキャリア噛み合いを保持し、より前のバージョンを読みます。この原始素はフィンガープリントタグ 28 を加えます。以前のグラフはフィンガープリントとリプレイを保ちます。用意された Studio のマーカーは、3 つの噛み合いポートをすべて識別します。実際の Unity エディター/Play/Player/IL2CPP の検証は別です。必要な逐次の `dotnet run --file tools/Build.cs -- verify` を実行してください。数値的な結果と範囲は [VALIDATION.ja.md](VALIDATION.ja.md) にあります。

## 残る挙動

同期した剛体の等しい遊星セットは、歯のコンプライアンス、製造上の荷重分担、クリアランス、噛み合い損失、潤滑、温度依存の物性をモデル化しません。[ポンプ供給の油圧ピストン駆動](AT_HYDRAULIC_ACTUATION.ja.md)は利用できます。完全な変速制御、ECU 協調、実測の OEM 幾何/マップは未完了です。汎用アセンブリは PSA AT8/AL4 との同一性を証明しません。サンプル境界と欠けている計測はそのままです。
