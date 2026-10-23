# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

연료 도메인 펌프는 이제 물리 니들과 증기 질량분율 동결 토출을 통해 유한 액체 체적 연소 수용실에 공급합니다. 전체 재고에는 상변화, 배제 일, 제트 운동에너지 및 정지 벽 충격량이 포함됩니다.

[축 구동 상변화 연료 펌프](FUEL_CHAMBER_PUMP.ko.md)

`LiquidColumn`은 단면적, 균일 속도, 액체 밀도와 온도가 일정한 수평 강체 관의 독립 C# 기준입니다. 각 국소 구간의 절대 포트 압력이 주어집니다. 선형·이차 저항은 명시적 연구 입력입니다. 그래프 컴포넌트가 아니며 기존 연료 네트워크와 노즐 방정식을 바꾸지 않습니다. [액체 입구 운동량 기준 모델](LIQUID_COLUMN.ko.md)

`fuel_inertial_pipe`는 두 유한 연료 노드와 유한 열벽을 연결합니다. 수평 강체 관로는 양 용기 외에 자체 액체 질량, 열 내부에너지와 운동에너지를 갖습니다. 밀도는 일정하고 온도는 완전 혼합으로 변합니다. 포트와 관로 전체가 액체여야 하며 기존 준정적 연결은 별도 2상 거동을 유지합니다. [유한 액체 연료 관로 운동량](FUEL_INERTIAL_PIPE.ko.md)

`equilibrium_fuel_duct`는 명시적인 `compressible_fuel` 셀을 통해 질량, 운동량, 총에너지를 수송합니다. 액체는 압축성이며 증기는 이상기체이고 두 상은 압력, 온도, 속도를 공유합니다. 국소 평형으로 응축과 플래시 증발을 계산합니다. 유한한 정지 저장조 2개와 유한 열벽이 수지를 닫습니다. [압축성 평형 연료 덕트](EQUILIBRIUM_FUEL_DUCT.ko.md)

## 다음 개발

압축성 덕트를 호환되는 축 구동 챔버와 니들/노즐 경계에 연결하고 압력 일과 상 가정을 명시합니다. 이후 상완화/미끄럼, 점화, 흡배기, 기계 손실, ECU/TCU 협조 제어를 진행합니다. 실측 보정과 실제 Unity 인수 검증은 별도로 수행합니다. [압축성 평형 연료 덕트](EQUILIBRIUM_FUEL_DUCT.ko.md)

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

매개변수는 `unverified` 상태입니다. 선언된 온도와 압력 범위, 양의 잠재 엔탈피, 분리된 공존 비체적, 희박 증기 조건을 지켜야 합니다. 순간 균질 평형을 가정한 강체 등단면 수평 덕트입니다. 유한 속도 핵생성, 상간 미끄럼, 표면장력, 중력, 관 탄성, 실제 유체 임계 거동은 제외합니다. 1차 수치 확산은 상변화와 음향 전면을 흐리게 할 수 있습니다. 보존과 재생은 공간 정확도나 실측 하드웨어 타당성을 입증하지 않습니다. 실제 Unity 인수 검증은 남아 있습니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)

[증기 질량분율 동결 연료 토출과 유한 수용실](FUEL_DISCHARGE.ko.md)
