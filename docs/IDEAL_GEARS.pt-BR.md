# Referências de engrenagem ideal e planetária

[English](IDEAL_GEARS.md) · [简体中文](IDEAL_GEARS.zh-CN.md) · [Français](IDEAL_GEARS.fr.md) · [Русский](IDEAL_GEARS.ru.md) · [日本語](IDEAL_GEARS.ja.md) · [한국어](IDEAL_GEARS.ko.md) · [Deutsch](IDEAL_GEARS.de.md) · [Español](IDEAL_GEARS.es.md) · [Italiano](IDEAL_GEARS.it.md) · **Português**

`Power.Core` fornece duas referências de carga constante, imutáveis e sem alocação: `IdealGearPair` e `SimplePlanetaryGear`. Elas devolvem velocidades dos membros, avanços angulares, torques de reação, trabalho externo, variação de energia cinética e um resíduo de energia. Fornecem evidência analítica independente para o solver de transmissão acoplado. O [solver de engrenagens acoplado](GEAR_NETWORK.pt-BR.md), separado, agora expõe componentes permanentes de engrenagem e planetária por JSON, assets portáteis e CLI/MCP, inclusive experimentos de troca comandados por embreagem. As classes de referência continuam soluções analíticas locais puras.

## Escopo físico e sinais

Uma engrenagem ideal não tem inércia de engrenamento, flexibilidade, folga de engrenamento nem perdas; todas as inércias fornecidas são inércias de rotor acopladas. As duas inércias de um par, ou os três membros de uma planetária, precisam ser positivas e finitas. O solo e os nós sem massa não são inferidos de inércia nula. A abstração segue o escopo de [IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html) e [IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html) da Modelica Standard Library. A implementação do Power! é escrita de forma independente; nenhuma implementação de terceiros é incluída ou chamada.

Para um par, a relação com sinal `r` define:

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

Relações positivas dão a mesma direção de porta; relações negativas a invertem. As reações são torques **sobre os rotores acoplados**, não os torques que os rotores aplicam à engrenagem. Elas realizam trabalho líquido zero para movimento compatível. A carcaça de um par de engrenagens pode carregar uma reação; o momento angular ordinário dos dois rotores, sozinho, não se conserva em geral. O momento generalizado `r J_A omega_A + J_B omega_B` muda com o torque externo generalizado `r T_A + T_B`.

