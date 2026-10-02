# 가동 실린더 가스 교환

[English](MOVING_CYLINDER.md) · [简体中文](MOVING_CYLINDER.zh-CN.md) · [Français](MOVING_CYLINDER.fr.md) · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · **한국어** · [Deutsch](MOVING_CYLINDER.de.md) · [Español](MOVING_CYLINDER.es.md) · [Italiano](MOVING_CYLINDER.it.md) · [Português](MOVING_CYLINDER.pt-BR.md)

`gas_cylinder`는 회전 크랭크 하나를 가스 챔버 하나에 연결합니다. 밀폐 단열 벤치마크와 달리, 이 챔버는 독립적인 질량과 내부 에너지를 지닙니다. 그래서 유동 제한과 벽 링크가 상태를 바꾸는 동안, 압력이 크랭크를 구동합니다. 컴포넌트는 Core, JSON, CLI, MCP, 이식 가능한 자산에서 쓸 수 있습니다. 피스톤 관성이나 상세 화학은 모델링하지 않습니다. 별도의 [예혼합 연소](PREMIXED_COMBUSTION.ko.md)와 [크랭크각 타이밍](VALVE_TIMING.ko.md) 컴포넌트가 이제 연료 에너지 전환을 공급하고, 연결된 유동 제한을 제어합니다.

## 모델 계약

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

가스 노드는 초기 절대 압력, 온도, R, gamma를 제공합니다. `Storage` 양은 0/None입니다. 실린더 컴포넌트 정확히 하나가 체적을 소유합니다. 컴파일러는 위상을 포함해, 크랭크 초기 각의 기하에서 초기 질량과 에너지를 유도합니다. 독립적으로 지정된 체적이나, 한 챔버를 소유하는 실린더 둘은 거부합니다. 연결되지 않은 고정 가스 노드는 여전히 양의 명시적 체적이 필요합니다.

JSON에서는 `domain: "gas"`를 쓰고, 가동 챔버에서는 `storage`를 생략합니다. `gas_cylinder` 컴포넌트에는 `node_a`(회전), `node_b`(가스), 매개변수 `bore`, `stroke`, `rod_length`, `phase`, `compression_ratio`, `back_pressure`가 필요합니다. 조성이나 초기 가스 상태는 이 컴포넌트에 중복되지 않습니다. 가스 노드는 압력, 온도, 질량, 내부 에너지를 노출합니다. 실린더는 체적, 피스톤 변위, 크랭크 토크를 노출합니다. 포트, 유동 제한, 개방 한계, 벽 링크는 기존의 [가스 네트워크 계약](GAS_NETWORK.ko.md)을 씁니다.

타이밍된 유동 제한이나 예혼합 추적이 없는 가동 챔버를 포함한 모델은 충실도 `moving_cylinder_gas_exchange`를 보고하고, 솔버 지문 태그 5를 더합니다. 기존의 선형 모델, 밀폐 실린더 모델, 고정 체적 전용 모델은 지문과 스텝을 유지합니다. 조성은 고정으로 남고, 연결된 가스 노드는 일치해야 하며, 모든 매개변수는 `unverified`로 남습니다.

## 방정식과 보존적 결합

조성이 고정된 열량적 완전기체에 대해:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

