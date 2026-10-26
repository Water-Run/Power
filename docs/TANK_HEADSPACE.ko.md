# 탱크 형상과 유한 헤드스페이스

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · **한국어** · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## 계약

`liquid_fuel_tank.parameters.headspace`는 `m3` 또는 `l`의 `capacity`와 `gas_node`를 지정합니다. 기체 노드는 `storage`를 생략하며 체적은 `capacity - liquid_mass / density`입니다. 소유자는 하나이고 체적은 양수여야 합니다. 유한 기체가 입구 압력을 결정하므로 펌프와 반환의 지정 저장소 압력은 0입니다.

연성 계산은 외부 압력원 없이 축, 레일과 기체 사이의 압력 일을 교환합니다. 기체 오리피스와 열 연결은 명시적 통기/열전달을 제공합니다. `pressure`, `fill_fraction`, 부호 있는 누적 `hydraulic_work`와 기체 질량, 에너지, 체적을 읽습니다. 용량, 액체 공급, 증발과 연소는 구분됩니다.

## 증거와 제한

`vented-tank-liquid-cylinder`와 `vented-tank-needle-cylinder`는 탱크 1513, 기체 1520, 통기 입력 960을 사용합니다. 자산 v37는 형상을 저장하고 v1-v36을 읽습니다. 해석적 일/미분, 독립 ODE 수렴, 수지, portable/MCP 재현, 롤백과 무할당 단계 검사가 통과했습니다.

강체 혼합 탱크, 비압축 액체와 이상기체 모델입니다. 슬로싱/정수압 형상, 실제 혼합물 열역학, 캐비테이션, 실측 펌프/밸브 맵, OEM 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미완성입니다. 매개변수는 `unverified`입니다.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ko.md)

## 유한 체적 연료 탱크 상평형

`liquid_fuel_tank.parameters.headspace.phase_equilibrium`을 `true`로 설정하면 탱크 증발과 응축을 풉니다. 연결 막이 명시적 `phase_equilibrium` 물성을 제공하며 추적 기체 공간은 R, cv, 발열량과 화학량론비를 공유합니다. 젖은 탱크는 공통 초기 온도와 포화 연료 분압, 마른 탱크는 포화 이하 분압을 선언합니다. 총밀도는 액체 밀도보다 낮습니다.

탱크 `evaporated_fuel_mass`, `condensed_fuel_mass`와 부호 있는 `mass_flow`는 완전 물리 단계의 순 상이동을 누적합니다. `vapor_partial_pressure`, 보정 `saturation_pressure`, 액체 재고와 기체 체적을 함께 읽습니다. 펌프는 분리 액체만 흡입하며 2상 펌프/레일 수송, 기포 동반과 국소 NPSH는 미완성입니다.

[유한 체적 연료 탱크 상평형](TANK_PHASE_EQUILIBRIUM.ko.md)
