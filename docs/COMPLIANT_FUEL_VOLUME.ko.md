# 탄성 연료 체적 참조 모델

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · **한국어** · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## 구성 관계 범위

`CompliantFuelVolume`은 상평형 연료 네트워크의 구성 폐쇄 관계를 제공합니다. 총질량과 온도 또는 에너지에서 액체, 증기, 탄성 저장을 동반한 공존을 풉니다. 펌프 공급 인젝터 경로는 결합 확장이 남습니다. [상평형 연료 수송 네트워크](EQUILIBRIUM_FUEL_NETWORK.ko.md)

## 열역학

액체의 밀도와 비열은 일정하며 증기는 이상 의사 성분입니다. 두 상은 기존 액막 물성 기준을 공유합니다. 액체 화학 퍼텐셜은 `p/rho_l`을 포함하므로 `p_0(T)`는 액체 변위를 무시한 물성 기준이며 유한 밀도 순수 유체의 포화 압력이 아닙니다. 탄성 벽 외압은 0이며 압력 일을 한 번만 저장합니다.

`StoredEnergyJoules`는 열량 및 탄성 에너지를 포함합니다. 그래프에 연결할 때 발열량에 해당하는 화학 에너지는 별도 장부로 유지합니다.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

공존 상태는 낮은 증기 밀도의 근을 선택합니다. 잠재 엔탈피는 `L_u + R T - p/rho_l`이며 압력 도함수는 Clapeyron 관계를 만족합니다. 고정 질량에서 보고한 열용량은 상전달을 포함한 유체 및 탄성 총에너지를 미분합니다. 벽은 엔트로피를 갖지 않습니다.

## Core API

`Evaluate`는 온도와 총질량을 받습니다. `Flash`는 총질량과 유체 및 탄성 저장 에너지를 받습니다. `AdvanceHeat`는 선언된 열전도도와 초 단위 국소 시간으로 후진 Euler를 적용해 유한 열욕과 열을 교환합니다. 속성 이름에 SI 단위가 명시됩니다. 이 국소 호출은 `Simulation`의 제한된 정수 시계를 대체하지 않습니다.

`FuelVolumeStatus`는 잘못된 입력, 온도·질량·에너지 범위, 솔버 실패를 구분하며 거부된 출력은 기본값입니다. `Flash`는 요청 에너지를 유지하고 부동소수점 열량 재구성 오차를 `CaloricResidualJoules`로 보고합니다. 인스턴스는 변화하는 상태를 보유하지 않으며 성공 호출은 관리 메모리를 할당하지 않습니다.

## 유효 범위

양의 기준 체적, 컴플라이언스, 액체 밀도 및 최대 벽 압력을 선언합니다. 압력 한계는 전체 공존 곡선을 포함하며 `rho_l R T_min`보다 작습니다. 최저 온도의 공존 벽 팽창은 기준 체적의 최소 `128 * epsilon`이며 `epsilon = 2^-52`입니다. 분해할 수 없는 벽 변형과 선언한 모델 범위 밖의 고밀도 증기 분기를 제외하지만 임계점을 예측하지는 않습니다.

물성 온도 구간과 `0 < M <= rho_l (V_0 + C p_max)`를 지킵니다. 진공, 운반 기체, 실제 혼합물, 열팽창, 유한 벽 관성, 표면 장력 및 유한 속도 상변화는 범위 밖입니다. 물성과 벽 매개변수는 `unverified`로 축 구동 펌프 충전, 인젝터 연결, 운동량 해석 및 초킹에는 추가 결합과 독립 근거가 필요합니다.

`AdiabaticReferenceVolume`는 질량과 엔트로피를 고정한 채 동일한 불변 물성을 명시적 기준 체적 사이로 옮깁니다. 반환 에너지 변화는 탄성 저장을 포함한 기계 일입니다. 가변 체적 평가, 플래시, 유한 열욕은 선언된 물성 및 벽 한계를 지킵니다. [축 구동 상변화 연료 펌프](FUEL_CHAMBER_PUMP.ko.md)

## 독립 근거

화학 퍼텐셜, Clapeyron 기울기, 열량 및 엔트로피 도함수, 탄성 저장을 포함한 Gibbs 관계를 비교합니다. 독립 구속 엔트로피 최대화는 포화 솔버를 사용하지 않습니다. 강체 용기, 이상 기체 및 광역성 비례 극한과 유한 열욕 RK4 세분화가 독립 참조입니다. 열교환은 총에너지를 보존하며 총엔트로피 비감소 조건에서 1차 수렴합니다. 실측 연료 유효성이나 Unity 승인을 뜻하지 않습니다.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.ko.md)
