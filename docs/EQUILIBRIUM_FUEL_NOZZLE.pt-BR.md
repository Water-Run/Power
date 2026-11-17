# Trânsito de combustível em equilíbrio nos bocais

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · **Português**

## Modelo físico

Defina `phase_mode` de `compressible_fuel_nozzle` como `equilibrium` para equilíbrio homogêneo instantâneo durante o trânsito. Líquido e vapor compartilham pressão, temperatura e velocidade; o título muda na isentrópica. Bocal e reservatórios finitos usam o mesmo potencial líquido compressível e referência de vapor ideal. `frozen` permanece uma hipótese separada suportada.

Pressão e entropia específica determinam líquido estável, vapor ou coexistência. O fechamento usa igualdade dos potenciais químicos e limites térmicos e de pressão existentes. Extremos térmicos permitem somente vizinhanças de arredondamento binary64. Área, calor, comando e agulha real seguem o contrato comum. O jato expande idealmente até o receptor; o impulso estacionário não acrescenta força axial nem trabalho mecânico.

## Envoltória global de fluxo

O quadrado positivo do som de equilíbrio implica densidade crescente com pressão a entropia fixa; a queda de entalpia diminui. Os fatores monótonos limitam o fluxo de cada intervalo, incluindo fronteiras de fase e vários extremos. Ramificação e limitação retornam limite inferior amostrado e superior restante com largura relativa até 1e-4. Limites: 512 intervalos na pilha e 8192 divisões. Diferenças calóricas usam margens de arredondamento e limites integrais monótonos com até 256 painéis Riemann e 80 iterações. Casos não resolvidos rejeitam o lote. Máximo interior suave é sônico; na fronteira pode não ser suave.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

Subpassos conservativos de primeira ordem transferem igual massa e entalpia e dividem a energia cinética entre fluido e parede. Verificam-se estados e entropia total do par. Limites: 4096 subpassos, 64 reduções à metade por candidato e transferência inicial de até 0.1 da menor massa. O fechamento permite 128 epsilon binary64 vezes o maior módulo volumétrico ou pressão, sem limitar pressões informadas. Falhas revertem todo o lote. Examine passo, inventário, limites, curso e área.

## Uso e evidências

Execute `equilibrium-flashing-nozzle` ou `equilibrium-pump-needle`. `flux_relative_uncertainty` descreve a última envoltória constitutiva local aceita e não limita fluxo médio, massa entregue, integração temporal de primeira ordem ou calibração. Comparam-se inversão independente de saturação/entropia, bloqueio analítico gás/líquido, busca global densa separada, identidades de energia/entropia e refinamento analítico de esvaziamento de vapor. Inventários, eixo/duto/agulha, reversão, cancelamento, ramos, alocações, assets e MCP real são verificados separadamente. Números e fronteiras anteriores ficam nas evidências de validação.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Validade e próximos passos

Os parâmetros continuam `unverified`. O limite de equilíbrio supõe pseudofluido puro, vapor ideal, expansão líquida nula e velocidade comum. Omite tempos finitos de nucleação/relaxação, deslizamento, spray, gás portador e receptor com combustão. A envoltória usa margens binary64 declaradas, não uma prova formal de intervalos por arredondamento dirigido. A classificação da garganta perto de transições deve ser lida com a envoltória. Validade medida e Unity real permanecem abertos.

Ampliar acoplamentos de contorno compatíveis com taxa finita e obter medições de relaxação do material, depois incluir spray resolvido e receptores com combustão. Continuar ignição, admissão/escape, perdas mecânicas, atuação da transmissão e controle ECU/TCU coordenado. Calibração medida e aceitação real do Unity seguem separadas.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
