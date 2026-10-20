# Bomba de combustível com mudança de fase acionada por eixo

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · **Português**

## Contrato físico

`fuel_chamber` acopla um eixo real a uma câmara finita de combustível. `fuel_check_valve` de entrada e saída fornecem enchimento e entrega por pressão. Líquido, vapor e energia elástica usam o mesmo fechamento. Com entrada insuficiente a câmara pode vaporizar na sucção; a entrega segue inventário e pressão calculados.

`node_a` é rotacional, `node_b` um nó de combustível com um único proprietário geométrico. Declarar `minimum_reference_volume`, `swept_volume` em `m3` ou `l` e `phase` em `rad` ou `deg`. O volume inicial coincide com a geometria dentro do arredondamento binary64. O mecanismo senoidal não adiciona inércia de pistão/came; a inércia do eixo é explícita.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## Trabalho e portas

O subpasso mecânico fechado mantém massa e entropia. A energia inclui fluido e parede elástica, então o trabalho do eixo usa o volume geométrico de referência. A mesma mudança aceita determina torque no solver mecânico conjunto com engrenagens, conversores, hidráulica e intervalos especulativos da embreagem. A expansão empurra o eixo; `hydraulic_work` positivo entra na câmara. Nenhuma fonte externa adicional de pressão é incluída.

`fuel_check_valve` usa condutância declarada em `m3_s_pa` e abertura fracionária opcional. Fluido doador homogêneo passa somente A→B quando A tem pressão maior. É uma válvula ideal sem massa e com pressão de abertura zero. Mantêm-se os contratos de entalpia conservativa e calor de parede finita da rede.

## Uso e evidências

Pedir `equilibrium-fuel-pump` ou `electric-equilibrium-fuel-pump` pelo MCP ou executar JSON na CLI. Ler massa, temperatura, pressão, vapor e energia junto à entrega de válvulas e velocidade do eixo. `volume` do componente é geométrico; `volume` do nó inclui expansão da parede. `torque`, `hydraulic_power`, `hydraulic_work` mostram a troca interna.

Pressão/trabalho líquidos, adiabáticas de vapor ideal e inversão independente de entropia bifásica verificam o fechamento. O movimento líquido fechado refina em segunda ordem; enchimento/entrega com fase em primeira contra RK4 invertido independentemente. Falta e suprimento completo de entrada, acionamento elétrico, engrenagens/hidráulica/embreagem, replay, rollback e alocações cobrem integração. Asset v33 guarda geometria e lê v1-v32. Unity Editor/Play/Player/IL2CPP real segue não verificado.

## Trabalho restante

Parâmetros permanecem `unverified`. Temperatura, pressão e volume representável são limitados durante todo o ciclo. Assume-se líquido incompressível, vapor ideal de pseudoespécie, equilíbrio instantâneo em temperatura comum e pressão externa de carcaça zero. Inércia local/NPSH, escoamento crítico, atraso de nucleação, inércia/histerese de válvula, misturas reais e calibração medida seguem abertos. Injetor físico e trabalho de deslocamento/cinética do receptor são separados.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.pt-BR.md)
