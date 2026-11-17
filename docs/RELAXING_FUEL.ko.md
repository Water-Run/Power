# 유한 속도 연료 상변화 완화

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · **한국어** · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## 물리 모델

`relaxing_fuel` 노드는 독립적인 증기 질량 분율을 가집니다. 액체와 증기는 압력, 온도, 속도를 공유하지만 화학 평형에는 명시된 유한 시간이 필요합니다. `relaxing_fuel_duct`는 명시적인 셀을 통해 질량, 운동량, 총에너지와 증기 질량을 수송합니다. 체류 시간은 셀 형상과 실제 속도에 따라 정해집니다.

폐쇄식은 평형 연료 모델의 압축성 액체 냉각 퍼텐셜과 이상 증기를 유지합니다. 초기 분율은 명시하며 평형으로 투영하지 않습니다. 질량, 부피와 내부 에너지가 일정할 때 분율은 일정한 `phase_relaxation_time_ns`로 평형값에 지수적으로 접근합니다. 잠열은 내부 에너지 수지에 남습니다. 화학 친화도는 액체와 증기의 화학 퍼텐셜 차이며, 상변화율과의 곱을 온도로 나누면 음이 아닌 엔트로피 생성이 됩니다. 이는 연구용 완화 법칙으로 실측 핵생성 상관식이 아닙니다.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## 수송과 수치 방법

보존형 Rusanov 플럭스가 네 장을 수송합니다. 각 전역 CFL 구간에서 국소 상변화 반 단계, 수송, 반 단계를 적용합니다. 공유 저장조는 덕트 수와 무관하게 소스항을 한 번씩 받습니다. 고립된 정적 부피 상태의 소스 궤적은 정확하지만 결합 수송은 일차 오차를 가집니다. CFL은 동결 분율 음속 상한을 사용합니다. 상변화와 덕트 엔트로피를 별도로 검사하며 범위 이탈은 전체 배치를 롤백합니다.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## 사용, 증거와 한계

`relaxing-fuel-evaporation` 또는 `relaxing-fuel-duct`를 실행합니다. 실제 및 평형 분율, 화학 친화도, 상변화 엔트로피, 포트 증기 수지와 전역 증기 잔차를 관찰합니다. 증발·응축 이력은 수락된 각 틱의 순 소스 이동을 부호별로 나누며 미시적인 정·역방향 총 이동은 아닙니다. 독립 지수 해, 엔트로피 미분, 동결 음향, 강성 평형 극한, 시간 자체 세분화와 독립 선형 음향 격자 세분화를 확인합니다. 트랜잭션, 이식 가능한 재생과 실제 MCP는 별도로 검증하며 수치는 검증 증거에 기록합니다.

매개변수는 `unverified`입니다. `initial_vapor_mass_fraction`은 [0,1], 완화 시간은 1 ns부터 한 시간까지 정수로 선언합니다. 재료의 온도 및 양압 범위를 지켜야 합니다. 강체 수평 일정 단면 덕트, 단일 순수 유사 유체, 액체 열팽창 없음과 이상 증기를 가정합니다. 상간 미끄럼, 계면 열전달 지연, 표면 장력, 분무와 운반 가스는 제외합니다. 완화 도메인의 펌프, 노즐, 이동 챔버와 연소 수신부 직접 결합은 남아 있습니다. 보존, 재생과 세분화는 실측 장비 타당성이나 실제 Unity 인수를 입증하지 않습니다. 실제 유체 임계 영역에서 벗어난 희박 증기에 한하며, 재료는 양의 잠열 엔탈피와 구별되는 공존 비체적을 유지해야 합니다.

호환되는 유한 속도 경계 결합을 확장하고 재료 완화 실측값을 확보한 뒤 해상된 분무와 연소 수신부를 추가합니다. 점화, 흡배기, 기계 손실, 변속기 작동과 ECU/TCU 협조 제어를 계속합니다. 실측 보정과 실제 Unity 인수는 별도로 진행합니다.

[VALIDATION.ko.md](VALIDATION.ko.md)
