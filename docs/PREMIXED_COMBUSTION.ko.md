# 예혼합 연소와 연료 에너지 수지

[English](PREMIXED_COMBUSTION.md) · [简体中文](PREMIXED_COMBUSTION.zh-CN.md) · [Français](PREMIXED_COMBUSTION.fr.md) · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · **한국어** · [Deutsch](PREMIXED_COMBUSTION.de.md) · [Español](PREMIXED_COMBUSTION.es.md) · [Italiano](PREMIXED_COMBUSTION.it.md) · [Português](PREMIXED_COMBUSTION.pt-BR.md)

`premixed_combustion`은 규정된 Wiebe 연소 프로파일을 크랭크와 유한 가스 챔버에 결합합니다. 연료, 신선 공기, 불활성 생성물은 가스 네트워크를 통해 수송됩니다. 반응은 쓸 수 있는 제한 반응물을 소비하고, 저장된 화학 에너지를 가스의 열 에너지로 바꿉니다. 압력 일은 가동 실린더가 쓰는 같은 크랭크 솔버를 구동합니다. Core, JSON, CLI, MCP, 자산 v6가 이 정의를 공유합니다.

이것은 집중 상수 물성의 예혼합 모델입니다. 연결된 네트워크의 모든 구성 성분은 하나의 R과 gamma를 공유합니다. 세 질량 등급은 상세 화학종, 가변 열용량, 반응 속도론, 화염 전파, 자기 점화, 노킹, 배출, 연료 증발, 분사를 나타내지 않습니다. 원래의 점화 예제는 이미 혼합된 기체 입구를 씁니다. [사이클 연료 계량](FUEL_METERING.ko.md)은 별도의 유한 기체 레일과 공기 유입을 지원합니다. 액체 분무와 증발은 모델 밖에 남아 있습니다. 규정 연소와 보존 검사 통과가 실측 엔진 성능을 확립하거나, 완전한 파워트레인 목표를 마치지는 않습니다.

## 조성과 포트

가스 노드는 기존의 `gas` 객체에 선택적으로 `premixed`를 더합니다.

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

발열량과 화학양론 공기/연료 질량비는 양수이고 유한해야 합니다. 연료 분율과 신선 공기 분율은 음이 아니어야 하고, 합은 최대 1입니다. 나머지는 불활성 생성물입니다. 신선 공기는 산화제와 그 희석제를 함께 나타냅니다. 연료 1 kg과 신선 공기 `r` kg을 소비하면 생성물 `1+r` kg이 생깁니다. 남는 신선 공기나 연료는 계속 쓸 수 있습니다. 생성물은 다시 반응할 수 없습니다.

예혼합 노드의 각 저장소 유동 제한은 같은 두 필드로 명시적 `reservoir_fractions`를 지정해야 합니다. 다른 컴포넌트나 내부 유동 제한에는 분율이 금지됩니다. 유입에서는 경계가 그 조성을 공급하고, 유출에서는 유한 체적의 실제 조성을 제거합니다. 연결된 유한 가스 체적은 추적, R, gamma, LHV, 화학양론비를 공유해야 합니다. 호환되지 않거나 추적되지 않는 연결은 거부됩니다. 화학 재고는 포트에서 사라질 수 없습니다.

가스 솔버는 각 구성 성분을, 총 가스와 같은 부호 있는 질량 흐름과 상류 분율로 옮깁니다. 음이 아닌 구성 성분 질량을 전개하고, 그 합으로 총질량을 다시 만듭니다. 예혼합 스텝은 들어오고 나가는 총질량 유량이 거의 상쇄되어도, 총 유출 흐름으로 유계됩니다. 압력 균등화나 저장소 역류로 화학 재고가 만들어지지 않습니다.

예혼합 가스는 선언된 상태 예산에 저장된 구성 성분 값 셋을 더합니다. 연소 컴포넌트는 비가역 각도 프런티어 하나를 더합니다. 모두 기존의 64상태 한계 안에 남습니다. 보정된 경계 장부와 반응 장부는 롤백, 해시, 분기에 참여합니다.

