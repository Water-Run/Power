# 연료 노즐의 평형 상변화 수송

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · **한국어** · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## 물리 모델

`compressible_fuel_nozzle`의 `phase_mode`를 `equilibrium`으로 설정하면 통과 중 순간 균질 상평형을 가정합니다. 액체와 증기는 압력, 온도, 속도를 공유하며 증기분율은 등엔트로피선을 따라 변합니다. 노즐과 유한 저장조는 같은 압축성 액체 퍼텐셜과 이상증기 기준을 사용합니다. `frozen`도 별도의 지원 가정으로 유지됩니다.

압력과 비엔트로피가 안정 액체, 증기 또는 공존 상태를 결정합니다. 화학 퍼텐셜 일치와 기존 온압 범위를 사용하며 온도 끝점은 binary64 반올림 근방만 허용합니다. 면적, 벽열 배분, 개도, 실제 니들은 공통 노즐 계약을 따릅니다. 제트는 수신 압력까지 이상 완전 팽창하며 정지 충격량은 니들 축력이나 기계적 일을 추가하지 않습니다.

## 전역 유속 포락

평형 음속 제곱이 양수이므로 등엔트로피 밀도는 압력과 함께 증가하고 엔탈피 강하는 감소합니다. 이 단조 인자로 상경계와 여러 극값을 포함한 구간 유속 상한을 구합니다. 분기 한정은 표본 하한과 잔여 상한의 상대 폭을 1e-4 이하로 만듭니다. 스택 구간 최대 512개, 분할 8192회입니다. 열량 차이의 반올림 여유와 단조 압력 적분 경계를 사용하며 최대 256 Riemann 구간과 80회 폐쇄 반복을 수행합니다. 해결할 수 없으면 배치를 거부합니다. 매끄러운 내부 최대값은 음속 조건을 만족하지만 상경계 최대값은 비매끄러울 수 있습니다.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

보존형 1차 하위 스텝이 같은 질량과 엔탈피를 전달하고 제트 운동에너지를 수신 유체와 벽에 배분합니다. 물성 상태와 쌍 전체 엔트로피를 확인합니다. 구간당 최대 4096 스텝, 후보당 최대 64회 반감, 초기 전달량은 작은 저장조 질량의 0.1배 이하입니다. 닫힘 판단은 최대 체적탄성률 또는 포트 압력의 128 binary64 epsilon배 반올림을 허용하며 출력 압력을 클램프하지 않습니다. 실패하면 전체 배치를 롤백합니다. 시간 간격, 재고, 물성 범위, 니들 행정과 면적을 확인하십시오.

## 사용과 검증

`equilibrium-flashing-nozzle` 또는 `equilibrium-pump-needle`를 실행합니다. `flux_relative_uncertainty`는 마지막 채택 국소 구성식 유속 포락이며 평균 유량, 전달 질량, 1차 시간 적분, 보정 오차를 제한하지 않습니다. 독립 포화/엔트로피 역산, 기체·액체 해석 초킹, 별도 조밀 전역 탐색, 에너지·엔트로피 항등식, 해석 증기 배출 세분화를 비교합니다. 재고, 축/관/니들, 롤백, 취소, 분기, 할당, 이식 asset, 실제 MCP는 별도로 검증합니다. 수치와 이전 경계는 검증 증거에 보존합니다.

[VALIDATION.ko.md](VALIDATION.ko.md)

## 적용 범위와 다음 작업

매개변수는 `unverified`입니다. 순수 의사유체, 이상증기, 액체 열팽창 0, 공통 속도를 가정합니다. 유한 핵생성·완화 시간, 상간 미끄럼, 분무, 운반기체, 연소 수신 챔버는 포함하지 않습니다. 포락은 선언한 binary64 여유를 사용하며 방향 반올림에 의한 형식 구간 증명은 아닙니다. 임계 전이 부근 목 분류는 유속 포락과 함께 해석해야 합니다. 실측 타당성과 실제 Unity 검증은 남아 있습니다.

재료 증거를 갖춘 유한 속도 노즐 내부 체류를 해상한 뒤 분무와 호환 연소 수신부를 추가합니다. 점화, 흡배기, 기계 손실, 변속기 작동과 ECU/TCU 협조 제어를 계속합니다. 실측 보정과 실제 Unity 인수는 별개로 진행합니다.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
