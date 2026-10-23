# 有限体積燃料タンクの相平衡

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · **日本語** · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## 契約

`liquid_fuel_tank.parameters.headspace.phase_equilibrium` を `true` にするとタンク内の蒸発と凝縮を解きます。対応する膜が `phase_equilibrium` 物性を与え、追跡ヘッドスペースは R、cv、発熱量と化学量論比を共有します。湿潤タンクは共通初期温度と飽和燃料分圧を、乾燥タンクは飽和以下の分圧を宣言します。総密度は液体密度未満です。

タンクの `evaporated_fuel_mass`、`condensed_fuel_mass` と符号付き `mass_flow` は完全刻みの正味相移動を累積します。`vapor_partial_pressure`、補正 `saturation_pressure`、液量と気体容積を併読します。ポンプは分離液体だけを吸い、二相ポンプ/レール輸送、気泡巻込みと局所 NPSH は未完成です。

## 熱力学的基準

剛体容器は非圧縮液体と理想擬似成分・キャリア気体を含みます。液体容積が気体容積を変えます。液体化学ポテンシャルに `p/rho` を含めるため、受け側の無視できる押しのけの基準 `p_0(T)` に圧力補正が入ります。`reference_pressure` はその物性基準の意味を保ち、有限密度の純流体飽和圧へ暗黙に読み替えません。

[燃料の相平衡と液体供給の境界](FUEL_PHASE_EQUILIBRIUM.ja.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

有界な定 U,V フラッシュは総燃料と熱エネルギーを保存します。明示的な気体オリフィスは蒸気、キャリアと化学エネルギーを運び、熱接続は有限熱ノードと交換します。ポンプ/戻りは気体と押しのけ仕事を交換し、相平衡が内部エネルギーを再配分します。`hydraulic_work` は内部相変化仕事を除くポンプ押しのけ仕事です。

## 証拠と制限

`equilibrium-tank-supply` は逆転、燃料を含む通気と有限加熱壁を検査します。`equilibrium-tank-fired-cylinder` は同じ供給を燃焼エンジンに接続します。独立エントロピー最大化、化学ポテンシャルと熱容量微分、連立 ODE 収束、全収支、再生、ロールバックと割当を検証します。アセット v36 はフラグを保存し v1-v35 を読みます。

ポンプ・伝熱・通気連成例は一次収束を示します。供給の両相は物性温度区間内、総密度は binary64 丸め余裕を含め液体未満に保ちます。一定密度/共通気体物性、瞬時平衡、平面界面は研究近似です。実混合物、表面張力、スロッシング、有限速度、実測較正と実際の Unity Editor/Play/Player/IL2CPP は未検証です。

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.ja.md](VALIDATION.ja.md)
