# 펌프 공급 액체 연료 레일

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · **한국어** · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## 계약

`liquid_rail_feed`는 액체 인젝터를 기존 용적 펌프와 명시적인 물질/열 경계에 연결합니다. 유압 출구 노드는 레일 컴플라이언스와 초기 절대압에 일치해야 합니다. 펌프와 인젝터가 이 압력 노드를 소유하고 다른 미추적 유체 경로는 거부됩니다.

압력 에너지는 유압 노드에 한 번만 저장됩니다. 유량과 축 반력은 보존 연성 해를 따릅니다. 유입 연료는 열 및 화학 경계 에너지를 전달하며 레일 열 저장이 온도를 혼합합니다. 부호가 있는 역류는 현재 레일 온도로 연료를 반환합니다. 노즐 배출, 벽 가열, 가용 증기와 지정 연소는 분리됩니다.

`pump-fed-liquid-cylinder`와 `pump-fed-needle-cylinder`는 물리적 분사와 선택적 니들 운동을 보존합니다. 압력 KPI는 명시적 단위의 선언된 펌프 전용 상한을 사용합니다. 공급 ID 1511에서 `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`을 읽으며 펌프 ID 1510은 실제 축-유체 일을 보여줍니다.

## 증거와 한계

v28은 공급 연결과 원천 온도를 저장하고 v1-v27를 읽습니다. 축/압력 해석 교환, 독립 동시 ODE 세분화, 열 혼합, 질량/연료/에너지/체적 원장, 역류와 완전 rollback을 각각 확인합니다.

기하학적 용량, 통기/기체 공간/슬로싱, 캐비테이션, 실측 충액/효율/조압과 분해 분무는 미완성입니다. 매개변수는 `unverified`이며 실제 Unity Editor/Play/Player/IL2CPP와 OEM 보정은 미검증입니다.

## 유한 액체 연료 탱크

`liquid_fuel_tank`는 연결 인젝터의 밀도, 필름 열 기준과 발열량으로 유한 액체 질량과 열 에너지를 저장합니다. 공급은 `tank_component`로 선택하고 `supply_temperature`를 생략합니다. 각 탱크는 연료 물성이 같은 한 공급에 속합니다.

탱크 열/화학 에너지는 전체 저장 원장에 포함됩니다. 내부 전달은 외부 질량이나 화학 공급을 추가하지 않습니다. 명시적 펌프 입구 지정 압력은 압력 일 경계를 유지합니다. 가스 흡배기는 화학 경계 에너지를 운반할 수 있습니다.

기하학적 용량, 통기/기체 공간/슬로싱, 캐비테이션, 실측 충액/효율/조압과 분해 분무는 미완성입니다. 매개변수는 `unverified`이며 실제 Unity Editor/Play/Player/IL2CPP와 OEM 보정은 미검증입니다.

[LIQUID_FUEL_TANK.ko.md](LIQUID_FUEL_TANK.ko.md)

## 추적 가능한 연료 릴리프 반환

`liquid_rail_return`는 공급을 독점 단방향 `hydraulic_relief`에 연결합니다. 밸브는 레일을 펌프와 같은 지정 입구 압력에 연결합니다. 모든 유체 경로를 등록하며 비호환 포트, 중복 소유와 미추적 경로는 거부됩니다.

`fluid_heat_fraction`는 반환 연료가 운반하는 밸브 손실 비율 [0,1]을 명시합니다. 나머지 열은 선언된 밸브 열 경로를 따릅니다. 레일/탱크 동시 열 혼합은 질량, 화학, 압력 일과 열 원장을 보존하며 외부 원천 반환은 경계를 통해 질량/에너지를 내보냅니다.

[LIQUID_FUEL_RETURN.ko.md](LIQUID_FUEL_RETURN.ko.md)
