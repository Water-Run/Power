# Pistão hidráulico e embreagem acionada por contato

[English](HYDRAULIC_PISTON.md) · [简体中文](HYDRAULIC_PISTON.zh-CN.md) · [Français](HYDRAULIC_PISTON.fr.md) · [Русский](HYDRAULIC_PISTON.ru.md) · [日本語](HYDRAULIC_PISTON.ja.md) · [한국어](HYDRAULIC_PISTON.ko.md) · [Deutsch](HYDRAULIC_PISTON.de.md) · [Español](HYDRAULIC_PISTON.es.md) · [Italiano](HYDRAULIC_PISTON.it.md) · **Português**

`hydraulic_piston` liga uma massa em translação a uma câmara hidráulica frontal e a uma câmara traseira ou a um reservatório explícito de contrapressão. `piston_clutch` lê a força de pastilha do pistão. Pressão positiva pode mover um pistão através da folga livre sem transmitir torque de embreagem.

## Equações e energia

Para deslocamento x, velocidade v, áreas efetivas frontal/traseira Af/Ab e pressões manométricas pf/pb, a força do pistão é `Af*pf - Ab*pb`. A expansão frontal aspira `Af*dx`; a contração traseira entrega `Ab*dx`. Uma câmara finita armazena `C*p*p/2` joules e `C*p` metros cúbicos de inventário de referência. O livro de volume inclui o volume varrido do pistão `(Af-Ab)*(x-x_initial)`. Um reservatório traseiro contribui trabalho externo com sinal `-pb*Ab*dx` e volume de referência `-Ab*dx`.

O nó em translação armazena `m*v*v/2`. Uma `linear_spring` acrescenta `K*(x-rest)^2/2` e perda viscosa `D*v_relative^2`, com encaminhamento térmico explícito. O canal `friction_heat` informa o calor de amortecimento acumulado por soma compensada. É independente das temperaturas arredondadas do nó térmico.

O potencial unilateral da pastilha é `Kpad*max(x-contact,0)^2/2`. Os batentes inferior e superior acrescentam o mesmo potencial quadrático fora do curso nominal. Os batentes são flexíveis: a penetração armazena energia e produz uma força de restauração. Eles não limitam o movimento. Para um potencial em dobradiça V, a reação do intervalo usa `-(V(x_next)-V(x_old))/dx`, avaliada por um gradiente discreto resistente a cancelamento. Em consequência, o trabalho de contato é exatamente a variação de potencial nas equações discretas. O jacobiano analítico cobre dobradiças ativas, inativas, em ativação e em liberação; numa dobradiça estacionária usa a derivada unilateral média.

As capacidades estática/de deslizamento da embreagem são `mu*surfaces*radius*Npad`. O solver usa a força discreta da pastilha durante o intervalo e a força instantânea da pastilha nos instantâneos. O calor de atrito permanece não negativo; uma embreagem ideal travada não dissipa potência de deslizamento. A mesma solução conjunta inclui pressão, inércia do pistão, amortecimento da mola, alimentação elétrica e as restrições mecânicas/de embreagem existentes.

## Contratos e limites numéricos

| Elemento | Dados exigidos |
|---|---|
| Nó `translational` | Massa positiva em kg, velocidade inicial em m/s, posição em m |
| `hydraulic_piston` | Porta A em translação, porta hidráulica frontal B; áreas frontal/traseira, nó/pressão traseiros, limites de curso crescentes, rigidez de batente, posição/rigidez de contato |
| `linear_spring` | Portas A/B em translação ou B aterrado; rigidez N/m, amortecimento N·s/m, deslocamento de repouso m, sumidouro térmico opcional |
| `piston_clutch` | Portas rotacionais A/B ou B aterrado, ID do componente de pistão, raio m, coeficientes estático/de deslizamento, número inteiro de superfícies de atrito |
| `force_source` | Porta A em translação e entrada de força externa em N |

`linear_spring.parameters.rest_angle` conserva a chave compartilhada do descritor, mas carrega uma grandeza de deslocamento em metros. Um pistão é dono de um dado nó em translação; vários elementos de atrito de embreagem podem referenciar a pastilha dele de forma explícita. Uma câmara traseira finita precisa diferir da câmara frontal. Um reservatório tem `back_node=0` e uma `back_pressure` explícita não negativa. O atrito estático precisa ser pelo menos o de deslizamento. As superfícies ficam em 1–128 e o contato da pastilha fica dentro do curso nominal.

O curso aceito do pistão por intervalo é limitado a um quarto do curso nominal. Reduza o tick fixo se o movimento violar esse limite ou se a solução conjunta não convergir. Pressão manométrica aceita negativa rejeita o lote completo; inspecione escoamento de alimentação, flexibilidade, áreas efetivas, inércia e amortecimento. Este modelo não tem grampo de cavitação. Rollback completo, cancelamento, ramificações e hashes de estado incluem os históricos de movimento, pressão, atrito e amortecimento.

As equações assumem flexibilidade e áreas efetivas constantes, uma massa móvel concentrada, mola/amortecimento de retorno lineares, uma pastilha elástica e extremidades flexíveis. Atrito de vedação, cavitação, modos de deformação dos pratos, desgaste, mapas detalhados de atrito e calibração OEM ficam fora desta implementação.

## Verificação e laboratório

Verificações independentes cobrem oscilação analítica acoplada massa/mola/fluido, câmaras traseiras finitas e de reservatório, volume varrido e trabalho de pressão, identidades de trabalho de dobradiça, derivadas analíticas de contato e uma referência de contato RK4 por trechos. O movimento linear suave mostra refinamento de segunda ordem; os testes de contato não suave conferem erro decrescente sem reivindicar ordem híbrida uniforme. As verificações da embreagem cobrem enchimento livre, contato da pastilha, captura, liberação e calor de atrito. Falha numérica tardia, cancelamento, independência de ramificações, lotes exatos e avanço sem alocação são verificados.

O [laboratório sintético de embreagem acionada por pistão](../assets/labs/piston-actuated-clutch.power.json) usa uma bateria finita, bomba elétrica regulada por duty, válvulas de enchimento/dreno, um pistão de 20 g, folga de pastilha de 2 mm, uma mola de retorno de 10 kN/m e amortecimento de 300 N·s/m. O amortecimento é um parâmetro explícito de pesquisa, escolhido para manter não negativa a câmara fornecida durante o transitório. Não é uma medição OEM. Em 15 s a carga da pastilha é cerca de 177.28 N, com capacidades estática/de deslizamento de 22.69/11.35 N·m. CLI, portátil e replay MCP real concordam em cada fronteira informada.

Veja [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) para limites numéricos, livros de energia separados, medições de desempenho e escopo de runtime. O asset v14 retém a topologia completa. Vistas de deslizador/contato do Studio e testes de importação/Play estão preparados; a evidência real de Unity Editor e Player continua pendente.
