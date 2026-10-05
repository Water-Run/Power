# 유한 액체 연료 탱크

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · **한국어** · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## 계약

`liquid_fuel_tank`는 연결 인젝터의 밀도, 필름 열 기준과 발열량으로 유한 액체 질량과 열 에너지를 저장합니다. 공급은 `tank_component`로 선택하고 `supply_temperature`를 생략합니다. 각 탱크는 연료 물성이 같은 한 공급에 속합니다.

양의 펌프 유량은 허용 구간의 남은 재고로 제한됩니다. 같은 유효 충액 변위가 축 반력과 압력 전달을 정해 축/유체 일을 보존합니다. 빈 탱크의 전진 회전은 액체나 유체 일을 공급하지 않으며 부호 있는 반환 흐름은 현재 레일 열 에너지를 탱크에 혼합합니다.

탱크 열/화학 에너지는 전체 저장 원장에 포함됩니다. 내부 전달은 외부 질량이나 화학 공급을 추가하지 않습니다. 명시적 펌프 입구 지정 압력은 압력 일 경계를 유지합니다. 가스 흡배기는 화학 경계 에너지를 운반할 수 있습니다.

`finite-tank-liquid-cylinder`와 `finite-tank-needle-cylinder`는 탱크 ID 1513과 공급 ID 1511을 사용합니다. `mass`, `temperature`, `internal_energy`, `chemical_energy`, `tank_state`를 읽으며 0은 액체 있음, 1은 비었음을 뜻합니다. 건조 온도는 선언된 초기 기준을 보고합니다.

## 증거와 한계

v27은 탱크와 공급 선택을 저장하고 v1-v26을 읽습니다. 각 탱크는 기존 상한 안에서 4개 상태를 추가합니다. 독립 습윤 교환, 고갈 압력/축 에너지 해석, 반환 혼합, 전체 원장, rollback, 분기와 무할당 스텝을 검사합니다.

기하학적 용량, 통기/기체 공간/슬로싱, 캐비테이션, 실측 충액/효율/조압과 분해 분무는 미완성입니다. 매개변수는 `unverified`이며 실제 Unity Editor/Play/Player/IL2CPP와 OEM 보정은 미검증입니다.

[VALIDATION.ko.md](VALIDATION.ko.md)