Para a planetária simples, sol, coroa e porta-planetas compartilham um eixo positivo. A relação de dentes `k = N_ring / N_sun` precisa ser maior que um. A relação cinemática e os dois graus de liberdade independentes concordam com as [equações de engrenagem planetária da MathWorks](https://www.mathworks.com/help/sdl/ref/planetarygear.html).

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

Essas reações somam zero e realizam trabalho líquido zero. O modelo aceita uma relação contínua, sem inferir número de dentes, módulo, resistência do dente nem geometria fabricável. Inércia de giro e orbital dos planetas, perdas, mancais, lubrificação e comportamento térmico ficam fora desta referência.

## Solução independente em coordenadas reduzidas

O movimento do par usa a porta B como coordenada independente:

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

A planetária elimina o movimento do porta-planetas antes de formar a matriz de massa da energia cinética. Com `a = 1/(1+k)` e `b = k/(1+k)`:

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

A implementação escala essa matriz dois por dois e expande o determinante em termos positivos para evitar subtrair produtos quase iguais. Para cargas constantes, a aceleração é constante, então velocidade e deslocamento seguem integração temporal linear/quadrática exata, até o arredondamento de ponto flutuante. Os torques de reação são então recuperados das equações dos membros. Os testes usam um multiplicador independente de restrição de aceleração para a planetária livre; não reutilizam a matriz reduzida como solução esperada.

## Contrato de estado, unidades e falha

Os nomes públicos das propriedades carregam unidades SI: kg m2, rad/s, rad, N m, J e segundos. As relações são adimensionais. `Advance` recebe uma duração local finita e positiva e devolve `GearStepStatus`. Essa duração local de referência não substitui o relógio inteiro limitado de `Simulation`. As classes não guardam estado em evolução. As entradas são registros valor; a saída é default em toda rejeição, e instâncias podem ser compartilhadas por chamadas independentes.

As velocidades iniciais já precisam satisfazer a relação. A compatibilidade usa um teste de arredondamento relativo com o epsilon de binary64 `2.2204460492503131e-16`, sem banda morta absoluta de baixa velocidade. Para um par, o limite é `64 epsilon (|omega_A| + |r omega_B|)`. A planetária inclui ainda as magnitudes dos dois termos de velocidade ponderados, de modo que o cancelamento é tratado em relação às operações que formaram a velocidade do porta-planetas. Os termos são escalados antes da soma para evitar estourar a tolerância.

Depois da validação, a velocidade dependente e o avanço angular são reconstruídos a partir de coordenadas independentes. Isso remove o resíduo de arredondamento aceito; não é um cálculo de engate com deslizamento finito nem de sincronização. Velocidades incompatíveis devolvem `IncompatibleState`. Use um modelo explícito de embreagem ou de impacto para um desacordo real de velocidade, em vez de descartar a energia dele. A fase absoluta da engrenagem não é especificada: só se informam avanços angulares.

Parâmetros de construção inválidos lançam exceções de argumento acionáveis. Combinações de parâmetros não finitas ou mal condicionadas são rejeitadas; o determinante planetário escalado precisa exceder `64 epsilon`. A rejeição do intervalo distingue duração inválida, estado inválido, estado incompatível, torque inválido e falha numérica. Overflow aritmético devolve `NumericalFailure`; entradas finitas sozinhas não garantem quantidades derivadas representáveis. Uma verificação de equilíbrio de forças em tempo de execução também rejeita cancelamento que deixe reações dos membros finitas, mas inconsistentes: cada resíduo de força é limitado por `512 epsilon` vezes a soma das magnitudes dos torques inercial, aplicado e de reação. O extremo aceito também confere o balanço de impulso de cada membro, usando `512 epsilon` vezes as magnitudes do momento antigo/novo e dos impulsos aplicado/de reação. Esta última detecta cancelamento excessivo na reconstrução da velocidade dependente. Essas verificações limitam resíduos, não o erro da solução para parâmetros arbitrários mal condicionados. Os testes incluem uma falha finita de cancelamento e um par de relação alta cuja reação pequena precisa continuar observável. O resíduo é `external_work - kinetic_energy_change`; nenhum calor de atrito é fabricado.

## Estados de transmissão e evidência

Os testes fornecem de forma explícita torques de retenção ou de trava para estabelecer estes limites ideais:

| Condição imposta | Relação de velocidade resultante |
|---|---|
| Coroa retida | `omega_C = omega_S / (1+k)` |
| Sol retido | `omega_C = k omega_R / (1+k)` |
| Porta-planetas retido | `omega_S = -k omega_R` |
| Sol travado à coroa | As três velocidades dos membros são iguais |

O freio fornecido realiza trabalho zero quando o membro está retido; uma trava sol/coroa recebe torques opostos com trabalho combinado zero. Essas verificações estabelecem estados estáticos de transmissão. Esta referência não implementa troca, engate de embreagem, circuito hidráulico nem TCU, e torques externos arbitrários não retêm um membro automaticamente.

Os mesmos oito grupos de teste rodam contra `net10.0` e `netstandard2.1`:

- Relações com sinal e inércia refletida; potência de reação e balanço de impulso por membro.
- Movimento planetário livre contra uma solução independente de multiplicador de força.
- Três casos de membro retido e direta, com cargas explícitas de retenção/trava.
- Invariância de partição sob carga constante e reversão de velocidade através de zero.
- Carga senoidal contra integrais independentes para as duas referências; reduzir o intervalo pela metade dá redução aproximadamente quádrupla do erro de velocidade/ângulo.
- Valores inválidos de inércia/relação/estado/carga, velocidades incompatíveis, mau condicionamento e overflow.
- 2,500 casos determinísticos para cada referência, conferindo trabalho, momento e reprodutibilidade.
- 10,000 avaliações repetidas de cada primitivo com alocação gerenciada zero, mais uso imutável compartilhado por chamadas concorrentes independentes.

Veja a [validação](VALIDATION.pt-BR.md) para o resultado completo da verificação em série. Os testes de assembly Standard rodam em .NET 10 e não fornecem evidência de Unity Editor/Play/IL2CPP.

## Integração acoplada

As restrições permanentes agora participam da solução eletromecânica/cilindro/embreagem, com espaços de trabalho de simulação independentes, rollback completo e canais estáveis de reação/erro. JSON/esquema, asset v8 com leitores anteriores, descoberta MCP e replay usam a mesma topologia. O experimento planetário em combustão faz subida redução/direta e uma descida. Veja [o contrato acoplado](GEAR_NETWORK.pt-BR.md) para equações e evidência. Topologia DCT/AT completa, conversor, hidráulica, controles, comportamento completo do motor e calibração medida de veículo continuam parte do objetivo completo do Power!.
