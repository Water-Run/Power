# 燃料の相平衡と液体供給の境界

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · **日本語** · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## 契約

任意の `fuel_film.parameters.phase_equilibrium` は `pa` または `bar` の `reference_pressure` と、`k` の `minimum_temperature` / `maximum_temperature` を指定します。既存の `saturation_temperature` は基準温度、`latent_internal_energy` はそこでの蒸気と液体の比内部エネルギー差です。平衡受け側は一つの膜が所有します。初期スナップショットを保持し、受理した区間で気液平衡と有限壁の熱交換を計算します。

`mass_flow` は完全な物理刻みの符号付き正味相移動です。`evaporated_fuel_mass` と `condensed_fuel_mass` は正負の質量を別々に累積し、相殺する分割演算を二重計上しません。`vapor_partial_pressure` は受け側の燃料分圧です。熱・質量・化学エネルギー収支は両相を含みます。

## 熱力学的基準

一つの理想擬似成分、受け側の共通気体定数、一定比熱を用い、液体押しのけ容積を無視します。相の化学ポテンシャルの一致から下の飽和関係を得ます。固定体積では総燃料と内部エネルギーから共通温度と蒸気質量が決まります。化学エネルギーは内部で移動し、反応は蒸気のみを消費します。

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## 供給と数値計算の制限

有界スカラー解法は単調な平衡エネルギーを使います。有限壁の伝熱は後退 Euler です。輸送と移動シリンダー仕事との連成には一次収束の証拠があります。液体共存は指定温度区間内に限り、その区間で完全蒸発した気体は過熱できます。飽和曲線は外挿しません。

既定の液体供給は飽和未満の湿潤状態を拒否します。任意の有限タンク平衡は補正化学ポテンシャルで供給蒸気移動を解きます。レールと吸入口は分離液体のままで、気泡巻込みとキャビテーション輸送には追加物理が必要です。

圧力比較は binary64 の丸め幅 `64 * epsilon * max(p, p_sat)` のみを許容し、`epsilon = 2^-52` です。表示余裕はクランプしません。これはバルクの飽和検査で、局所圧力損失とポンプ NPSH には流れを解くモデルが必要です。独立検査では断熱フラッシュと有限壁熱交換の総エントロピーが減少しないことも求めます。

## 証拠

`equilibrium-fuel-cell` は凝縮、通気による蒸発、再凝縮を示します。`equilibrium-injected-cylinder` は通気タンクエンジンに可逆相平衡と供給余裕を加えます。独立したエントロピー最大化、解析的極限、連立 ODE 収束、完全再生、ロールバックと割当を検証します。アセット v33 は物性を保持し v1-v32 を読みます。

パラメーターは `unverified` です。実測ガソリン物性、多成分または有限速度の相変化、分解液体押しのけ/噴霧、OEM 較正は提供しません。実際の Unity Editor/Play/Player/IL2CPP 受け入れは未完了です。

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.ja.md](VALIDATION.ja.md)

## 有限体積燃料タンクの相平衡

`liquid_fuel_tank.parameters.headspace.phase_equilibrium` を `true` にするとタンク内の蒸発と凝縮を解きます。対応する膜が `phase_equilibrium` 物性を与え、追跡ヘッドスペースは R、cv、発熱量と化学量論比を共有します。湿潤タンクは共通初期温度と飽和燃料分圧を、乾燥タンクは飽和以下の分圧を宣言します。総密度は液体密度未満です。

タンクの `evaporated_fuel_mass`、`condensed_fuel_mass` と符号付き `mass_flow` は完全刻みの正味相移動を累積します。`vapor_partial_pressure`、補正 `saturation_pressure`、液量と気体容積を併読します。ポンプは分離液体だけを吸い、二相ポンプ/レール輸送、気泡巻込みと局所 NPSH は未完成です。

[有限体積燃料タンクの相平衡](TANK_PHASE_EQUILIBRIUM.ja.md)
