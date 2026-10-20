# 相平衡燃料輸送ネットワーク

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · **日本語** · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## モデル範囲

`fuel` ドメインは有限弾性容器内で純燃料の液体と蒸気を共に輸送します。各容器は総質量と熱量・弾性の合計エネルギーを独立に保持し、相平衡フラッシュが温度、圧力、蒸気割合を決定します。`fuel_restriction` は均質な供給側流体を交換し、`fuel_heat_link` は有限熱ノードと熱交換します。発熱量に対応する化学エネルギーは別台帳です。

燃料ノードの `storage` は `m3` または `l` 単位の基準体積、`initial` は `k` 単位の温度、`position` は `kg` 単位の総質量です。`fuel` オブジェクトに物性、コンプライアンス、圧力・温度限界を明示します。接続容器は密度、熱量・相基準、温度限界、発熱量を共有します。流動抵抗の両端は有限燃料ノードで、熱リンクは燃料と熱ノードを結びます。開度は [0,1] で入力チャネルは任意です。

## 保存的な流れ

抵抗は `m3_s_pa` 単位の明示したコンダクタンス K による準静的散逸基準体積則です。リンク慣性、運動エネルギー蓄積、相間滑りを無視します。供給側密度で基準体積流を総質量流へ変換し、供給側エンタルピーが両相を運びます。追加の外部圧力仕事源や圧力損失の別熱項はありません。弾性仕事は容器内に蓄積済みです。

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## 時間積分とエントロピー

有界後退 Euler が両容器に同じ採用質量 q とエンタルピー H を求めます。リンクは安定したコンポーネント ID 順に進み、有限熱浴は別の保存的部分ステップを使います。結合は一次精度です。質量・温度・圧力範囲外や求解失敗は入力、補償履歴、クラッチ試行区間を含むバッチ全体を拒否します。

整合した物性と正の圧力駆動コンダクタンスは連続系の対エントロピー生成を非負にします。後退 Euler は数値エントロピーを加えます。`entropy_produced` は混合と時間離散化を含む採用済み対エントロピー変化で、実測損失マップではありません。保存と細分化に加えて独立したエントロピー検証を行います。

## 使用と証拠

MCP で `equilibrium-fuel-network` または `electrothermal-fuel-network` を要求するか、同じ JSON を CLI で実行します。各容器の `mass`、`internal_energy`、`elastic_energy`、`vapor_mass`、`vapor_mass_fraction`、`vapor_volume_fraction`、`entropy`、`specific_enthalpy` を確認します。`internal_energy` は弾性壁を含み、化学エネルギーは別です。リンクは符号付き流量、累積質量、`transport_energy`、対エントロピー変化を公開します。

実験は共存への液体排出、逆流、開度変更、有限壁と機電系の熱結合を扱います。独立した液体圧力指数解、同時二相質量・エンタルピー・熱 ODE、熱駆動逆流、クラッチ試行捕捉で精度と組合せを検証します。資産と実 MCP は全サンプル値とハッシュを保持します。資産 v33 は物性を保持し v1-v32 を読みます。実 Unity Editor/Play/Player/IL2CPP は未検証です。

## 残る作業

燃料輸送は軸駆動 `fuel_chamber` と理想 `fuel_check_valve` に接続できます。物理噴射器、局所慣性・チョーク流、実測混合物は別作業です。パラメーターは `unverified` です。 [軸駆動の相変化燃料ポンプ](FUEL_CHAMBER_PUMP.ja.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.ja.md) · [VALIDATION.md](VALIDATION.ja.md)
