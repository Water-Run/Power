# Movimento resolvido dos planetas Ravigneaux

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · **Português**

O conjunto resolvido inclui o giro absoluto dos dois conjuntos internos de planetas e a inércia de massa orbital deles em torno do porta-planetas. Quatro restrições físicas de engrenamento ligam seis rotores. As cinco embreagens/freios de faixa e o conversor externo continuam componentes ordinários. A redução de quatro membros continua disponível como simplificação declarada separada; ela não fornece evidência de giro dos planetas.

A conectividade de engrenamento e as relações primitivas têm uma [referência estrutural](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html) separada. O Power! deriva e implementa o próprio grafo conservativo de rotores e verificações independentes de matriz de massa. Nenhuma implementação nem pacote de modelo de fornecedor é incluído.

## Geometria e energia

Para o raio primitivo da coroa `R` e as relações de sol grande/pequeno `kL` e `kS`, a geometria primitiva rígida é:

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` exige um raio de coroa no SI, massas e inércias de giro positivas por planeta, e de 1 a 32 pares de planetas iguais e síncronos. O espaçamento uniforme precisa acomodar os dois conjuntos sem sobreposição dos círculos primitivos. A geometria e a inércia agregada precisam permanecer representáveis. Todas essas entradas são propriedades explícitas de pesquisa; o auxiliar não fornece valores medidos.

Para `n` pares iguais, o porta-planetas recebe a inércia orbital `n (mInner orbitInner^2 + mOuter orbitOuter^2)`. O parâmetro existente `CarrierInertia` é a inércia da estrutura do porta-planetas neste caminho resolvido. Cada rotor novo tem `n` vezes a inércia de giro por planeta. As velocidades deles são velocidades angulares absolutas, então a energia cinética é o `J omega^2/2` ordinário; a corrotação conserva a energia de giro dos planetas. Usar giro relativo com este armazenamento diagonal omitiria o acoplamento do porta-planetas.

## Contrato de engrenamento

`carrier_gear` impõe `A - ratio B + (ratio-1) C = 0`, em que C é o porta-planetas que de fato se move. Engrenamentos externos usam uma relação negativa de raios primitivos; o engrenamento interno coroa/planeta externo usa uma relação positiva. Relações finitas, diferentes de zero e com sinal, inclusive um, são suportadas. São exigidas três portas rotacionais distintas e velocidades iniciais compatíveis.

Os quatro engrenamentos são sol-grande/planeta-externo, sol-pequeno/planeta-interno, coroa/planeta-externo e planeta-interno/planeta-externo. Os três torques de reação entram na mesma projeção de ponto médio e têm potência de porta somada zero e soma de torque zero. A reação do porta-planetas não é enviada em silêncio ao solo estacionário. Um refinamento limitado do resíduo relativo melhora respostas de força pequenas. A solução de ponto médio impõe resíduo de velocidade nulo no extremo seguinte, evitando a reflexão repetida do arredondamento precedente. As duas operações usam respostas reais de força de restrição e retêm os multiplicadores de correção nos históricos reais de reação. O rascunho pertence a cada simulação; os fatores compilados permanecem imutáveis. Linhas normalizadas, coordenadas compensadas e históricos completos de reação preservam fase, ramificações, cancelamento e rollback do lote.

A referência livre independente usa coordenadas de coroa/porta-planetas. Com `aOuter = R/outerRadius` e `aInner = R/innerRadius`:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

Isso inclui as duas energias de giro e a inércia orbital acrescentada à parte. Cargas generalizadas independentes, inércias refletidas de todos os caminhos à frente/ré, momento angular e impulso/calor de captura do porta-planetas conferem a solução montada.

## Grafo compartilhado e evidência

`CreateResolvedGraph` recebe as portas originais, quatro IDs distintos de nó/engrenamento de planeta e as propriedades declaradas dos planetas. Devolve definições ordinárias imutáveis: seis rotores internos, quatro engrenamentos de porta-planetas, uma redução final e cinco elementos de atrito. O JSON plano retém as inércias totais dos rotores e as relações de engrenamento com sinal; a descrição do exemplo registra a geometria geradora e as propriedades por planeta. Os digests de fonte preservam essa evidência de autoria declarada.

`resolved-ravigneaux-transmission` exercita todas as entregas à frente subindo/descendo. `fired-resolved-ravigneaux-converter` acrescenta o motor, os mapas de conversor com sinal e a trava. Os dois declaram três pares, R=0.1 m, massas interna/externa 0.3/1 kg e inércias de giro por planeta 0.000015/0.0005 kg m2. A estrutura do porta-planetas é 0.03 kg m2; a adição explícita de órbita é 0.0184375 kg m2. São entradas de pesquisa.

O asset portátil v24 retém o engrenamento de porta-planetas com sinal e lê versões anteriores. O primitivo acrescenta a etiqueta de impressão digital 28; grafos anteriores conservam as impressões digitais e o replay. Marcadores de Studio preparados identificam as três portas de engrenamento. A verificação real de Unity Editor/Play/Player/IL2CPP continua separada. Execute a verificação em série exigida `dotnet run --file tools/Build.cs -- verify`; os desfechos numéricos e o escopo estão em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md).

## Comportamento restante

Conjuntos rígidos iguais e síncronos de planetas não modelam flexibilidade de dente, repartição de carga de fabricação, folga, perdas de engrenamento, lubrificação nem propriedades dependentes da temperatura. O [acionamento por pistão hidráulico alimentado por bomba](AT_HYDRAULIC_ACTUATION.pt-BR.md) está disponível. Controle completo de troca, coordenação da ECU e geometria/mapas OEM medidos continuam inacabados. O conjunto genérico não prova a identidade PSA AT8/AL4. Os limites das amostras e as medições ausentes permanecem intactos.
