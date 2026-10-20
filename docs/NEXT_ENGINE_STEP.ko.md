# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

연료 도메인 용기는 보존적 소산 저항과 유한 벽 열 링크를 통해 액체/증기 혼합물을 교환합니다. 펌프 충전, 축 결합 및 인젝터 배출 연결이 남습니다.

[상평형 연료 수송 네트워크](EQUILIBRIUM_FUEL_NETWORK.ko.md)

## 다음 개발

연료 도메인 용기를 축 구동 펌프 충전 및 물리 인젝터 배출에 연결하고 물질·운동량·일을 보존적으로 결합합니다. 국소 입구 관성과 초킹을 독립 참조로 해결합니다. 이 네트워크를 완성된 파워트레인으로 취급하지 않고 점화, 흡배기, 손실, 변속기 구동, ECU/TCU 협조를 계속합니다.

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

구성 관계의 물성 및 분해 가능한 벽 변형 한계가 계속 적용됩니다. 매개변수는 `unverified`입니다. 펌프 충전과 축 일, 인젝터 연결, 국소 관성, 초킹, 핵생성 지연, 상간 미끄럼 및 실제 혼합물 측정은 별도 작업입니다. 기존 펌프 공급 인젝터 레일은 연결 구현까지 분리 액체 경로를 유지합니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)
