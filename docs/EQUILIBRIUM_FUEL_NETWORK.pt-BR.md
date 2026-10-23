# Rede de transporte de combustível em equilíbrio

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · **Português**

## Escopo do modelo

O domínio `fuel` transporta líquido puro e vapor juntos em recipientes elásticos finitos. Cada recipiente mantém massa total e energia calórica mais elástica independentes; o flash determina temperatura, pressão e frações de vapor. `fuel_restriction` troca fluido doador homogêneo e `fuel_heat_link` troca calor com um nó térmico finito. A energia química do poder calorífico mantém um balanço separado.

O nó usa `storage` para volume de referência em `m3` ou `l`, `initial` para temperatura em `k` e `position` para massa total em `kg`. O objeto `fuel` declara propriedades, complacência e limites de pressão/temperatura. Recipientes conectados compartilham densidade, referências calórica e de fase, limites térmicos e poder calorífico. Ambos os portos da restrição são finitos; o elo térmico une combustível e nó térmico. Abertura em [0,1] com canal opcional.

## Escoamento conservativo

A restrição usa uma lei dissipativa quase estática de volume de referência com condutância K explícita em `m3_s_pa`. Despreza inércia do elo, armazenamento cinético e deslizamento entre fases. A densidade doadora converte vazão volumétrica em massa total; a entalpia doadora leva ambas as fases. Não há trabalho externo adicional de pressão nem calor separado para a queda de pressão. O trabalho elástico já está no recipiente.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## Integração temporal e entropia

Euler implícito limitado resolve as mesmas massa q e entalpia H aceitas nos dois recipientes. Elos avançam por ID estável; o banho finito usa outro subpasso conservativo. O acoplamento é de primeira ordem. Massa, temperatura ou pressão fora dos limites e falhas rejeitam todo o lote, incluindo entradas, históricos compensados e intervalos especulativos de embreagem.

Termodinâmica compatível e condutância positiva movida por pressão dão produção contínua de entropia do par não negativa. Euler implícito acrescenta entropia numérica. `entropy_produced` registra a mudança completa aceita, incluindo mistura e discretização; não é um mapa medido de perdas. Testes independentes complementam conservação e refinamento.

## Uso e evidências

Pedir `equilibrium-fuel-network` ou `electrothermal-fuel-network` pelo MCP ou executar o mesmo JSON pela CLI. Ler `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy`, `specific_enthalpy`. Aqui `internal_energy` inclui parede elástica e a energia química é separada. Os elos mostram vazões com sinal, massa acumulada, `transport_energy` e mudança de entropia do par.

Os laboratórios cobrem descarga líquida em coexistência, fluxo reverso, abertura e calor finito/eletromecânico. Exponencial independente de pressão líquida, EDO bifásica simultânea massa/entalpia/calor, reversão térmica e captura especulativa da embreagem verificam precisão e composição. Artefatos e MCP real preservam todos os valores e hashes. Asset v35 guarda propriedades e lê v1-v34. Unity Editor/Play/Player/IL2CPP real segue não verificado.

## Trabalho restante

O transporte agora compõe com `fuel_chamber` por eixo e `fuel_check_valve` ideal. Injetor, inércia/escoamento crítico e misturas reais medidas seguem separados. Os parâmetros são `unverified`. [Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.pt-BR.md) · [VALIDATION.md](VALIDATION.pt-BR.md)
