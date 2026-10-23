# 압축성 평형 연료 덕트

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · **한국어** · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## 모델과 물성

`equilibrium_fuel_duct`는 명시적인 `compressible_fuel` 셀을 통해 질량, 운동량, 총에너지를 수송합니다. 액체는 압축성이며 증기는 이상기체이고 두 상은 압력, 온도, 속도를 공유합니다. 국소 평형으로 응축과 플래시 증발을 계산합니다. 유한한 정지 저장조 2개와 유한 열벽이 수지를 닫습니다.

액체의 열팽창은 0이고 비열은 일정합니다. `K`는 압력 0에서의 체적탄성률이며 실제 값은 `K+p`입니다. 냉압축 에너지, 화학 퍼텐셜, 엔트로피, 평형 음속은 동일한 퍼텐셜에서 도출합니다. `a`는 기존 액체 에너지 오프셋, `p0(T)`는 액체 체적을 무시한 포화 곡선, `x`는 증기 질량분율, `Cv_eq`는 비체적이 일정할 때 혼합물 내부에너지의 온도 미분입니다. `Tr`, `Pref`, `Lu_ref`는 기준 온도, 압력, 잠재 내부에너지입니다. SI 단위를 사용합니다.

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## 수송과 경계

`liquid_bulk_modulus`를 선언하고 `compliance`는 생략하며 각 노드의 `initial_velocity`를 명시합니다. 순서가 있고 중복되지 않는 내부 `cells`를 1–24개 선언하며 각 셀은 덕트 하나에 속합니다. 셀 길이는 체적을 `area`로 나눈 값입니다. 저장조 속도는 0이고 내부 셀을 겸할 수 없습니다. 모든 포트는 동일한 물성을 사용합니다. `linear_resistance`와 `quadratic_resistance`는 전체 관의 저항이며 셀 체적에 따라 배분합니다. `fluid_heat_fraction`은 마찰열을 유체와 벽에 배분합니다. 직접 개도 입력은 없습니다.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

보존형 Rusanov 플럭스는 동일한 하위 스텝 상태에서 모든 면을 갱신합니다. 속도 상한은 최대 절대속도와 물성 음속 상한의 합입니다. CFL 한계는 최소 포트 체적의 0.2배를 면적과 속도로 나눈 값이며 구간당 최대 4096 하위 스텝입니다. 수송과 국소 정확 저항 감쇠는 1차 분할을 사용합니다. 수치 확산은 총 엔트로피 변화에 포함하며 보고된 마찰열과 구분합니다. 정지 경계는 기계적 일 없이 저장조 운동량을 흡수하고 유입 운동에너지는 유한 저장조의 내부에너지로 바뀝니다. 잘못된 상태나 반올림 오차를 넘는 엔트로피 감소는 전체 배치를 거부합니다. 시간 간격을 줄이고 물성 범위와 셀 해상도를 확인하십시오.

## 사용과 검증

`equilibrium-fuel-condensing-duct` 또는 `equilibrium-fuel-flashing-duct`를 실행합니다. 각 셀은 밀도, 속도, 상분율, 음속, 온도, 에너지를 제공합니다. 포트 질량과 에너지, 벽 충격량, 전체 재고도 관측합니다. 독립 검사는 액체 압축 일, 이상증기 극한, 화학 퍼텐셜 일치, Clapeyron 및 Gibbs 관계, 엔트로피 역산 음속, 질량에너지 역산, 보존, 유한 벽 마찰을 다룹니다. 셀 평균 음향 오차는 8, 16, 24개 셀에서 감소합니다. 정확한 재생, 롤백, 취소, 분기, 할당 없는 스텝, 이식 가능한 asset, 실제 MCP는 따로 검증하며 자세한 결과는 검증 증거에 기록합니다.

[VALIDATION.ko.md](VALIDATION.ko.md)

## 적용 범위와 다음 작업

매개변수는 `unverified` 상태입니다. 선언된 온도와 압력 범위, 양의 잠재 엔탈피, 분리된 공존 비체적, 희박 증기 조건을 지켜야 합니다. 순간 균질 평형을 가정한 강체 등단면 수평 덕트입니다. 유한 속도 핵생성, 상간 미끄럼, 표면장력, 중력, 관 탄성, 실제 유체 임계 거동은 제외합니다. 1차 수치 확산은 상변화와 음향 전면을 흐리게 할 수 있습니다. 보존과 재생은 공간 정확도나 실측 하드웨어 타당성을 입증하지 않습니다. 실제 Unity 인수 검증은 남아 있습니다.

압축성 덕트를 호환되는 축 구동 챔버와 니들/노즐 경계에 연결하고 압력 일과 상 가정을 명시합니다. 이후 상완화/미끄럼, 점화, 흡배기, 기계 손실, ECU/TCU 협조 제어를 진행합니다. 실측 보정과 실제 Unity 인수 검증은 별도로 수행합니다.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