질량/에너지 수지는 표준 개방계 제1법칙을 따릅니다. [Cantera의 제어 체적 방정식](https://www.cantera.org/stable/reference/reactors/controlreactor.html)을 참고하세요. 그 참고는 방정식을 뒷받침하지, Power!의 적분 방식이나 검증을 뒷받침하지는 않습니다. 가스는 Cantera의 선형 단방향 밸브 구현이 아니라, 기존의 양방향 압축성 노즐 법칙을 씁니다. 배압 일은 외부 원천 일입니다. 저장소 엔탈피와 벽 교환은 기존의 장부 부호를 유지합니다.

구현은 가동 챔버를 포함한 모델에 대칭 연산자 분할을 씁니다.

1. 구간의 시작 크랭크 기하에서 가스 교환과 벽 열을 반 틱 진행합니다.
2. 유계 이산 기울기 크랭크 솔버로, 전체 틱에 걸쳐 결합된 전기기계와 단열 압력 일을 풉니다.
3. 결과 크랭크 기하에서 가스 교환과 벽 열을 반 틱 진행합니다.
4. 누적된 가스-벽 열과 전기기계 손실을 열 해석에 적용합니다.

2단계에서 질량은 고정이고, `U_new = U_old (V_old/V_new)^(gamma-1)`입니다. 평균 가스 압력과 토크는 이 같은 에너지 변화의 분할 차분에서 옵니다. 크랭크는 가스 일에서 배압 일을 뺀 값을 얻고, 챔버는 대응하는 가스 일을 부동소수점 정확도로 정확히 잃습니다. `log1p`/`expm1`과 해석적 체적 분할 차분은, 작은 스텝과 사점 근처에서 거의 같은 상태를 빼는 일을 피합니다. 여러 실린더가 크랭크를 공유하거나, 결합된 축을 통해 작용할 수 있습니다.

이 분할은 벽 전달이 없는, 시험된 매끄러운 초킹 흐름 사례에서 2차 수렴을 가집니다. 벽 온도는 두 가스 반 스텝 동안 고정된 뒤, 기존의 열 해석이 이어집니다. 벽 결합의 정확성은 1차로 남습니다. 평형 근처 유동 제한기도 국소 차수를 바꿀 수 있습니다. 보존이 정확성을 확립하지는 않습니다.

## 한계, 실패, 호환

크랭크 이동은 틱당 0.25 rad로 제한됩니다. 비선형 해석은 최대 16회 반복과 10회의 직선 탐색을 씁니다. 각 가스 반 스텝은 4096 서브스텝 한계, 2% 목표 상대 변화, 25% 수정 변화 거부를 유지합니다. 잘못되었거나 비유한 가스 상태, 출력, 솔버 소진은 이전의 모든 틱과 예약 입력을 포함한 호출자 배치 전체를 거부합니다. 다시 시도하기 전에 `step_ns`를 줄이고, 유동 면적, 가스 상태, 컨덕턴스, 크랭크 속도, 관성을 검사하세요. 취소와 분기는 모든 가스 상태와 장부 상태를 유지합니다. 성공한 스텝과 호출자 버퍼 스냅샷은 관리 메모리를 할당하지 않습니다.

자산 v4는 각 가스 실린더에 유계 색인 기하 기록을 더하고, v1/v2/v3 판독기를 모두 보존합니다. 솔버 작업 공간은 직렬화하지 않습니다. 진본 고정 체적 v3 픽스처는, 가동 기하를 도입해도 이전 가스 지문이나 리플레이가 바뀌지 않음을 검증합니다. [자산 형식](ASSET_FORMAT.ko.md)과 [픽스처 출처](../tests/Power.Tests/Fixtures/README.md)를 참고하세요.

## 실험과 증거

[가동 실린더 실험실](../assets/labs/moving-cylinder.power.json)은 저장소 유동 제한 둘과 유한 열 벽으로 실린더 하나를 모터링합니다. 시간 기반 개방 이벤트 여덟 개가 챔버로 들어오고 나가는 흐름을 시험하고, 보고서 경계와 표시 경계 사이의 틱을 포함합니다. 이것은 교정되지 않은 모터링 실험입니다. 일정은 ECU, 캠 프로파일, 4행정 엔진 제어기, 연소 모델이 아닙니다.

시험은 전진/역회전과 사점을 통해 닫힌 챔버를 기존의 밀폐 실린더 구현과 비교하고, 열린 챔버를 지배 ODE의 독립적으로 작성된 RK4 적분과 비교합니다. 후자는 기하, 초킹 질량 흐름, 압력 일을 방정식에서 직접 씁니다. 스텝 세분화는 매끄러운 흐름과 벽 결합 정확성을 따로 검사합니다. 추가 검사는 결합/공유 크랭크 여럿, 밀폐/개방이 섞인 실린더, 보존, 잘못된 소유, 단위 정규화, 원자적 실패/복구, 분기, 취소, 할당 0, 이식 호환, 모든 JSON/MCP/자산 보고서 경계를 다룹니다.

Unity에는 가동 피스톤 뷰, 가스 연결, 가져오기/Play 테스트가 있습니다. 실제 편집기, 렌더링, Play Mode, IL2CPP 증거는 아직 남아 있습니다. 실행된 검사는 [검증 기록](VALIDATION.ko.md)을, 남은 엔진, 변속기, 제어, 교정 작업은 [로드맵](ROADMAP.ko.md)을 참고하세요.
