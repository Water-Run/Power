# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

연료 공급은 유한 탱크, 보존적 반환, 기하 헤드스페이스와 명시적 통기를 포함합니다. 펌프, 레일과 기체는 내부 일을 교환하며 액체 공급, 증발과 규정 반응은 구분됩니다. 현재 계약과 검증 증거에서 재개합니다.

[탱크 형상과 유한 헤드스페이스](TANK_HEADSPACE.ko.md)

## 다음 개발

다음 연료 개발은 명시적 물성으로 압력 의존 상평형과 캐비테이션에 집중합니다. 없는 OEM 측정은 없는 상태로, 연구 매개변수는 미검증으로 유지합니다. 실측 펌프/밸브 충액과 조압, 자기/전자 구동과 해상된 분무는 후속 과제입니다.

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

강체 혼합 탱크, 비압축 액체와 이상기체 모델입니다. 슬로싱/정수압 형상, 상평형, 캐비테이션, 실측 펌프/밸브 맵, OEM 보정과 실제 Unity Editor/Play/Player/IL2CPP는 미완성입니다. 매개변수는 `unverified`입니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)
