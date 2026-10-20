# 유한 체적 연료 탱크 상평형

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · **한국어** · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## 계약

`liquid_fuel_tank.parameters.headspace.phase_equilibrium`을 `true`로 설정하면 탱크 증발과 응축을 풉니다. 연결 막이 명시적 `phase_equilibrium` 물성을 제공하며 추적 기체 공간은 R, cv, 발열량과 화학량론비를 공유합니다. 젖은 탱크는 공통 초기 온도와 포화 연료 분압, 마른 탱크는 포화 이하 분압을 선언합니다. 총밀도는 액체 밀도보다 낮습니다.

탱크 `evaporated_fuel_mass`, `condensed_fuel_mass`와 부호 있는 `mass_flow`는 완전 물리 단계의 순 상이동을 누적합니다. `vapor_partial_pressure`, 보정 `saturation_pressure`, 액체 재고와 기체 체적을 함께 읽습니다. 펌프는 분리 액체만 흡입하며 2상 펌프/레일 수송, 기포 동반과 국소 NPSH는 미완성입니다.

## 열역학 기준

강체 용기는 비압축 액체와 이상 의사 성분/운반 기체를 포함합니다. 액체 체적이 기체 체적을 바꿉니다. 액체 화학 퍼텐셜은 `p/rho`를 포함하므로 수용부의 무시 가능한 변위 기준 `p_0(T)`에 압력 보정이 붙습니다. `reference_pressure`는 그 물성 기준을 유지하며 유한 밀도 순수 유체 포화압으로 조용히 바꾸지 않습니다.

[연료 상평형과 액체 공급 경계](FUEL_PHASE_EQUILIBRIUM.ko.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

유계 정 U,V 플래시가 총연료와 열에너지를 보존합니다. 명시적 오리피스는 증기, 운반 기체와 화학 에너지를 수송하고 열 연결은 유한 열 노드와 교환합니다. 펌프/반환은 기체와 변위 일을 교환하며 이후 평형이 내부에너지를 재분배합니다. `hydraulic_work`는 내부 상변화 일을 제외한 펌프 변위 일입니다.

## 증거와 제한

`equilibrium-tank-supply`는 역회전, 연료 통기와 유한 가열 벽을 검사합니다. `equilibrium-tank-fired-cylinder`는 같은 공급을 연소 엔진에 연결합니다. 독립 엔트로피 최대화, 화학 퍼텐셜/열용량 미분, 연립 ODE 수렴, 완전 수지, 재현, 롤백과 할당을 검증합니다. 자산 v31은 플래그를 보존하고 v1-v30을 읽습니다.

펌프·열·통기 연성 예는 1차 수렴을 보입니다. 공급의 두 상은 지정 물성 온도 구간 안에 있고 총밀도는 binary64 반올림 여유를 두고 액체보다 낮습니다. 일정 밀도/공통 기체 물성, 순간 평형과 평면 계면은 연구 근사입니다. 실제 혼합물, 표면장력, 슬로싱, 유한 속도, 실측 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미검증입니다.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.ko.md](VALIDATION.ko.md)
