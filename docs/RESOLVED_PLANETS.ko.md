# 분해된 Ravigneaux 유성 운동

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · **한국어** · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

분해 어셈블리에는 두 내부 유성 세트의 절대 자전과, 캐리어에 대한 그 궤도 질량 관성이 포함됩니다. 물리적 물림 제약 네 개가 로터 여섯 개를 연결합니다. 다섯 레인지 클러치/브레이크와 외부 컨버터는 보통 컴포넌트로 남습니다. 네 부재 축소는 별도의 선언된 단순화로 여전히 사용할 수 있습니다. 그것은 유성 자전 증거를 공급하지 않습니다.

물림 연결과 피치 관계에는 별도의 [구조 참고](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html)가 있습니다. Power!는 자체 보존 로터 그래프와 독립 질량 행렬 검사를 유도하고 구현합니다. 벤더 구현이나 모델 패키지는 포함되지 않습니다.

## 형상과 에너지

링 피치 반경 `R`, 대선/소선 비 `kL`과 `kS`에 대해 강체 피치 형상은 다음과 같습니다.

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters`는 SI 링 반경, 유성당 양의 질량과 자전 관성, 그리고 1..32개의 동기화된 동일 유성 쌍을 요구합니다. 균등 간격은 피치원이 겹치지 않은 채 두 세트에 맞아야 합니다. 형상과 합산 관성은 표현 가능해야 합니다. 이 입력은 모두 명시적 연구 물성입니다. 도우미는 측정값을 공급하지 않습니다.

같은 쌍이 `n`개일 때, 캐리어는 궤도 관성 `n (mInner orbitInner^2 + mOuter orbitOuter^2)`를 받습니다. 이 분해 경로에서 기존의 `CarrierInertia` 매개변수는 캐리어 구조 관성입니다. 새 로터 각각은 유성당 자전 관성의 `n`배를 갖습니다. 그 속도는 절대 각속도이므로, 운동 에너지는 보통의 `J omega^2/2`입니다. 함께 회전해도 유성 자전 에너지는 남습니다. 이 대각 저장에 상대 자전을 쓰면 캐리어 결합이 빠집니다.

## 물림 계약

`carrier_gear`는 `A - ratio B + (ratio-1) C = 0`을 부과합니다. 여기서 C는 실제로 움직이는 캐리어입니다. 외부 물림은 음의 피치 반경 비를 쓰고, 링/외측 유성 내부 물림은 양의 비를 씁니다. 1을 포함한 유한하고 0이 아닌 부호 있는 비가 지원됩니다. 서로 다른 회전 포트 세 개와 호환되는 초기 속도가 필요합니다.

네 물림은 대선/외측 유성, 소선/내측 유성, 링/외측 유성, 내측 유성/외측 유성입니다. 반력 토크 세 개는 모두 같은 중점 사영에 들어가며, 합산 포트 동력과 토크 합은 0입니다. 캐리어 반력은 조용히 정지 접지로 보내지지 않습니다. 유계 상대 잔차 세밀화는 작은 힘 응답을 개선합니다. 중점 풀이는 다음 끝점의 속도 잔차를 0으로 맞추어, 앞선 반올림 오차가 반복해서 반사되지 않게 합니다. 두 연산 모두 실제 제약 힘 응답을 쓰고, 보정 승수를 실제 반력 이력에 유지합니다. 스크래치는 각 시뮬레이션에 속합니다. 컴파일된 인자는 불변으로 남습니다. 정규화된 행, 보정된 좌표, 완전한 반력 이력은 위상, 분기, 취소, 배치 롤백을 보존합니다.

독립적인 자유 기준은 링/캐리어 좌표를 씁니다. `aOuter = R/outerRadius`, `aInner = R/innerRadius`일 때:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

여기에는 두 자전 에너지와, 따로 더한 궤도 관성이 포함됩니다. 독립된 일반화 부하, 모든 전진/후진 경로의 반사 관성, 각운동량, 캐리어 포획 임펄스/열이 조립된 풀이를 검사합니다.

## 공유 그래프와 증거

`CreateResolvedGraph`는 원래 포트, 서로 다른 유성 노드/물림 ID 네 개, 선언된 유성 물성을 받습니다. 불변인 보통 정의를 반환합니다. 내부 로터 여섯 개, 캐리어 물림 네 개, 종감속 하나, 마찰 요소 다섯 개입니다. 평탄한 JSON은 총 로터 관성과 부호 있는 물림 비를 유지합니다. 예제 설명은 생성 형상과 유성당 물성을 기록합니다. 소스 다이제스트는 그 선언된 저작 증거를 보존합니다.

`resolved-ravigneaux-transmission`은 모든 전진 상/하단 인계를 다룹니다. `fired-resolved-ravigneaux-converter`는 엔진, 부호 있는 컨버터 맵, 록업을 더합니다. 둘 다 쌍 세 개, R=0.1 m, 내측/외측 질량 0.3/1 kg, 유성당 자전 관성 0.000015/0.0005 kg m2를 선언합니다. 캐리어 구조는 0.03 kg m2입니다. 명시적 궤도 추가는 0.0184375 kg m2입니다. 이것은 연구 입력입니다.

이식 가능한 자산 v24는 부호 있는 캐리어 물림을 유지하고 이전 버전을 읽습니다. 이 원시 요소는 지문 태그 28을 더합니다. 이전 그래프는 지문과 리플레이를 유지합니다. 준비된 Studio 마커는 물림 포트 세 개를 모두 식별합니다. 실제 Unity 편집기/Play/Player/IL2CPP 검증은 별개입니다. 필요한 직렬 `dotnet run --file tools/Build.cs -- verify`를 실행하세요. 수치 결과와 범위는 [VALIDATION.ko.md](VALIDATION.ko.md)에 있습니다.

## 남은 거동

동기화된 강체 동일 유성 세트는 이 컴플라이언스, 제조 부하 분담, 간극, 물림 손실, 윤활, 온도 의존 물성을 모델링하지 않습니다. [펌프가 공급하는 유압 피스톤 구동](AT_HYDRAULIC_ACTUATION.ko.md)을 사용할 수 있습니다. 완전한 변속 제어, ECU 협조, 측정된 OEM 형상/맵은 아직 끝나지 않았습니다. 이 일반 조립이 PSA AT8/AL4 정체성을 증명하지는 않습니다. 샘플 경계와 누락된 측정은 그대로 남아 있습니다.
