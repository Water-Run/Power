# 有限液体燃料管路の運動量

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · **日本語** · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## 物理契約

`fuel_inertial_pipe` は二つの有限燃料ノードと有限熱壁を接続します。水平剛体管は両容器に加えて独自の液体質量、熱内部エネルギー、運動エネルギーを持ちます。密度一定で温度は完全混合により変化します。ポートと管路全体が液相である必要があり、既存の準静的リンクは別の二相挙動を保ちます。

長さ、面積、線形・二次抵抗、初期流量、初期温度、`fluid_heat_fraction` を宣言します。抵抗単位は `pa_s_m3` と `pa_s2_m6` です。両端材料と密度は一致します。直接開度入力はなく、他弁の閉鎖で管路運動量を消去しません。材料・化学参照はポートから継承します。

## エネルギーとポート

上流容器から供給エンタルピーが出ます。管路は流入顕熱と自身の液体を混合し、下流圧力で混合エンタルピーを送り出します。圧力仕事は運動エネルギーを変え、摩擦熱を一度だけ供給します。宣言割合が液体を、残りが有限壁を加熱します。逆流では供給・受容を交換して同じ収支を使います。

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

運動量は中点流量と採用終端圧力を使います。ポートエネルギーと混合は後退オイラーなので連成全体は一次収束です。無摩擦でも終端圧力離散化が振動の一部を数値熱に変えますが、物理摩擦には計上しません。対エントロピーは混合と時間離散化を含みます。ゼロ流量イベントで正逆輸送履歴を分離します。

## 使用と証拠

`inertial-fuel-line` で逆流と熱混合、`inertial-pump-fed-cylinder` で入口管路、ポンプ、物理ソレノイド針、規定燃焼を実行します。`volume_flow`、`mass_flow`、`forward_volume`、`reverse_volume`、`kinetic_energy`、`energy_at_a`、`energy_at_b`、熱在庫、全収支を読みます。体積流量は瞬時、質量流量は前刻み平均です。独自の質量と化学在庫が加わります。

独立した有限コンプライアンス調和解と非線形運動量・温度 RK4 で精度と一次収束を検査します。熱配分、対エントロピー、逆流、全収支、クラッチ試行区間、無割り当て、巻戻し、取消、独立分岐を確認します。可搬資産と実 MCP が全標本境界を確認します。実測校正や実 Unity 合格ではありません。

## 適用範囲と次の作業

パラメーターは `unverified` です。材料の温度・圧力範囲と圧力補正した液相安定性を守ります。失敗区間は全て戻り、刻みを減らして供給枯渇・圧力・相を確認します。変密度二相運動量、キャビテーション、音波、重力、移動管形、解像熱前線は含みません。規定燃焼は詳細な化学平衡や反応エントロピー精度を示しません。

キャビテーション入口を主張する前に、整合するエネルギー・エントロピー閉包で変密度・相変化運動量へ拡張します。その後、相緩和・滑り、点火、吸排気、損失、変速機駆動、ECU/TCU 協調を進めます。実測校正と実 Unity は別です。

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
