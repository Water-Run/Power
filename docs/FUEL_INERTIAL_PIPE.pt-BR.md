# Quantidade de movimento da linha finita de combustível líquido

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · **Português**

## Contrato físico

Um `fuel_inertial_pipe` conecta dois nós finitos de combustível e uma parede térmica finita. A linha horizontal rígida possui massa líquida, energia calórica e cinética próprias além dos vasos. Densidade constante e temperatura variável perfeitamente misturada. Portas e linha ficam líquidas; conexões quase estáticas mantêm seu modelo bifásico separado.

Declare comprimento, área, resistências linear/quadrática, vazão inicial, temperatura inicial e `fluid_heat_fraction`. As unidades são `pa_s_m3` e `pa_s2_m6`. Material e densidade coincidem. Não existe abertura direta: fechar outra válvula não apaga o movimento. Referências material e química vêm das portas.

## Energia e portas

A entalpia doadora sai do vaso a montante. A linha mistura energia sensível com inventário próprio e fornece entalpia misturada à pressão a jusante. O trabalho muda energia cinética e fornece calor de atrito uma única vez. A fração declarada aquece líquido, o resto a parede finita. A reversão troca doador/receptor com os mesmos balanços.

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

O movimento usa vazão média e pressões finais aceitas. Energia e mistura usam Euler implícito; o acoplamento completo converge em primeira ordem. Mesmo sem atrito, a discretização transforma parte da oscilação em calor numérico, não registrado como atrito físico. Entropia inclui mistura e discretização. Eventos de vazão zero separam transferências diretas e reversas.

## Uso e evidências

Execute `inertial-fuel-line` para reversão/mistura, ou `inertial-pump-fed-cylinder` para entrada, bomba, agulha eletromagnética física e combustão prescrita. Leia `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, inventários e balanços. Vazão volumétrica instantânea e mássica média do último passo. A linha adiciona massa e energia química próprias.

Solução harmônica independente com flexibilidade finita e RK4 independente de movimento/temperaturas não lineares verificam exatidão e primeira ordem. Cobrem calor, entropia, reversão, balanços, intervalos especulativos de embreagem, alocações, rollback, cancelamento e ramos. Assets portáteis e MCP real verificam cada fronteira. Isso não comprova calibração ou Unity real.

## Validade e próximos trabalhos

Os parâmetros seguem `unverified`. Respeite intervalos materiais e estabilidade líquida corrigida pela pressão. Falhas revertem todo o intervalo; reduza passo e confira esgotamento, pressão e fase. Faltam movimento bifásico com densidade variável, cavitação, acústica, gravidade, geometria móvel e frentes térmicas. Combustão prescrita não prova química de equilíbrio ou entropia de reação detalhadas.

Estender movimento a densidade variável e mudança de fase com fechamento energético/entrópico compatível antes de afirmar entrada cavitante. Continuar relaxação/deslizamento, ignição, admissão/escape, perdas, transmissão e ECU/TCU. Calibração medida e Unity real seguem separados.

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
