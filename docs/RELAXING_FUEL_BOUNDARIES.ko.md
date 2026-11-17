# 상변화 완화 연료 펌프와 동결 상 계량

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · **한국어** · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## 물리 모델

`compressible_fuel_chamber`는 `relaxing_fuel` 저장조의 부피를 제어할 수 있습니다. 가역 축 일은 증기 질량 분율을 고정하며 화학 완화는 별도의 에너지 보존 소스항입니다. `compressible_fuel_nozzle`은 `phase_mode`가 `frozen`일 때 두 완화 저장조를 연결하여 총질량, 증기 질량과 정체 엔탈피를 수송합니다. 실제 솔레노이드, 스프링과 스토퍼가 물리 니들을 통해 개도를 계량할 수 있습니다.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## 결합과 수치 방법

분율 고정 엔트로피 관계로 부피 변화 후 압력과 온도를 구합니다. 일은 액체 냉압축 에너지를 포함하며 작은 변위에는 안정적인 증분 함수를 씁니다. 동일한 이산 일이 축 토크를 결정합니다. 각 전역 구간에서 보존 덕트 및 동결 노즐 수송 전후에 각 노드의 상변화 소스항을 한 번씩 적용합니다. 구간은 덕트 CFL과 더 작은 포트 재고의 0.02 비율로 제한하며 최대 4096개입니다. 기계 일과 소스는 분리합니다. 결합 수송은 일차이며 시간 세분화가 필요합니다.

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## 사용, 증거와 한계

`relaxing-fuel-chamber` 또는 `relaxing-pump-needle`를 실행합니다. 챔버 부피와 일, 실제 및 평형 분율, 상변화 엔트로피, `total_vapor_delivered`, 포트 재고와 전역 증기 잔차를 관찰합니다. 순수 상 단열선, 독립 분율 고정 압력 적분, 미소 일, 가역 사이클과 독립 이상 증기 축 ODE를 비교합니다. 유한 속도 결합 세분화, 공유 저장조 소스 시간, 전체 수지, 취소, 롤백, 분기, 할당, 이식 재생과 실제 MCP는 별도로 검증하며 수치는 검증 증거에 기록합니다.

매개변수는 `unverified`입니다. 양쪽 포트는 동일한 도메인과 재료를 사용하며 완화 포트는 평형 통과를 거부합니다. 저장조의 유한 완화는 부피가 없는 노즐 내부의 화학 변화나 체류 시간을 해상하지 않습니다. 상간 미끄럼, 액체 음압 장력, 핵생성 장벽 법칙, 분무, 운반 가스와 연소 수신부는 없습니다. 이동 저장조는 균일 혼합 및 평균 속도 영이며 덕트 내부 셀은 강체이고 소유자는 하나입니다. 임계 거동에서 벗어난 희박 이상 증기, 선언된 양압·온도 범위, 양의 잠열 엔탈피와 분리된 공존 비체적이 필요합니다. 초기 증기 분율은 명시적인 연구 입력입니다. 수치 일치는 실측 보정이나 실제 Unity 인수를 입증하지 않습니다.

재료 증거를 갖춘 유한 속도 노즐 내부 체류를 해상한 뒤 분무와 호환 연소 수신부를 추가합니다. 점화, 흡배기, 기계 손실, 변속기 작동과 ECU/TCU 협조 제어를 계속합니다. 실측 보정과 실제 Unity 인수는 별개로 진행합니다.

[VALIDATION.ko.md](VALIDATION.ko.md)
