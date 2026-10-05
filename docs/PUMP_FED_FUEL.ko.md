# 펌프 공급 액체 연료 레일

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · **한국어** · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## 계약

`liquid_rail_feed`는 액체 인젝터를 기존 용적 펌프와 명시적인 물질/열 경계에 연결합니다. 유압 출구 노드는 레일 컴플라이언스와 초기 절대압에 일치해야 합니다. 펌프와 인젝터가 이 압력 노드를 소유하고 다른 미추적 유체 경로는 거부됩니다.

압력 에너지는 유압 노드에 한 번만 저장됩니다. 유량과 축 반력은 보존 연성 해를 따릅니다. 유입 연료는 열 및 화학 경계 에너지를 전달하며 레일 열 저장이 온도를 혼합합니다. 부호가 있는 역류는 현재 레일 온도로 연료를 반환합니다. 노즐 배출, 벽 가열, 가용 증기와 지정 연소는 분리됩니다.

`pump-fed-liquid-cylinder`와 `pump-fed-needle-cylinder`는 물리적 분사와 선택적 니들 운동을 보존합니다. 압력 KPI는 명시적 단위의 선언된 펌프 전용 상한을 사용합니다. 공급 ID 1511에서 `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`을 읽으며 펌프 ID 1510은 실제 축-유체 일을 보여줍니다.

## 증거와 한계

v26은 공급 연결과 원천 온도를 저장하고 v1-v25를 읽습니다. 축/압력 해석 교환, 독립 동시 ODE 세분화, 열 혼합, 질량/연료/에너지/체적 원장, 역류와 완전 rollback을 각각 확인합니다.

원천은 명시적 외부 경계이며 모델링된 유한 연료 탱크가 아닙니다. 탱크 고갈, 펌프 효율/조압, 라인 손실, 캐비테이션, 압력 의존 물성과 유한 체적 분무는 미완성입니다. 매개변수는 `unverified`이며 OEM 보정이나 실제 Unity Editor/Play/Player/IL2CPP 검증을 입증하지 않습니다.
