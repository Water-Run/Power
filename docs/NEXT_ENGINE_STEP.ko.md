# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

연료 도메인 펌프는 이제 물리 니들과 증기 질량분율 동결 토출을 통해 유한 액체 체적 연소 수용실에 공급합니다. 전체 재고에는 상변화, 배제 일, 제트 운동에너지 및 정지 벽 충격량이 포함됩니다.

[축 구동 상변화 연료 펌프](FUEL_CHAMBER_PUMP.ko.md)

`LiquidColumn`은 단면적, 균일 속도, 액체 밀도와 온도가 일정한 수평 강체 관의 독립 C# 기준입니다. 각 국소 구간의 절대 포트 압력이 주어집니다. 선형·이차 저항은 명시적 연구 입력입니다. 그래프 컴포넌트가 아니며 기존 연료 네트워크와 노즐 방정식을 바꾸지 않습니다. [액체 입구 운동량 기준 모델](LIQUID_COLUMN.ko.md)

`fuel_inertial_pipe`는 두 유한 연료 노드와 유한 열벽을 연결합니다. 수평 강체 관로는 양 용기 외에 자체 액체 질량, 열 내부에너지와 운동에너지를 갖습니다. 밀도는 일정하고 온도는 완전 혼합으로 변합니다. 포트와 관로 전체가 액체여야 하며 기존 준정적 연결은 별도 2상 거동을 유지합니다. [유한 액체 연료 관로 운동량](FUEL_INERTIAL_PIPE.ko.md)

`equilibrium_fuel_duct`는 명시적인 `compressible_fuel` 셀을 통해 질량, 운동량, 총에너지를 수송합니다. 액체는 압축성이며 증기는 이상기체이고 두 상은 압력, 온도, 속도를 공유합니다. 국소 평형으로 응축과 플래시 증발을 계산합니다. 유한한 정지 저장조 2개와 유한 열벽이 수지를 닫습니다. [압축성 평형 연료 덕트](EQUILIBRIUM_FUEL_DUCT.ko.md)

`compressible_fuel_chamber`는 회전축을 유한한 `compressible_fuel` 저장조에 연결합니다. 축 운동이 체적을 바꾸며 동일한 압력 일이 유체 에너지와 축 토크를 바꿉니다. 저장조는 `equilibrium_fuel_duct`에 연료를 공급하거나 받으며 평형 플래시 증발과 응축을 처리합니다. [축 구동 압축성 연료 챔버](COMPRESSIBLE_FUEL_CHAMBER.ko.md)

`compressible_fuel_nozzle`는 축 구동 챔버를 포함한 유한 `compressible_fuel` 저장조 사이에 단방향 유동을 제공합니다. 액체 압축 에너지와 이상증기는 저장조 물성을 공유합니다. `phase_mode`를 `frozen`으로 명시하여 통과 중 증기 질량분율을 유지하고 저장조는 다시 평형을 이루게 합니다. [상분율을 동결한 압축성 연료 노즐](COMPRESSIBLE_FUEL_NOZZLE.ko.md)

`compressible_fuel_nozzle`의 `phase_mode`를 `equilibrium`으로 설정하면 통과 중 순간 균질 상평형을 가정합니다. 액체와 증기는 압력, 온도, 속도를 공유하며 증기분율은 등엔트로피선을 따라 변합니다. 노즐과 유한 저장조는 같은 압축성 액체 퍼텐셜과 이상증기 기준을 사용합니다. `frozen`도 별도의 지원 가정으로 유지됩니다. [연료 노즐의 평형 상변화 수송](EQUILIBRIUM_FUEL_NOZZLE.ko.md)

`relaxing_fuel` 노드는 독립적인 증기 질량 분율을 가집니다. 액체와 증기는 압력, 온도, 속도를 공유하지만 화학 평형에는 명시된 유한 시간이 필요합니다. `relaxing_fuel_duct`는 명시적인 셀을 통해 질량, 운동량, 총에너지와 증기 질량을 수송합니다. 체류 시간은 셀 형상과 실제 속도에 따라 정해집니다. [유한 속도 연료 상변화 완화](RELAXING_FUEL.ko.md)

`compressible_fuel_chamber`는 `relaxing_fuel` 저장조의 부피를 제어할 수 있습니다. 가역 축 일은 증기 질량 분율을 고정하며 화학 완화는 별도의 에너지 보존 소스항입니다. `compressible_fuel_nozzle`은 `phase_mode`가 `frozen`일 때 두 완화 저장조를 연결하여 총질량, 증기 질량과 정체 엔탈피를 수송합니다. 실제 솔레노이드, 스프링과 스토퍼가 물리 니들을 통해 개도를 계량할 수 있습니다. [상변화 완화 연료 펌프와 동결 상 계량](RELAXING_FUEL_BOUNDARIES.ko.md)

## 다음 개발

재료 증거를 갖춘 유한 속도 노즐 내부 체류를 해상한 뒤 분무와 호환 연소 수신부를 추가합니다. 점화, 흡배기, 기계 손실, 변속기 작동과 ECU/TCU 협조 제어를 계속합니다. 실측 보정과 실제 Unity 인수는 별개로 진행합니다. [압축성 평형 연료 덕트](EQUILIBRIUM_FUEL_DUCT.ko.md)

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

매개변수는 `unverified`입니다. 양쪽 포트는 동일한 도메인과 재료를 사용하며 완화 포트는 평형 통과를 거부합니다. 저장조의 유한 완화는 부피가 없는 노즐 내부의 화학 변화나 체류 시간을 해상하지 않습니다. 상간 미끄럼, 액체 음압 장력, 핵생성 장벽 법칙, 분무, 운반 가스와 연소 수신부는 없습니다. 이동 저장조는 균일 혼합 및 평균 속도 영이며 덕트 내부 셀은 강체이고 소유자는 하나입니다. 임계 거동에서 벗어난 희박 이상 증기, 선언된 양압·온도 범위, 양의 잠열 엔탈피와 분리된 공존 비체적이 필요합니다. 초기 증기 분율은 명시적인 연구 입력입니다. 수치 일치는 실측 보정이나 실제 Unity 인수를 입증하지 않습니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)

[증기 질량분율 동결 연료 토출과 유한 수용실](FUEL_DISCHARGE.ko.md)
