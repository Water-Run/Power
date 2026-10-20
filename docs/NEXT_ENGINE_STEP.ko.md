# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

탱크는 압력 보정 화학 퍼텐셜과 분리 액체 흡입으로 유한 체적 증기 이동을 풉니다.

[유한 체적 연료 탱크 상평형](TANK_PHASE_EQUILIBRIUM.ko.md)

순수 연료 탄성 용기 폐쇄 관계는 액체, 증기 및 탄성 저장을 동반한 공존을 풉니다. 국소 구성 관계 참조이며 그래프의 펌프 및 노즐 수송에는 통합이 필요합니다.

[탄성 연료 체적 참조 모델](COMPLIANT_FUEL_VOLUME.ko.md)

## 다음 개발

이 폐쇄 관계를 보존적 2상 펌프 및 레일 수송에 연결하고 국소 입구 손실, 혼합물 운동량, 노즐 배출을 풉니다. 명시적 물성과 완전한 체적/일 결합을 유지합니다. 없는 OEM 측정은 채우지 않습니다. 점화, 흡배기, 기계 손실 및 ECU/TCU 협조는 별도로 계속합니다.

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

펌프·열·통기 연성 예는 1차 수렴을 보입니다. 공급의 두 상은 지정 물성 온도 구간 안에 있고 총밀도는 binary64 반올림 여유를 두고 액체보다 낮습니다. 일정 밀도/공통 기체 물성, 순간 평형과 평면 계면은 연구 근사입니다. 실제 혼합물, 표면장력, 슬로싱, 유한 속도, 실측 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미검증입니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)
