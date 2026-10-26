# Referência de volume de combustível complacente

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · **Português**

## Escopo constitutivo

`CompliantFuelVolume` fornece o fechamento constitutivo da rede de combustível em equilíbrio. Resolve líquido, vapor e coexistência com armazenamento elástico por massa total e temperatura ou energia. O caminho de injetor alimentado por bomba ainda exige ampliar o acoplamento. [Rede de transporte de combustível em equilíbrio](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Termodinâmica

Densidade e calor específico do líquido são constantes; o vapor é uma pseudoespécie ideal. As fases compartilham a referência material do filme existente. O potencial químico líquido inclui `p/rho_l`. Portanto, `p_0(T)` é a referência com deslocamento desprezível, não a pressão de saturação do fluido puro com densidade líquida finita. A parede tem pressão externa zero e armazena o trabalho de pressão uma vez.

`StoredEnergyJoules` contém energia calórica e elástica; a energia química do poder calorífico mantém um balanço separado na conexão ao grafo.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

A coexistência seleciona a raiz de vapor de baixa densidade. Sua entalpia latente é `L_u + R T - p/rho_l`; a derivada da pressão satisfaz Clapeyron. Com massa fixa, a capacidade térmica deriva energia do fluido mais elasticidade, incluindo transferência de fase. A parede não carrega entropia.

## API Core

`Evaluate` recebe temperatura e massa total. `Flash` recebe massa e energia armazenada do fluido mais elasticidade. `AdvanceHeat` troca calor com banho finito por condutância declarada e duração local em segundos, usando Euler implícito. Os nomes indicam unidades SI. Essas chamadas locais não substituem o relógio inteiro limitado de `Simulation`.

`FuelVolumeStatus` distingue entrada inválida, limites de temperatura, massa e energia e falha do solver; saídas rejeitadas têm valor padrão. `Flash` preserva a energia pedida e informa o erro calórico de reconstrução em ponto flutuante em `CaloricResidualJoules`. As instâncias não guardam estado evolutivo e chamadas bem-sucedidas não alocam memória gerenciada.

## Validade

Declarar volume de referência, complacência, densidade líquida e pressão máxima positivos. O limite cobre toda a curva de coexistência e fica abaixo de `rho_l R T_min`. A expansão de coexistência na temperatura mínima deve ser ao menos `128 * epsilon` do volume de referência, com `epsilon = 2^-52`. Exclui deformação não resolvida e o ramo de vapor denso fora do modelo declarado; não prevê um ponto crítico.

Respeitar o intervalo térmico material e `0 < M <= rho_l (V_0 + C p_max)`. Vácuo, gás portador, misturas reais, expansão térmica, inércia da parede, tensão superficial e cinética de fase finita ficam fora. Parâmetros permanecem `unverified`. Enchimento de bomba por eixo, conexão de injetor, quantidade de movimento e escoamento crítico exigem mais acoplamento e evidências independentes.

`AdiabaticReferenceVolume` move o mesmo material imutável entre volumes explícitos com massa e entropia fixas. A mudança de energia inclui elasticidade e é o trabalho mecânico. Avaliação variável, flash e banho finito respeitam os limites declarados. [Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

## Evidências independentes

Os testes comparam potenciais químicos, inclinações de Clapeyron, derivadas calóricas e entrópicas e Gibbs com armazenamento elástico. A maximização independente da entropia restrita não usa o solver de saturação. Limites de recipiente rígido, gás ideal e escala extensiva e refinamento RK4 do banho finito fornecem referências independentes. A troca preserva energia total e refina em primeira ordem com entropia total não decrescente. Não comprova propriedades medidas de combustível nem aceitação Unity.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.pt-BR.md)
