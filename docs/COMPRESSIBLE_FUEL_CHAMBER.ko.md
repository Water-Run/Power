# 축 구동 압축성 연료 챔버

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · **한국어** · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## 물리 계약

`compressible_fuel_chamber`는 회전축을 유한한 `compressible_fuel` 저장조에 연결합니다. 축 운동이 체적을 바꾸며 동일한 압력 일이 유체 에너지와 축 토크를 바꿉니다. 저장조는 `equilibrium_fuel_duct`에 연료를 공급하거나 받으며 평형 플래시 증발과 응축을 처리합니다.

`node_a`는 회전 포트이고 `node_b`의 기하 소유자는 하나입니다. `minimum_reference_volume`, `swept_volume`, `phase`를 선언하고 초기 체적을 초기 축각의 기하와 맞춥니다. 혼합 저장조의 벌크 속도는 0이며 내부 셀을 겸할 수 없습니다. 질량 없는 정현파 기구는 피스톤이나 캠 관성을 추가하지 않으며 축 관성은 명시합니다. 외부 케이스 압력은 0입니다.

## 일과 수송

닫힌 일 단계는 질량과 엔트로피를 유지합니다. 액체 냉압축 에너지와 열 내부에너지는 덕트와 동일한 평형 퍼텐셜을 사용합니다. 순수 액체에는 정확한 압력 적분을, 이상증기에는 해석적 단열선을 사용합니다. 상변화에서는 엔트로피를 역산하고 최종 에너지를 확인합니다. 채택된 에너지 차이가 공동 기계 해법에서 이산 압력 토크를 제공합니다. 양의 `hydraulic_work`는 축에서 유체로 전달되는 에너지입니다.

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

가변 체적 저장조는 반 수송, 전체 축 일, 반 수송 순서로 계산합니다. 수송은 수치 확산을 가진 1차이며 대칭 분할이 차수를 높이지 않습니다. 매끄러운 닫힌 액체 축 운동은 2차 수렴합니다. 기계 구간의 각 증가량은 0.25 rad로 제한합니다. CFL, 물성 범위, 전체 롤백, 취소, 독립 분기는 그대로 적용됩니다. 정지 포트/하우징 충격량은 피스톤 압력 일과 구분합니다.

## 사용과 검증

`compressible-fuel-chamber-duct` 또는 `electric-compressible-fuel-duct`를 실행합니다. 셀 상태와 함께 저장조 질량, 체적, 압력, 온도, 엔트로피, 증기분율 및 챔버 토크, 동력, 일을 읽습니다. 독립 액체 일과 이상증기 극한, 별도 역산 평형 등엔트로피선과 압력 구적, 액체 축 ODE, 이상증기 반이산 방정식 RK4 세분화를 검사합니다. 보존, 전기/기어/유압/클러치 조합, 이식 재생, 실제 MCP, 할당 없는 트랜잭션은 별도로 확인합니다. 수치 결과는 검증 증거에 기록합니다.

[VALIDATION.ko.md](VALIDATION.ko.md)

## 적용 범위와 남은 작업

매개변수는 `unverified`입니다. 압축성이고 열팽창이 0인 액체와 이상증기의 평형 순수 의사유체, 혼합 챔버, 강체 셀을 가정합니다. 온도와 압력 범위를 지켜야 합니다. 챔버 벌크 운동에너지는 해석하지 않으며 자동 체크 밸브가 없습니다. 유한 속도 핵생성, 상간 미끄럼, 실제 유체 임계 거동, 실측 보정, 실제 Unity 인수 검증은 남아 있습니다.

호환되는 압축성 방향 밸브와 니들/노즐 경계를 추가하고 엔탈피, 운동에너지, 상 가정을 명시합니다. 이후 상완화/미끄럼, 점화, 흡배기, 기계 손실, ECU/TCU 협조 제어를 진행합니다. 실측 보정과 실제 Unity 인수 검증은 별도로 수행합니다.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
