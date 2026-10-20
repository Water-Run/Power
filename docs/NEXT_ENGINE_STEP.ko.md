# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

연료 도메인 펌프는 이제 물리 니들과 증기 질량분율 동결 토출을 통해 유한 액체 체적 연소 수용실에 공급합니다. 전체 재고에는 상변화, 배제 일, 제트 운동에너지 및 정지 벽 충격량이 포함됩니다.

[축 구동 상변화 연료 펌프](FUEL_CHAMBER_PUMP.ko.md)

`LiquidColumn`은 단면적, 균일 속도, 액체 밀도와 온도가 일정한 수평 강체 관의 독립 C# 기준입니다. 각 국소 구간의 절대 포트 압력이 주어집니다. 선형·이차 저항은 명시적 연구 입력입니다. 그래프 컴포넌트가 아니며 기존 연료 네트워크와 노즐 방정식을 바꾸지 않습니다. [액체 입구 운동량 기준 모델](LIQUID_COLUMN.ko.md)

## 다음 개발

명시적 관로 에너지 재고와 호환 포트 엔탈피로 액체 운동량 기준을 유한 연료 네트워크에 연결합니다. 공동현상 입구를 주장하기 전에 변밀도·상변화 유효성을 확립합니다. 상 완화, 점화, 흡배기, 손실과 ECU/TCU 협조를 계속합니다. [액체 입구 운동량 기준 모델](LIQUID_COLUMN.ko.md)

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

매개변수는 `unverified`입니다. 선언 온도 경계, 양의 기체 체적, 희박 증기 영역을 지킵니다. 입구 국소 관성, 유한 속도 핵생성, 상간 미끄럼, 분무 해상, 실제 유체 임계 거동 및 실측 보정은 빠져 있습니다. 규정 연소는 화학평형이나 반응 엔트로피 정확도를 입증하지 않습니다. 입구 동역학, 점화, 흡배기, 손실 및 ECU/TCU 협조로 진행합니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)

[증기 질량분율 동결 연료 토출과 유한 수용실](FUEL_DISCHARGE.ko.md)
