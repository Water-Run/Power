# 상평형 연료 수송 네트워크

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · **한국어** · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## 모델 범위

`fuel` 도메인은 유한 탄성 용기에서 순수 연료 액체와 증기를 함께 수송합니다. 각 용기는 총질량과 열량·탄성 총에너지를 독립적으로 유지하며 상평형 플래시가 온도, 압력, 증기 비율을 결정합니다. `fuel_restriction`은 균질 공급 유체를 교환하고 `fuel_heat_link`는 유한 열 노드와 열을 교환합니다. 발열량에 해당하는 화학 에너지는 별도 장부입니다.

연료 노드의 `storage`는 `m3` 또는 `l` 단위 기준 체적, `initial`은 `k` 단위 온도, `position`은 `kg` 단위 총질량입니다. `fuel` 객체에 물성, 컴플라이언스, 압력/온도 한계를 명시합니다. 연결 용기는 밀도, 열량 및 상 기준, 온도 한계, 발열량을 공유합니다. 저항 양단은 유한 연료 노드이며 열 링크는 연료와 열 노드를 연결합니다. 개도는 [0,1] 비율이고 입력 채널은 선택 사항입니다.

## 보존적 흐름

저항은 `m3_s_pa` 단위의 명시적 전도도 K를 갖는 준정적 소산 기준 체적 법칙입니다. 링크 관성, 운동에너지 저장, 상간 미끄럼을 무시합니다. 공급 측 밀도가 기준 체적 흐름을 총질량 흐름으로 바꾸며 공급 측 엔탈피가 두 상을 운반합니다. 추가 외부 압력 일이나 압력 손실에 대한 별도 열 항은 없습니다. 탄성 일은 이미 용기에 저장됩니다.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## 시간 적분과 엔트로피

제한된 후진 Euler가 두 용기에 동일한 승인 질량 q와 엔탈피 H를 구합니다. 링크는 안정된 구성요소 ID 순서로 진행하며 유한 열욕은 별도 보존 부분 단계를 사용합니다. 결합은 1차입니다. 질량·온도·압력 한계 위반이나 해 실패는 입력, 보상 이력, 클러치 시험 구간을 포함한 전체 배치를 거부합니다.

호환 물성과 양의 압력 구동 전도도는 연속 쌍 엔트로피 생성을 비음수로 만듭니다. 후진 Euler는 수치 엔트로피를 더합니다. `entropy_produced`는 혼합과 시간 이산화를 포함한 승인된 전체 쌍 엔트로피 변화이며 실측 손실 맵이 아닙니다. 보존 및 세분화와 함께 독립 엔트로피 검사를 수행합니다.

## 사용과 근거

MCP에서 `equilibrium-fuel-network` 또는 `electrothermal-fuel-network`를 요청하거나 동일 JSON을 CLI로 실행합니다. 용기별 `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy`, `specific_enthalpy`를 읽습니다. 여기서 `internal_energy`는 탄성 벽을 포함하며 화학 에너지는 별도입니다. 링크는 부호 있는 흐름, 누적 질량, `transport_energy`, 쌍 엔트로피 변화를 공개합니다.

실험은 공존 상태로 이동하는 액체 배출, 역류, 개도 변경, 유한 벽/전기기계 열 결합을 다룹니다. 독립 액체 압력 지수해, 동시 2상 질량/엔탈피/열 ODE, 열 구동 역류, 클러치 시험 포착으로 정확도와 조합을 검사합니다. 자산과 실제 MCP가 모든 샘플 값과 해시를 유지합니다. 자산 v33는 물성을 저장하고 v1-v32을 읽습니다. 실제 Unity Editor/Play/Player/IL2CPP는 미검증입니다.

## 남은 작업

연료 수송은 축 구동 `fuel_chamber` 및 이상 `fuel_check_valve`와 결합합니다. 물리 인젝터, 국소 관성/초킹, 실측 혼합물은 별도 작업입니다. 매개변수는 `unverified`입니다. [축 구동 상변화 연료 펌프](FUEL_CHAMBER_PUMP.ko.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.ko.md) · [VALIDATION.md](VALIDATION.ko.md)