## 연소 법칙과 크랭크 이력

컴포넌트는 `node_a`(크랭크)를 `node_b`(예혼합 가스)에 연결합니다. 가동 챔버는 자기 기하의 크랭크를 써야 하고, 각 챔버는 연소 컴포넌트를 최대 하나 허용합니다. 고정 용기는 해석 실험을 위해 독립 크랭크를 쓸 수 있습니다.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

사이클은 명시적으로 360도 또는 720도입니다. 시작 각은 실제 크랭크에 대한 상대 각이며, 실린더 기하 위상으로 암묵적으로 오프셋되지 않습니다. 지속은 [1e-6 rad, 사이클 각]에 있습니다. 형상 지수 `n`은 [1,16], 계수 `a`는 (0,50]입니다. 시작은 사이클에 대한 나머지로 정규화됩니다. 모든 각에는 단위가 필요합니다. 연소 시작부터의 전진 진행 `z`를 [0,1]로 자르면, 적분 해저드는 `H(z) = a z^n`입니다. 완전한 사이클마다 `a`가 기여합니다.

새로 지나간 전진 각에 대해:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

닫힌 충전물과 배율 1에서, 연소된 분율은 초기 제한 연료량의 `1-exp(-a z^n)`입니다. 지속 경계에서 1로 **강제되지 않습니다**. 한 번의 완전한 연소 창 뒤에 `exp(-a)`가 타지 않은 채로 남습니다. 작은 노출은 상쇄를 피하려고 `expm1`을 씁니다. 활성 창 동안 들어오는 신선 충전물은 완전 혼합된 반응물에 합류합니다. 숨은, 무제한의 사이클당 열원은 없습니다.

선택적 입력 채널은 `burn_multiplier`이며, 해저드를 스케일하는 [0,1] 분율입니다. 0은 반응을 끕니다. 열린 입구로 연료가 들어오는 것을 막지는 않습니다. 이 입력은 인젝터 명령도, 예측 점화 제어기도 아닙니다.

각 컴포넌트는 도달한 최대 크랭크 각을 저장하며, 시작 각으로 초기화됩니다. 반응은 그 프런티어를 넘어서만 일어납니다. 정지, 역회전, 이미 방문한 각을 다시 지나는 것으로는 열을 다시 방출할 수 없습니다. 꺼진 전진 이동도 프런티어를 옮기므로, 다시 켜도 놓친 열은 방출되지 않습니다. 연소 창 안에서 시작하면 남은 전진 노출만 소비합니다. 큰 역전 뒤에는, 크랭크가 이전 최댓값을 넘을 때까지 연소가 억제됩니다. 양방향 엔진 점화와, 제어기가 주도하는 재무장은 이후의 제어 작업으로 남아 있습니다.

## 에너지와 수치 결합

가스 내부 에너지는 열로 남습니다. `U = m cv T`입니다. 화학 에너지는 별도로 `E_chemical = m_fuel LHV`입니다. 저장소 총엔탈피에는 `mdot cp T`와 수송된 화학 에너지가 모두 포함됩니다. 전역 저장 에너지 변화에는 화학 재고가 포함되므로, 연소는 내부 전환이지 추가된 외부 원천 일이 아닙니다.

```text
energy_residual = mechanical/electrical source work + reservoir total enthalpy
                  - rejected heat - change(total stored energy)
```

`net_fuel_energy_in`은 경계 장부의 화학 부분을 따로 노출합니다. 모델을 떠나는 미연 연료를 포함한 순유입입니다. 총 연료 공급도, 정상 상태 연료 소비 지표도 아닙니다. `fuel_residual`과 `fresh_air_residual`은 초기 재고, 순 경계 전달, 현재 재고, 누적 반응을 비교합니다. `mass_residual`은 계속 총 가스 질량을 다룹니다. 구성 성분 전환은 질량을 보존합니다.

가동 챔버에서 열 미리보기는 시도하는 새 크랭크 각에 의존하며, 비선형 크랭크 해석에 참여합니다. 틱 동안의 총열을 `Q`, `r = (V_old/V_new)^(gamma-1)`이라 하면:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

