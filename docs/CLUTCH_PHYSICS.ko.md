# 관리형 드라이 클러치 물리

[English](CLUTCH_PHYSICS.md) · [简体中文](CLUTCH_PHYSICS.zh-CN.md) · [Français](CLUTCH_PHYSICS.fr.md) · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · **한국어** · [Deutsch](CLUTCH_PHYSICS.de.md) · [Español](CLUTCH_PHYSICS.es.md) · [Italiano](CLUTCH_PHYSICS.it.md) · [Português](CLUTCH_PHYSICS.pt-BR.md)

`Power.Core`는 불변 `DryClutch` 마찰 법칙과, 일정한 외부 토크와 체결 아래 두 관성을 적분하는 `ClutchPair` 기준 적분기를 제공합니다. 둘 다 제3자 의존성 없이 `net10.0`과 `netstandard2.1`용으로 컴파일됩니다.

이 원시 요소는 이제 통합된 [클러치 그래프 컴포넌트](CLUTCH_NETWORK.ko.md)의 독립 기준입니다. 그래프는 축, 모터, 실린더를 결합하고, 여러 클러치와 열 경로를 지원하며, 내부 이벤트를 거쳐도 배치 전체의 롤백을 유지합니다. JSON, CLI/MCP, 자산 v8, Studio는 그 그래프 정의를 사용합니다. 여기에 적힌 독립 쌍은 일정 부하 기준으로 남으며, 그 자체가 컴파일된 네트워크를 진행하지는 않습니다.

## 마찰 계약

모든 용량과 반력은 포트 A에서 나타냅니다. 부호 있는 비 `r`은 기존 축 컴포넌트와 같은 동력 규약을 씁니다.

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

체결 분율은 `[0,1]`에 있습니다. 정지 용량은 음수가 아니며 슬립 용량보다 작지 않습니다. 둘 다 0일 수 있습니다. 유효 정지 용량이 0이면 클러치는 해제됩니다. 추론된 클램프 압력, 마찰 계수, 플레이트 형상, 온도 페이드, 마모, 항력, 액추에이터 지연은 없습니다.

슬립이 0이 아니면 `tau_A = -sign(g) * C_k`입니다. 슬립이 정확히 0일 때, 적분하는 쪽은 상대 가속도를 0으로 유지하는 데 필요한 토크를 공급해야 합니다. 그 크기가 `C_s` 이하이면 클러치는 그 반력으로 잠기고 마찰열을 내지 않습니다. 그렇지 않으면 불균형 부하 방향으로 미끄러지기 시작하며 `C_k`를 씁니다. 정지 한계와 같으면 잠긴 채로 있습니다. 이 법칙에는 속도 데드밴드가 없고, 작은 상대 속도를 조용히 고착 제약으로 바꾸지 않습니다.

운동 마찰과 제약된 정적 반력을 이렇게 이상적으로 구분하는 것은 다음 1차 자료가 설명하는 역학을 따릅니다. [Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html)와 [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html)입니다. Power!의 구현은 독립적으로 작성되었고 명시적 토크 용량을 씁니다. 두 구현을 재현하지 않으며, 그 더 넓은 구성 모델을 주장하지도 않습니다.

`ClutchMode`는 `Disengaged`, `Locked`, `SlippingPositive`, `SlippingNegative`를 구분합니다. 속도가 0인 모드는, 외부 부하가 정지 용량을 넘으면 이탈하는 슬립 상태일 수 있습니다. `HeatFlowWatts`는 순시값입니다. 그런 영속도 이탈에서의 값은 0이며, 이후의 열은 양수입니다.

## 정확한 일정 부하 쌍

두 양의 관성 `J_A`, `J_B`와 일정한 외부 토크 `T_A`, `T_B`에 대해:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

각 슬립 단계의 가속도는 일정합니다. 요청한 구간 안에서 상대 속도가 0에 닿으면, 솔버는 `t_zero = -g / (dg/dt)`까지 정확히 진행한 뒤 정적 반력을 평가합니다. 그다음 나머지는 잠기거나 반대 방향으로 슬립하며 적분합니다. 일정한 외력에서는 그런 도달이 최대 한 번이므로, 풀이는 최대 두 단계이며 수렴 루프나 시간 세분이 없습니다. 구간 끝점과 정확히 일치하는 이벤트는 그 우측 극한의 반력 모드를 반환합니다.

잠금 궤적은 `omega_A = r*omega_B`를 따르며

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

계산된 도달에서, 운동량을 보존하는 사영이 binary64 이벤트 반올림 잔여를 제거합니다. 큰 관성 가중 합을 만드는 대신 유계 관성 가중치를 씁니다. 일단 잠기면 속도 제약은 명시적으로 구성됩니다. 이것은 해결된 이벤트에서의 반올림 보정이지, 유한 슬립의 비탄성 순시 체결이 아닙니다. 각 진행은 일정한 가속도의 각 단계를 적분합니다. 외부 일은 `T_A*delta_theta_A + T_B*delta_theta_B`이고, 마찰열은 `-tau_A*g`의 적분입니다. 결과에는 A에서의 부호 있는 토크 임펄스와, 따로 검사할 수 있는 운동 에너지 변화가 들어갑니다. 에너지 잔차는 `external_work - heat - delta_kinetic`입니다.

