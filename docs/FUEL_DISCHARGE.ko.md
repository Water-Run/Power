# 증기 질량분율 동결 연료 토출과 유한 수용실

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · **한국어** · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## 물리 모델

`fuel_discharge`는 유한 연료 도메인 공급원을 `finite_fuel_film`의 기체 노드에 연결합니다. `phase_mode`를 `frozen`으로 선언하면 노즐 통과 중 증기 질량분율은 고정되고 양쪽 벌크는 다시 평형화됩니다. 균질 두 상은 온도와 속도를 공유합니다. 액체는 비압축성이고 증기는 이상 의사 종입니다. 재료 특성과 액체 밀도가 같아야 합니다.

상류 정체 압력과 온도에서 수용 압력과 상류 압력 사이의 다음 질량 유속을 최대화합니다. 유일한 내부 최대가 초크 유량을 제한합니다. 순수 액체는 베르누이, 순수 증기는 이상기체 음속 관계로 수렴합니다. 유량 계수는 유효 목 면적을 조절합니다.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## 에너지, 체적 및 운동량

공급원은 운반된 정체 엔탈피를 잃습니다. `wall_kinetic_fraction`은 이상 완전 팽창 제트 운동에너지 일부를 유한 정지 벽에 배분하고 나머지는 수용 유체에 들어갑니다. 이 기준 속도는 수용 압력까지 이상 팽창을 가정하며 실측 출구 속도를 예측하지 않습니다. 충격량은 정지 수용 벽에 전달되고 하우징은 반대 반력을 받습니다. 정지 반력은 일을 하지 않으며 피스톤이나 니들 힘을 추가하지 않습니다.

`finite_volume_density`를 명시합니다. 기체 `volume`은 공동 체적에서 액체 질량/밀도를 뺀 값이고 막 `volume`은 액체 배제 체적입니다. 움직이는 실린더는 수용계 전체의 질량과 엔트로피를 보존해 압력 일을 구합니다. 배제 일은 내부에 남습니다. 고정 공동과 크랭크 실린더를 지원하며 선형 기체 피스톤은 지원하지 않습니다.

## 사용 및 증거

`finite-volume-fuel-discharge`로 혼합 공급원 초크를, `pump-fed-finite-volume-cylinder`로 전동 펌프, 물리 솔레노이드 니들 및 규정 연소를 실행합니다. 선택적 `needle`과 `valve_timing`은 개도에 곱해집니다. 새 예제는 명시적 전압 펄스를 사용하며 사이클 질량 요구나 샘플 투여 제어기가 없습니다. `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity`, `choked`를 전체 재고와 읽습니다. 마지막 네 값은 정지 후 마지막으로 수용된 유동 상태를 유지합니다.

독립 베르누이·이상기체 극한, 별도 혼합 유속 최대화, 에너지/엔트로피 및 독립 적분 노즐/수용실 ODE로 방정식을 검사합니다. 시간 수렴은 1차입니다. 이식 자산 재생, 실제 MCP, 롤백, 취소, 독립 분기 및 할당 검사가 통합을 확인합니다. 실제 Unity Editor/Play/Player/IL2CPP는 미검증입니다.

## 유효 범위와 후속 작업

매개변수는 `unverified`입니다. 선언 온도 경계, 양의 기체 체적, 희박 증기 영역을 지킵니다. 입구 국소 관성, 유한 속도 핵생성, 상간 미끄럼, 분무 해상, 실제 유체 임계 거동 및 실측 보정은 빠져 있습니다. 규정 연소는 화학평형이나 반응 엔트로피 정확도를 입증하지 않습니다. 입구 동역학, 점화, 흡배기, 손실 및 ECU/TCU 협조로 진행합니다.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
