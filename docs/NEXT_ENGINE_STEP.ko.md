# 엔진 개발 재개 노트

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · **한국어** · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 재개 지점

유한 연료 챔버가 축과 압력 일을 교환하고 이상 단방향 저항을 통해 충전/공급합니다. 입구 부족 시 벌크 상변화도 다룹니다. 물리 인젝터와 수신부 결합은 남습니다.

[축 구동 상변화 연료 펌프](FUEL_CHAMBER_PUMP.ko.md)

## 다음 개발

연료 도메인 펌프를 물리 인젝터 및 실린더/액막 수신부에 연결하고 2상 수송, 변위 및 운동 일을 명시합니다. 국소 입구 관성과 초킹을 독립 참조로 풉니다. 점화, 흡배기, 기계 손실, 변속기 구동 및 ECU/TCU를 전체 목표로 계속합니다.

## 수용

새 방정식에 맞는 독립 해석/극한 기준, 완전한 질량/에너지 수지와 시간 간격 수렴을 요구합니다. 배치 롤백, 취소, 분기, 안정 채널, 이식 재현과 이전 판독기를 보존합니다. 이후 점화, 흡기/배기, 기계 손실, 변속기 구동과 ECU/TCU 협조를 계속합니다.

매개변수는 `unverified`입니다. 온도, 압력, 분해 가능한 체적 한계가 전체 주기에 적용됩니다. 비압축 액체, 이상 의사 성분 증기, 순간 공통 온도 평형 및 외부 케이스 압력 0을 가정합니다. 국소 입구 관성/NPSH, 초킹, 핵생성 지연, 밸브 관성/이력, 실제 혼합 연료 및 실측 보정은 남습니다. 물리 인젝터 연결과 수신부 변위/운동 일 결합도 별도 요구입니다.

[DEVELOPMENT_STATUS.ko.md](DEVELOPMENT_STATUS.ko.md) · [ROADMAP.ko.md](ROADMAP.ko.md) · [VALIDATION.ko.md](VALIDATION.ko.md)
