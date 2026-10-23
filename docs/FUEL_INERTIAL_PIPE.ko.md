# 유한 액체 연료 관로 운동량

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · **한국어** · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## 물리 계약

`fuel_inertial_pipe`는 두 유한 연료 노드와 유한 열벽을 연결합니다. 수평 강체 관로는 양 용기 외에 자체 액체 질량, 열 내부에너지와 운동에너지를 갖습니다. 밀도는 일정하고 온도는 완전 혼합으로 변합니다. 포트와 관로 전체가 액체여야 하며 기존 준정적 연결은 별도 2상 거동을 유지합니다.

길이, 면적, 선형·이차 저항, 초기 유량, 초기 온도와 `fluid_heat_fraction`을 선언합니다. 저항 단위는 `pa_s_m3`, `pa_s2_m6`입니다. 양 포트 재료와 밀도가 같아야 합니다. 직접 개도 입력이 없으며 다른 밸브를 닫아도 관로 운동량을 지우지 않습니다. 재료·화학 기준은 포트에서 상속합니다.

## 에너지와 포트

공급 엔탈피가 상류 용기를 나옵니다. 관로는 유입 현열과 자체 액체를 혼합해 하류 압력에서 혼합 엔탈피를 보냅니다. 압력 일은 운동에너지를 바꾸고 마찰열을 한 번만 공급합니다. 선언 비율은 액체를, 나머지는 유한 벽을 가열합니다. 역전에서는 공급·수용 역할을 바꾸어 같은 균형을 씁니다.

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

운동량은 중점 유량과 수용된 최종 압력을 사용합니다. 포트 에너지와 혼합은 후진 오일러이므로 전체 결합은 1차입니다. 무마찰에서도 최종 압력 이산화가 진동 일부를 수치 열로 바꾸지만 물리 마찰에는 포함하지 않습니다. 쌍 엔트로피는 혼합·시간 이산화를 포함합니다. 영유량 이벤트는 정·역방향 이력을 분리합니다.

## 사용 및 증거

`inertial-fuel-line`으로 역전·열 혼합을, `inertial-pump-fed-cylinder`로 입구 관로, 펌프, 물리 솔레노이드 니들과 규정 연소를 실행합니다. `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, 열 재고와 전체 원장을 읽습니다. 체적 유량은 순간값, 질량 유량은 이전 틱 평균입니다. 자체 질량과 화학 재고가 추가됩니다.

독립 유한 컴플라이언스 조화해와 별도 비선형 운동량·온도 RK4로 정확도와 1차 세분화를 확인합니다. 열 분배, 쌍 엔트로피, 역전, 전체 원장, 클러치 시험 구간, 무할당, 롤백, 취소와 독립 분기를 검사합니다. 이식 자산과 실제 MCP가 모든 경계를 검사합니다. 실측 보정이나 실제 Unity 합격은 아닙니다.

## 유효 범위와 후속 작업

매개변수는 `unverified`입니다. 재료 온도·압력 범위와 압력 보정 액상 안정성을 지킵니다. 실패 구간은 전체 롤백되며 틱을 줄이고 공급 고갈·압력·상을 확인합니다. 변밀도 2상 운동량, 공동현상, 음파, 중력, 이동 관 형상과 열 전선 해상은 제외합니다. 규정 연소는 상세 화학평형·반응 엔트로피 정확도를 입증하지 않습니다.

공동현상 입구를 주장하기 전에 호환 에너지·엔트로피 폐쇄로 변밀도·상변화 운동량을 확장합니다. 이후 상 완화·미끄럼, 점화, 흡배기, 손실, 변속기 구동과 ECU/TCU 협조를 진행합니다. 실측 보정과 실제 Unity는 별도입니다.

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