이산 압력 토크는 같은 일을 쓰므로, 가스 에너지, 화학 에너지, 크랭크 일이 일치합니다. 연료는 후보 상태에서 해석이 성공한 뒤에만 소비됩니다. 가스 수송은 여전히 크랭크 일/반응 둘레의 대칭 반 스텝을 씁니다. 벽 온도는 바깥 틱에 걸쳐 고정됩니다. 벽 결합은 1차입니다.

켜진 연소에서, 틱당 각 이동과 끝점 속도 이동은 `min(0.25 rad, duration_angle/32)` 안에 있어야 하고, 대응하는 binary64 각도 분해능 가드가 있습니다. 방출 열은 한 틱에서 연소 전 열 에너지의 25%를 넘으면 안 됩니다. 이것은 받아들인 일과 분해능의 한계이지, 정확성 보장이 아닙니다. 가스 서브스텝과 실린더 반복 한계와 함께 적용됩니다. `numerical_failure`에서는 `step_ns`를 줄이고, 예약된 이벤트를 새 틱에 맞추고, 모델과 세션을 다시 만드세요. 실패하거나 취소된 호출은 상태, 입력, 프런티어, 화학 장부, 재생 커서를 커밋하지 않습니다.

## 출력, 호환, 증거

예혼합 노드는 KPI 필드 `fuel_mass`, `fresh_air_mass`, `product_mass`, `chemical_energy`를 더합니다. 연소 컴포넌트는 누적 `fuel_burned`(kg)와 `heat_released`(J)를 더합니다. KPI 필드 `burn_frontier`는 방문한 최대 크랭크 각(채널량 `burn_frontier_angle`, rad)을 노출하므로, 역전 뒤 억제된 연소를 검사할 수 있습니다. 전역 채널은 화학 에너지, 순 연료 에너지 입력, 연료 잔차, 신선 공기 잔차를 더합니다. 탐색이 반환하는 채널량이 권위 있는 이름입니다. 예를 들어 노드 연료 질량의 이름은 `unburned_fuel_mass`입니다. 보통의 가스 내부 에너지와 흐름 출력은 열과 부호 있는 흐름이라는 의미를 유지합니다.

예혼합 모델은 지문 태그 7과 정규화된 반응/조성 매개변수를 더합니다. 이전의 비반응 지문과 스텝은 변하지 않습니다. 자산 v6는 조성, 저장소 분율, 연소 기록을 더합니다. 진본 v1–v5 픽스처는 호환을 유지합니다. 새 충실도는 `premixed_gas_transport`와 `premixed_wiebe_combustion`입니다.

시험은 닫힌 용기의 해석적 연료/공기 소비와 온도, 제한 반응물, 저장소의 순방향/역방향 전달, 닫힌 네트워크의 구성 성분 보존, 독립적인 반응 크랭크/가스 ODE 수렴, 정지/역전/꺼진 연소, 잘못된 계약, 배치 롤백, 취소, 분기, 스텝/스냅샷의 할당 0을 다룹니다. [점화 실린더 실험실](../assets/labs/fired-cylinder.power.json)은 반복되는 흡기/압축/연소/팽창/배기 단계로 부하를 구동하고, 63개의 JSON/CLI/MCP/자산 보고서 경계 모두에서 동일하게 리플레이됩니다. 수치 증거와 실제 실행 범위는 [VALIDATION.ko.md](VALIDATION.ko.md)에 기록되어 있습니다.

[Cantera의 이상 기체 반응기 방정식](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)이 제어 체적의 질량/화학종/에너지 맥락을 제공합니다. [Ansys SI 엔진 예](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)는 명시적 연소 타이밍과 Wiebe 매개변수를 씁니다. 이 참고는 계약의 동기입니다. 그 상세 화학, 2영역 모델, 예제 매개변수는 복사되지 않았으며, 이 일정 물성 솔버의 검증으로 주장되지 않습니다. 어느 패키지에도 런타임 의존성은 없습니다. 모든 샘플 매개변수는 `unverified`로 남습니다.
