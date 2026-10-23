# 액체 입구 운동량 기준 모델

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · **한국어** · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## 물리 경계

`LiquidColumn`은 단면적, 균일 속도, 액체 밀도와 온도가 일정한 수평 강체 관의 독립 C# 기준입니다. 각 국소 구간의 절대 포트 압력이 주어집니다. 선형·이차 저항은 명시적 연구 입력입니다. 그래프 컴포넌트가 아니며 기존 연료 네트워크와 노즐 방정식을 바꾸지 않습니다.

## 운동량과 에너지

양 포트의 유량과 속도가 같아 이류 운동에너지 유속이 상쇄됩니다. 압력 일은 저장 운동에너지를 바꾸고 마찰열을 만듭니다. 등온 열조가 열을 받으며 유체 온도와 내부에너지는 일정합니다. 반환 엔트로피 생성은 마찰열을 선언 온도로 나눈 값입니다. 유한 열 재고를 나타내지 않습니다.

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

음적 중점법으로 단조 스칼라 운동량 식을 풉니다. 역전은 같은 식의 영유량 이벤트에서 분할하여 정·역방향 체적과 열을 별도 보존합니다. 성공 호출은 내부 구간을 최대 두 개 사용합니다. 정상 평형을 넘는 단계는 `StepTooLarge`를 반환하므로 시간을 줄입니다. 에너지와 충격량 잔차는 binary64 반올림 규모로 검사합니다. 수용된 거친 간격도 정확도 검사가 필요합니다.

## 코어 API와 수치 증거

명시적 SI 형상, 밀도, 저항, 온도와 최소 절대 압력으로 `LiquidColumn`을 구성합니다. `LiquidColumnState`와 초 단위 시간을 `Advance`에 전달합니다. 속성 이름에 SI 단위가 있습니다. 결과는 부호 및 방향별 전달, 압력 일, 운동에너지 변화, 마찰열과 엔트로피입니다. 거부 시 결과는 기본값이며 입력은 불변이고 관리 메모리를 할당하지 않습니다. 국소 시간은 시뮬레이션 정수 시계를 대체하지 않습니다.

독립 무손실 가속, 선형 저항 지수해와 이차 저항 쌍곡 탄젠트 해로 극한을 확인합니다. 독립 변압 RK4는 역전을 포함한 매끄러운 2차 세분화를 검사합니다. 수동 감쇠, 정상 흐름, 전체 에너지 균형, 비음 엔트로피, 거부, 불변 공유와 할당을 두 어셈블리에서 검사합니다.

## 다음 결합

매개변수는 `unverified`입니다. 압력 하한은 호출자가 선언한 액상 유효 경계이며 증기압이나 NPSH 예측이 아닙니다. 압축성, 음파, 중력, 이동 형상, 열 저장, 공동현상, 상간 미끄럼과 상 완화는 범위 밖입니다. 2상 연료 연결은 준정적입니다. 실제 Unity 수용과 실측 보정은 별도입니다.

`fuel_inertial_pipe`는 두 유한 연료 노드와 유한 열벽을 연결합니다. 수평 강체 관로는 양 용기 외에 자체 액체 질량, 열 내부에너지와 운동에너지를 갖습니다. 밀도는 일정하고 온도는 완전 혼합으로 변합니다. 포트와 관로 전체가 액체여야 하며 기존 준정적 연결은 별도 2상 거동을 유지합니다. [유한 액체 연료 관로 운동량](FUEL_INERTIAL_PIPE.ko.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
