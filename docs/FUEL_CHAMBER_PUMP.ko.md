# 축 구동 상변화 연료 펌프

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · **한국어** · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## 물리 계약

`fuel_chamber`는 실제 축과 유한 연료 챔버를 결합합니다. 입출구 `fuel_check_valve`가 압력차로 충전과 공급을 수행합니다. 액체, 증기, 탄성 벽 에너지는 같은 폐쇄 관계를 씁니다. 입구 공급 부족 시 흡입 중 플래시 증발이 가능하며 공급량은 계산한 재고와 압력에 따릅니다.

`node_a`는 회전축이고 `node_b`는 기하 소유자가 하나인 연료 노드입니다. `minimum_reference_volume`, `swept_volume`를 `m3` 또는 `l`, `phase`를 `rad` 또는 `deg`로 선언합니다. 초기 체적은 binary64 반올림 안에서 초기 축 각도 기하와 일치합니다. 사인 기구에 피스톤/캠 관성을 추가하지 않으며 축 관성은 명시합니다.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## 일과 포트

닫힌 기계 부분 단계는 질량과 엔트로피를 유지합니다. 저장 에너지는 유체와 탄성 벽을 포함하므로 축 일은 기하 기준 체적 변화에 해당합니다. 동일한 승인 에너지 변화가 기어, 컨버터, 유압, 클러치 시험을 포함한 연성 해의 토크를 결정합니다. 팽창은 축을 밀며 양의 `hydraulic_work`는 축에서 챔버로 향합니다. 외부 압력원을 추가하지 않습니다.

`fuel_check_valve`는 `m3_s_pa` 단위 전도도와 선택적 비율 개도를 사용합니다. A가 고압일 때만 균질 공급 유체를 A에서 B로 통과시킵니다. 질량과 개방 압력차가 없는 이상 체크 밸브입니다. 보존 엔탈피 수송과 유한 벽 열교환 계약은 연료 네트워크를 따릅니다.

## 사용과 근거

MCP에서 `equilibrium-fuel-pump` 또는 `electric-equilibrium-fuel-pump`를 요청하거나 JSON을 CLI로 실행합니다. 챔버 질량, 온도, 압력, 증기 비율, 에너지를 밸브 공급과 축 속도와 함께 읽습니다. 구성요소 `volume`는 기하 기준이며 노드 `volume`는 벽 팽창을 포함합니다. `torque`, `hydraulic_power`, `hydraulic_work`가 내부 축 교환을 보여 줍니다.

독립 액체 압력/일, 이상 증기 단열선, 2상 엔트로피 반전으로 폐쇄 관계를 검증합니다. 닫힌 액체 축 운동은 2차, 상변화 충전/공급은 독립 반전 RK4 참조에 대해 1차 세분화를 보입니다. 입구 부족, 충분한 액체 공급, 전기 구동, 기어/유압/클러치 조합, 정확한 재생, 롤백, 메모리 검사가 통합을 다룹니다. 자산 v37은 기하를 저장하고 v1-v36를 읽습니다. 실제 Unity Editor/Play/Player/IL2CPP는 미검증입니다.

## 남은 작업

매개변수는 `unverified`입니다. 온도, 압력, 분해 가능한 체적 한계가 전체 주기에 적용됩니다. 비압축 액체, 이상 의사 성분 증기, 순간 공통 온도 평형 및 외부 케이스 압력 0을 가정합니다. 국소 입구 관성/NPSH, 초킹, 핵생성 지연, 밸브 관성/이력, 실제 혼합 연료 및 실측 보정은 남습니다.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ko.md)

연료 도메인 펌프는 이제 물리 니들과 증기 질량분율 동결 토출을 통해 유한 액체 체적 연소 수용실에 공급합니다. 전체 재고에는 상변화, 배제 일, 제트 운동에너지 및 정지 벽 충격량이 포함됩니다. [증기 질량분율 동결 연료 토출과 유한 수용실](FUEL_DISCHARGE.ko.md)
