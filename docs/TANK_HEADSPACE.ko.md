# 탱크 형상과 유한 헤드스페이스

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · **한국어** · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## 계약

`liquid_fuel_tank.parameters.headspace`는 `m3` 또는 `l`의 `capacity`와 `gas_node`를 지정합니다. 기체 노드는 `storage`를 생략하며 체적은 `capacity - liquid_mass / density`입니다. 소유자는 하나이고 체적은 양수여야 합니다. 유한 기체가 입구 압력을 결정하므로 펌프와 반환의 지정 저장소 압력은 0입니다.

연성 계산은 외부 압력원 없이 축, 레일과 기체 사이의 압력 일을 교환합니다. 기체 오리피스와 열 연결은 명시적 통기/열전달을 제공합니다. `pressure`, `fill_fraction`, 부호 있는 누적 `hydraulic_work`와 기체 질량, 에너지, 체적을 읽습니다. 용량, 액체 공급, 증발과 연소는 구분됩니다.

## 증거와 제한

`vented-tank-liquid-cylinder`와 `vented-tank-needle-cylinder`는 탱크 1513, 기체 1520, 통기 입력 960을 사용합니다. 자산 v29는 형상을 저장하고 v1-v28을 읽습니다. 해석적 일/미분, 독립 ODE 수렴, 수지, portable/MCP 재현, 롤백과 무할당 단계 검사가 통과했습니다.

강체 혼합 탱크, 비압축 액체와 이상기체 모델입니다. 슬로싱/정수압 형상, 상평형, 캐비테이션, 실측 펌프/밸브 맵, OEM 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미완성입니다. 매개변수는 `unverified`입니다.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ko.md)