이 쌍은 0이 아닌 유한한 `r`의 어느 부호나 지원합니다. 일반화 운동량 `r*J_A*omega_A + J_B*omega_B`는 `r*T_A + T_B`를 통해서만 변합니다. `r = 1`이면 보통의 각운동량 보존이 적용됩니다. 변속비는 이상적인 기계 변압기를 나타내며, 그 지지부가 토크에 반력할 수 있습니다. 접지 브레이크는 `ClutchPair.Brake(J, friction)`으로 구성합니다. 그러면 포트 B의 속도와 외부 토크는 고정된 0이고 `r = 1`입니다. 무한대는 관성 센티널로 쓰지 않습니다.

## API와 실패 동작

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

이 예제는 1.6초 뒤 두 포트 모두 20 rad/s에 도달하고 800 J의 열을 냅니다. 4초 동안의 각 진행은 144 rad와 64 rad입니다. 모든 수는 합성값이며, 차량 교정이라는 주장은 없습니다.

두 클래스는 불변입니다. `ClutchPairState`와 `ClutchPairStep`은 값 형식입니다. `Advance`는 호출자의 상태를 바꾸지 않고 메모리도 할당하지 않습니다. 독립된 호출자가 같은 쌍을 공유할 수 있습니다. 유지되는 단계 이력이나 전역 시뮬레이션 시계는 없습니다. 공급된 속도와 새로운 일정 부하가 다음 구간을 정합니다.

| 상태 | 의미와 복구 |
|---|---|
| `Ok` | 완전하고 유한한 국소 결과를 사용할 수 있습니다. 보존과 모델 적합성은 따로 평가하세요 |
| `InvalidDuration` | 초 단위의 유한하고 엄격히 양수인 구간을 넣으세요 |
| `InvalidEngagement` | `[0,1]`의 유한한 분율을 넣으세요 |
| `InvalidState` | 유한한 속도를 넣으세요. 접지 브레이크는 B 속도가 0이어야 합니다 |
| `InvalidTorque` | 유한한 외부 토크를 넣으세요. 접지 브레이크는 B 토크가 0이어야 합니다 |
| `NumericalFailure` | 유도된 운동, 이벤트 시각 또는 에너지가 지원되는 binary64 범위를 넘습니다. 단위/척도를 검사하고 구간을 줄이거나 다시 구성하세요 |

어떤 거부여도 출력은 `default`이며, 부분적으로 공개된 상태는 없습니다. 잘못된 불변 매개변수는 생성 시 `ArgumentException` 또는 그 하위 클래스를 던집니다. `DryClutch.Evaluate`도 잘못된 입력이나 넘치는 순시 열을 거부합니다. 이벤트 시각이 언더플로로 0이 되면, 유한한 상대 운동 에너지를 조용히 버리지 않고 실패합니다. 물리 결과는 부동소수점 반올림의 영향을 받습니다. 유한한 입력만으로 유도량이 표현 가능하다는 보장은 없습니다.

`ZeroSlipTimeSeconds`는 상대 속도 0에 처음으로 체결되어 도달한 시각이거나, 구간이 거기서 시작하면 0입니다. 해제 운동을 포함해 그런 도달이 없으면 null입니다. 고착을 뜻하지는 않습니다. 큰 외부 부하는 즉시 반전을 일으킬 수 있습니다. `SlippingDurationSeconds`에는 이탈하는 슬립 단계가 들어가고, 해제 운동은 빠집니다. `EndReaction`은 최종 상태에서의 순시값이고, 열, 일, 임펄스, 각 진행은 구간 전체에서 적분됩니다.

국소 초 매개변수는 `Simulation`의 고정된 유계 나노초 시계를 대체하지 않습니다. 그래프 적분은 정확한 외부 틱/이벤트 경계, 상태 해시, 분기 독립성, 취소, 완전한 다중 틱 롤백을 유지합니다.

## 증거와 한계

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs)는 두 Core 대상 어셈블리에 같은 열 개 그룹을 실행합니다.

- 닫힌 형식의 두 관성 동기화 시간, 속도, 각 진행, 임펄스, 운동량, 손실 운동 에너지. 완전 체결과 부분 체결을 포함합니다.
- 정확한 정적 부하 분담, 등호를 포함하는 브레이크어웨이 문턱, 더 낮은 운동 용량, 데드밴드 없는 0이 아닌 슬립, 운동 용량이 0인 정적 래치.
- 구간 내부와 끝점에서의 반전, 그리고 과도한 부하에서의 접지 제동, 고정, 이탈.
- 양/음 변속비, 일반화 운동량, 독립적으로 계산한 에너지 변화. 2,000개의 결정적 조합이 관성, 변속비, 속도, 외부 부하, 용량, 지속 시간을 훑습니다.
- 일정한 외력 아래 하이브리드 이벤트를 지나는 분할 불변성. 변하는 사인 부하의 중점 샘플링이 독립적인 해석적 속도, 각, 열 적분으로 수렴합니다. 이것은 그 부하 샘플링 예제의 2차 거동을 입증합니다. 그래프는 이와 별도로 자체적인 결합 및 하이브리드 수렴 검사를 갖습니다.
- 잘못된 입력, 오버플로, 해결할 수 없는 이벤트, 실패 시 기본 출력, 독립적인 반복 평가, 그리고 성공한 구간 10,000개에 걸친 할당 없음.

이 쌍은 열을 생성된 에너지로 반환합니다. 그래프 컴포넌트는 그것을 열 노드나 외부 장부로 보냅니다. 어느 API도 DCT/AT 토폴로지, 기어 선택, 토크 컨버터, 유압 액추에이터, ECU/TCU 협조, 클러치 재료 식별, 측정 교정을 구현하지 않습니다. 그 경계는 [로드맵](ROADMAP.ko.md)에 남아 있습니다.
