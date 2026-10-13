# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

수용부는 가역 연료 상평형과 액체 공급 포화 검사를 지원합니다.

[연료 상평형과 액체 공급 경계](FUEL_PHASE_EQUILIBRIUM.ko.md)

## 다음 개발

다음에는 명시적 물성과 보존적 체적/일 연성으로 탱크 증기 이동 및 2상 펌프/레일 유동을 풉니다. 누락된 OEM 측정은 누락 상태로 둡니다. 점화, 흡기/배기, 기계 손실과 ECU/TCU 협조는 별도로 진행합니다.

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

매개변수는 `unverified`입니다. 실측 가솔린 물성, 다성분 또는 유한 속도 상변화, 해상된 액체 변위/분무와 OEM 보정은 제공하지 않습니다. 실제 Unity Editor/Play/Player/IL2CPP 수용은 미완성입니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)
