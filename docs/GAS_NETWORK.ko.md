# 컴파일된 가스 네트워크

[English](GAS_NETWORK.md) · [简体中文](GAS_NETWORK.zh-CN.md) · [Français](GAS_NETWORK.fr.md) · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · **한국어** · [Deutsch](GAS_NETWORK.de.md) · [Español](GAS_NETWORK.es.md) · [Italiano](GAS_NETWORK.it.md) · [Português](GAS_NETWORK.pt-BR.md)

유한 가스 네트워크는 이제 `CompiledModel`과 `Simulation`을 통해 실행됩니다. 이 체크포인트는 고정 체적 챔버, 압력과 온도가 고정된 저장소, 제어되는 오리피스, 열 벽 링크를 다룹니다. [가동 실린더 확장](MOVING_CYLINDER.ko.md)은 이제 가스 교환을 크랭크에 의존하는 체적과 압력 일에 연결합니다. 아래에 설명하는 고정 체적 솔버는 원래 거동을 유지합니다.

## C# API와 단위

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume`은 체적 m³, 압력 Pa, 온도 K를 받고, 선택적으로 R은 J/(kg K), gamma를 받습니다. 가스 노드는 체적을 `Storage`에, 온도를 `Initial`에, 압력을 `Position`에, 조성을 `Gas`에 저장합니다. 리터, bar, 제곱밀리미터는 명시적 양으로 받아들여지고, 지문을 만들기 전에 정규화됩니다.

`GasOrifice`는 가스 노드 ID 둘을 잇습니다. `GasReservoir`는 노드 하나를 고정 경계에 잇고, `NodeB == 0`이 그 저장소를 식별합니다. `GasHeatLink`는 가스 노드와 열 노드를 W/K 컨덕턴스로 잇습니다. 연결된 가스 노드는 정확히 같은 R과 gamma를 공유해야 합니다. 개방은 [0,1]의 무차원 분율이며, 초기 입력, 직접 입력, 예약 입력에 대해 검증됩니다. 입력 채널 ID가 0이면 초기 개방이 고정됩니다. 가스 전용 네트워크에는 더미 로터가 필요하지 않습니다. 한계는 노드 32개, 컴포넌트 64개, 스칼라 상태 64개로 남습니다. 가스 체적마다 상태 둘을 소비합니다.

각 가스 노드는 압력, 온도, 질량, 내부 에너지를 노출합니다. 유동 제한은 부호 있는 A에서 B로의 질량 흐름을 노출하고, 열 링크는 부호 있는 가스에서 벽으로의 열 흐름을 노출합니다. 저장소 엔탈피는 안쪽이 양입니다. 에너지 잔차는 `source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`입니다. 질량 잔차는 `sum(mass - initial_mass) - cumulative_reservoir_mass`입니다. 부동소수점 잔차는 정확한 0이 아니라 물리 스케일에 대해 평가됩니다.

## 수치 방법과 경계

가스 솔버는 Heun 예측자/수정자를 쓰는 명시적 서브스텝을 사용합니다. 틱 초기의 최대 상대 질량/에너지 변화율이 균일한 서브스텝 수를 고르며, 서브스텝당 2% 변화를 목표로 합니다. 서브스텝이 4096개를 넘거나, 비물리적 상태, 비유한 값, 수정된 질량/에너지 변화가 25%를 넘으면 배치 전체가 거부됩니다. `StepNanoseconds`를 줄이고 다시 컴파일하거나, 유동 면적, 체적, 컨덕턴스, 초기 조건을 검사하세요.

노즐 법칙은 압력이 같을 때 특이 도함수를 가집니다. 각 평가는 전달 에너지를 연결된 쌍의 등압 양으로 제한하고, 질량과 상류 엔탈피를 함께 스케일합니다. 유한 체적에서 이 에너지는 `abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`이고, 고정 저장소는 B 항을 생략합니다. 이것은 쌍을 이룬 장부를 보존하면서, 고립된 쌍이 압력을 교차하며 진동하는 일을 막습니다. 제한기는 평형 근처의 적분을 바꿉니다. 2차 정확성은 시험 안의, 매끄럽고 제한이 없는 초킹 흐름 세분화 사례에서만 단언됩니다.

벽 온도는 가스 서브스텝 동안 시작 값에 머뭅니다. 누적된 벽 열은 그다음 기존의 열 해석에 들어갑니다. 이 결합은 바깥 틱에 대해 1차입니다. 큰 스텝의 안정성이나 보존만으로 정확성이 확립되지는 않습니다. 벽 시험은 유한 시간 온도를 해석적 2용량 해와 비교합니다. 이 방법은 앞서 제안된 쌍별 암시적 솔버가 아니며, 그 제안을 검증하지 않습니다.

질량, 에너지, 저장소 합, 보정 장부 보정항은 시뮬레이션 상태에 속하며, 복사, 롤백, 분기, 해시에 포함됩니다. 성공한 스텝과 호출자 버퍼 스냅샷은 관리 메모리를 할당하지 않습니다. 실패한 예약 배치는 앞선 성공 틱 뒤의 실패를 포함해, 이전의 모든 틱과 입력을 복원합니다. 입력 갱신과 종료 예약 이벤트도 비유한 가스 관측량을 거부합니다.

가스 노드가 있는 모델은 솔버 지문 태그 4를 더합니다. 기존의 선형/실린더 모델 지문과 상태 해시는 이전 구성을 유지합니다. 샘플 매개변수는 `unverified`로 남습니다.

## JSON, 에이전트, 이식 가능한 통합 — 2026-09-22

[가스 네트워크 실험실](../assets/labs/gas-network.power.json)은 JSON, CLI, MCP, 이식 가능한 리플레이의 공유 예제입니다. 가스 챔버 둘, 제어되는 내부 유동 제한, 제어되는 저장소 유동 제한, 벽 열 링크가 있습니다. 이벤트에는 보고서/표시 경계 사이의 틱이 포함됩니다. 모든 보고서 경계가 디코딩된 자산 리플레이와 비교됩니다. 매개변수는 합성이며 `unverified`로 남습니다.

`power.model.v1`은 다음 명시적 정의를 더합니다.

| 정의 | JSON 필드와 단위 |
|---|---|
| 가스 노드 | `domain: "gas"`; `storage`: m3 또는 l; `initial`: k; `position`: pa 또는 bar; `gas`: j_kg_k의 gas_constant와 gamma > 1 |
| 가스 오리피스 | `kind: "gas_orifice"`; node_a/node_b; `initial_input`: [0, 1]의 분율; 매개변수: m2 또는 mm2의 면적과 discharge_coefficient |
| 저장소 오리피스 | node_b가 없거나 0인 가스 오리피스. reservoir_pressure(pa 또는 bar)와 reservoir_temperature(k)도 필요 |
| 가스 벽 링크 | `kind: "gas_heat_link"`; node_a는 가스, node_b는 열; 매개변수: w_k의 conductance |

없거나 0인 `input_channel`은 명시적 초기 개방을 고정합니다. 2체적 유동 제한에는 저장소 매개변수가 금지됩니다. 조성은 가스 노드에만 필요합니다. 새 검사 필드는 `mass_flow`, `heat_flow`, `reservoir_enthalpy`, `mass_residual`입니다. 기존의 가스 상태와 에너지 검사 필드는 계속 사용할 수 있습니다.

`CompiledModel.ValidateInput`은 상태를 바꾸지 않고 정적 채널/값 제약을 검사합니다. 실험 검증과 이식 가능한 자산 생성은 나중 이벤트를 포함한 모든 예약 개방에 이것을 씁니다. 런타임 제출과 스텝은 여전히 상태에 의존하는 추가 관측량 검사를 하고, 완전한 롤백을 유지합니다.

`power.asset.v3`와 이후 버전은 유계 색인 확장 기록으로 가스 조성, 면적, 유량 계수, 저장소 압력을 유지합니다. 벽 컨덕턴스, 저장소 온도, 초기 개방, 입력 채널 ID는 기본 컴포넌트 필드를 씁니다. v1/v2 판독기는 원래 모델 집합에 대해 계속 지원되며, 가스 정의는 거부합니다. 진본 변경 전 픽스처가 하위 호환을 검증합니다. [자산 형식](ASSET_FORMAT.ko.md)을 참고하세요.

MCP 기능 버전 0.8.0은 가스 도메인, 컴포넌트, 충실도, 개방 한계, 유계 솔버 한계를 알립니다. `get_example_model`은 `gas-network`를 받습니다. 빌드는 `GasNetwork.powerasset`을 내보냅니다. 스튜디오는 기존 입력과 출력 채널로 개략 용기, 저장소 표식, 유동 제한/열 경로를 더합니다. 새 가져오기와 Play Mode 테스트는 실제 Unity 편집기 실행이 필요하며, .NET 증거로는 다루어지지 않습니다.

## 검증과 남은 엔진 작업

아홉 개의 컴파일 모델 그룹과 여섯 개의 가스 원시 요소 그룹이 두 Core 대상에 대해 계속 실행됩니다. 이식 테스트는 추가로 섞인 실린더/가스/열 모델, 비 SI 양, 기본이 아닌 조성, 확장 손상, 누락/중복 기록, v1/v2 호환, 예약 한계, 취소, 이벤트 커서 롤백을 다룹니다. JSON/Core 동등성과 실제 MCP 리플레이가 통합 경계를 다룹니다. 직렬 검증 결과는 [검증](VALIDATION.ko.md)을 참고하세요.

이 검사에서 Standard 어셈블리는 .NET 10에서 실행됩니다. 이것은 Unity 편집기나 IL2CPP 증거가 아닙니다. 타이밍된 유동 제한이나 예혼합 추적이 없는 모델에서는, 고정 체적 전용 솔버의 방정식, 적분 한계, 지문 구성이 변하지 않습니다. 가동 챔버나 타이밍된 유동 제한이 있는 모델은 [MOVING_CYLINDER.ko.md](MOVING_CYLINDER.ko.md)와 [VALVE_TIMING.ko.md](VALVE_TIMING.ko.md)에 문서화된, 버전이 따로 매겨진 분할 결합을 씁니다.

선택적 [예혼합 연소](PREMIXED_COMBUSTION.ko.md)는 이제 일정한 가스 물성으로 연료, 신선 공기, 생성물을 수송합니다. 상세 화학종 열화학, 교정된 차량 샘플, 완전한 엔진/변속기/제어 이정표는 아직 열려 있습니다.
