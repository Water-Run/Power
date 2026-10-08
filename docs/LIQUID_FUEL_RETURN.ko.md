# 추적 가능한 연료 릴리프 반환

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · **한국어** · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## 계약

`liquid_rail_return`는 공급을 독점 단방향 `hydraulic_relief`에 연결합니다. 밸브는 레일을 펌프와 같은 지정 입구 압력에 연결합니다. 모든 유체 경로를 등록하며 비호환 포트, 중복 소유와 미추적 경로는 거부됩니다.

`fluid_heat_fraction`는 반환 연료가 운반하는 밸브 손실 비율 [0,1]을 명시합니다. 나머지 열은 선언된 밸브 열 경로를 따릅니다. 레일/탱크 동시 열 혼합은 질량, 화학, 압력 일과 열 원장을 보존하며 외부 원천 반환은 경계를 통해 질량/에너지를 내보냅니다.

반환 ID 1515에서 `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat`, `mass_flow`를 읽습니다. 공급 ID 1511은 펌프 총 전달을 보고합니다. 총 순환량은 초기 재고를 넘을 수 있고 현재 재고는 초기 재고에서 펌프 전달을 빼고 반환을 더한 값입니다.

## 증거와 한계

`recirculating-liquid-cylinder`와 `recirculating-needle-cylinder`는 유한 연료, 실제 분사, 증발과 선택적 니들 동역학을 보존합니다. v29은 연결/열 비율을 저장하고 v1-v28을 읽습니다. 각 반환은 기존 상한 내 8개 상태를 추가합니다.

독립 릴리프 감쇠/일, 기계/압력/열 동시 세분화, 열 비율, 다중 경로, 외부 경계, replay, rollback과 할당 검사가 통과합니다. 비등이나 미해상 전달 구간은 전체 배치를 실패시킵니다.

강체 혼합 탱크, 비압축 액체와 이상기체 모델입니다. 슬로싱/정수압 형상, 상평형, 캐비테이션, 실측 펌프/밸브 맵, OEM 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미완성입니다. 매개변수는 `unverified`입니다.

[VALIDATION.ko.md](VALIDATION.ko.md)

## 탱크 형상과 유한 헤드스페이스

`liquid_fuel_tank.parameters.headspace`는 `m3` 또는 `l`의 `capacity`와 `gas_node`를 지정합니다. 기체 노드는 `storage`를 생략하며 체적은 `capacity - liquid_mass / density`입니다. 소유자는 하나이고 체적은 양수여야 합니다. 유한 기체가 입구 압력을 결정하므로 펌프와 반환의 지정 저장소 압력은 0입니다.

[탱크 형상과 유한 헤드스페이스](TANK_HEADSPACE.ko.md)
