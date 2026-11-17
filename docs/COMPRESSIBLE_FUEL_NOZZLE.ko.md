# 상분율을 동결한 압축성 연료 노즐

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · **한국어** · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## 물리 계약

`compressible_fuel_nozzle`는 축 구동 챔버를 포함한 유한 `compressible_fuel` 저장조 사이에 단방향 유동을 제공합니다. 액체 압축 에너지와 이상증기는 저장조 물성을 공유합니다. `phase_mode`를 `frozen`으로 명시하여 통과 중 증기 질량분율을 유지하고 저장조는 다시 평형을 이루게 합니다.

면적, 유효면적 유량계수, `wall_kinetic_fraction`, 유한 열벽을 선언합니다. `initial_input`은 [0,1] 개도이며 선택 입력 채널로 변경합니다. 선택적인 `needle_node`, `closed_position`, `full_open_position`은 명령에 실제 병진 니들 개도를 곱합니다. 두 포트의 벌크 속도는 0이고 물성과 발열량이 같으며 내부 셀은 사용할 수 없습니다. 역압은 이상적인 개방 차압 0의 경계를 닫습니다.

## 에너지와 초킹

두 상은 압력, 온도, 속도를 공유하고 상간 질량전달을 억제합니다. 엔트로피 관계와 냉압축 에너지가 등엔트로피 엔탈피 강하를 결정합니다. 유일한 질량유속 최대값에는 평형 음속과 다른 동결 음속을 사용합니다. 계수는 유효면적을 조정합니다. 초킹 시 출구 에너지는 수신 압력까지 완전히 팽창한 이상 제트입니다. 공급 정체 엔탈피는 수신 유체와 명시적 벽열을 한 번만 지불합니다. 충격량은 정지 열화를 뜻하며 전체 하우징 추력이나 니들 축력이 아닙니다.

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

보존형 1차 하위 스텝이 같은 질량과 엔탈피를 전달하고 제트 운동에너지를 수신 유체와 벽에 배분합니다. 물성 상태와 쌍 전체 엔트로피를 확인합니다. 구간당 최대 4096 스텝, 후보당 최대 64회 반감, 초기 전달량은 작은 저장조 질량의 0.1배 이하입니다. 닫힘 판단은 최대 체적탄성률 또는 포트 압력의 128 binary64 epsilon배 반올림을 허용하며 출력 압력을 클램프하지 않습니다. 실패하면 전체 배치를 롤백합니다. 시간 간격, 재고, 물성 범위, 니들 행정과 면적을 확인하십시오.

## 사용과 검증

`compressible-fuel-nozzle`는 유한 증기 배출을, `compressible-pump-needle`는 관 공급, 축 펌핑, 단방향 입구, 솔레노이드 니들을 검사합니다. 누적 질량, 엔탈피, 제트 운동에너지, 벽열, 충격량, 엔트로피를 읽습니다. 평균 유량과 힘은 전체 틱에 해당하며 목·출구·상분율은 마지막 채택 통과 상태입니다. 정확한 액체 일, 기체/액체 해석 초킹, 독립 압력 구적, 동결 엔트로피와 음속 미분, 조밀 유속 탐색, 해석 초킹 배출 세분화를 비교합니다. 보존, 실제 운동, 재생, 분기, 롤백, 취소, 할당, asset과 MCP를 별도로 검증합니다.

[VALIDATION.ko.md](VALIDATION.ko.md)

## 적용 범위와 다음 작업

매개변수는 `unverified`입니다. 이상증기와 압축성·열팽창 0의 액체로 구성된 순수 의사유체입니다. 이상 제트 온도를 포함한 선언 범위를 지켜야 합니다. 동결 통과는 과열 액체를 유지할 수 있지만 실측 핵생성이나 준안정 한계는 예측하지 않습니다. 유한 속도 완화, 상간 미끄럼, 해상된 분무, 비응축 운반기체, 연소 수신 챔버, 실측 맵, 실제 Unity 인수 검증은 남아 있습니다. 보존과 재생은 하드웨어 타당성을 입증하지 않습니다.

호환되는 유한 속도 경계 결합을 확장하고 재료 완화 실측값을 확보한 뒤 해상된 분무와 연소 수신부를 추가합니다. 점화, 흡배기, 기계 손실, 변속기 작동과 ECU/TCU 협조 제어를 계속합니다. 실측 보정과 실제 Unity 인수는 별도로 진행합니다.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
