# 연료 상평형과 액체 공급 경계

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · **한국어** · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## 계약

선택 항목 `fuel_film.parameters.phase_equilibrium`은 `pa` 또는 `bar`의 `reference_pressure`와 `k`의 `minimum_temperature` / `maximum_temperature`를 지정합니다. 기존 `saturation_temperature`는 기준 온도이고 `latent_internal_energy`는 그 온도에서 증기와 액체의 비내부에너지 차이입니다. 평형 수용부마다 하나의 막을 사용합니다. 초기 스냅샷을 보존하고 수용된 구간에서 기액 평형과 유한 벽 열교환을 계산합니다.

`mass_flow`는 완전한 물리 시간 단계의 부호 있는 순 상변화입니다. `evaporated_fuel_mass`와 `condensed_fuel_mass`는 양·음 질량을 각각 누적하여 상쇄되는 분할 계산을 중복 집계하지 않습니다. `vapor_partial_pressure`는 수용부 연료 분압입니다. 열, 질량과 화학 에너지 수지는 두 상을 포함합니다.

## 열역학 기준

하나의 이상 의사 성분, 수용부의 공통 기체 상수, 일정 비열을 쓰며 액체 변위 체적은 무시합니다. 상의 화학 퍼텐셜 일치로 아래 포화 관계를 얻습니다. 고정 체적에서 총연료와 내부에너지가 공통 온도와 증기 질량을 결정합니다. 화학 에너지는 내부 이동하며 반응은 증기만 소비합니다.

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## 공급과 수치적 제한

유계 스칼라 풀이가 단조 평형 에너지를 사용합니다. 유한 벽 열전달은 후진 Euler입니다. 수송 및 이동 실린더 일과의 연성에 1차 수렴 증거가 있습니다. 액체 공존은 지정 온도 구간 안에 있어야 합니다. 구간 안에서 완전히 증발한 기체는 과열될 수 있으며 포화 곡선을 외삽하지 않습니다.

액체 레일, 유한 탱크와 공급 입구는 Pa 단위 `saturation_pressure`와 `cavitation_margin`을 제공합니다. 여유는 국소 절대압에서 액온의 포화압을 뺀 값입니다. 액체가 있고 여유가 음수이면 전체 배치를 거부합니다. 단상 공급 경계를 검사하며 탱크 증기 이동, 기포 동역학과 캐비테이션 펌프 유동은 미완성입니다.

압력 비교는 binary64 반올림 범위 `64 * epsilon * max(p, p_sat)`만 허용하며 `epsilon = 2^-52`입니다. 보고 여유를 클램프하지 않습니다. 벌크 포화 검사이므로 국소 압력 손실과 펌프 NPSH에는 유동을 해상하는 모델이 필요합니다. 독립 검사에서는 단열 플래시와 유한 벽 열교환의 총엔트로피가 감소하지 않을 것도 요구합니다.

## 증거

`equilibrium-fuel-cell`은 응축, 통기 증발과 재응축을 보여 줍니다. `equilibrium-injected-cylinder`는 통기 탱크 엔진에 가역 수용부 상평형과 공급 여유를 추가합니다. 독립 엔트로피 최대화, 해석적 극한, 연립 ODE 수렴, 완전 재현, 롤백과 할당 검사를 수행합니다. 자산 v30은 물성을 보존하고 v1-v29를 읽습니다.

매개변수는 `unverified`입니다. 실측 가솔린 물성, 다성분 또는 유한 속도 상변화, 해상된 액체 변위/분무와 OEM 보정은 제공하지 않습니다. 실제 Unity Editor/Play/Player/IL2CPP 수용은 미완성입니다.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.ko.md](VALIDATION.ko.md)
