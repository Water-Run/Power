# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

탱크는 압력 보정 화학 퍼텐셜과 분리 액체 흡입으로 유한 체적 증기 이동을 풉니다.

[유한 체적 연료 탱크 상평형](TANK_PHASE_EQUILIBRIUM.ko.md)

## 다음 개발

다음에는 명시 물성과 체적·운동량·일의 보존 연성으로 2상 수송과 국소 입구 손실을 풉니다. 없는 OEM 측정은 없는 상태로 유지합니다. 점화, 흡배기, 기계 손실과 ECU/TCU는 따로 진행합니다.

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

펌프·열·통기 연성 예는 1차 수렴을 보입니다. 공급의 두 상은 지정 물성 온도 구간 안에 있고 총밀도는 binary64 반올림 여유를 두고 액체보다 낮습니다. 일정 밀도/공통 기체 물성, 순간 평형과 평면 계면은 연구 근사입니다. 실제 혼합물, 표면장력, 슬로싱, 유한 속도, 실측 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미검증입니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)
