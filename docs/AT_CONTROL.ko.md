# 유압 AT 피드백

[English](AT_CONTROL.md) · [简体中文](AT_CONTROL.zh-CN.md) · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · **한국어** · [Deutsch](AT_CONTROL.de.md) · [Español](AT_CONTROL.es.md) · [Italiano](AT_CONTROL.it.md) · [Português](AT_CONTROL.pt-BR.md)

## 계약

`at_controller`는 [-1,4] 범위의 정수 목표 단을 받으며 영은 중립입니다. 다섯 쌍의 충전/배출 밸브와 선택적인 토크 컨버터 록업을 관리합니다. 경로 순서는 캐리어 입력, 작은 선기어 입력, 큰 선기어 입력, 캐리어 브레이크, 큰 선기어 브레이크, 마지막으로 록업입니다.

충돌하는 단을 적용하기 전에 실제 패드 힘으로 해제를 확인합니다. 제한된 압력 PI는 측정한 챔버 압력을 사용합니다. 필요한 접촉과 클러치의 물리적 잠금이 확인된 뒤에만 단을 활성 상태로 보고합니다. 소수 요청과 소유된 밸브에 대한 직접 쓰기는 수정 가능한 오류를 반환합니다.

샘플 단계, 고장, 압력 적분과 시간은 전체 트랜잭션 상태에 포함됩니다. 취소, 늦은 실패와 분기는 동일한 이력을 보존합니다. 고장은 해제/체결 시간 초과, 낮은 공급압, 방향 변경과 확인된 잠금 손실을 포함합니다. 배출 명령만으로 물리적으로 막힌 배출 경로를 해제할 수 없습니다.

선택적 록업은 전진 단, 입력 속도, 슬립과 유지 시간 제한 및 별도의 해제 히스테리시스를 사용합니다. 출력은 실제 Released/Applying/Locked/Releasing 상태를 나타냅니다. 록업은 물리적 피스톤 클러치이며 속도 일치 명령이 아닙니다.

## 증거와 한계

예제 `controlled-hydraulic-ravigneaux`와 `controlled-fired-hydraulic-ravigneaux`는 요청 채널 900과 컨트롤러 ID 1400을 사용합니다. 변경되지 않은 128 상태 한도 안에서 99와 122개의 보고 상태를 보존합니다. v28는 경로, 게인과 클록을 저장하고 v1-v27를 읽습니다.

연구용 제어이며 매개변수는 `unverified`입니다. ECU 토크 협조, 상세 센서/밸브, 전체 차량 고장과 OEM 보정은 아직 미완성입니다. managed 및 Standard 검사는 실제 Unity Editor/Play/Player/IL2CPP 검증을 입증하지 않습니다.
