# Dosagem mecânica por carretel e regulação de pressão

[English](HYDRAULIC_SPOOL.md) · [简体中文](HYDRAULIC_SPOOL.zh-CN.md) · [Français](HYDRAULIC_SPOOL.fr.md) · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · [Deutsch](HYDRAULIC_SPOOL.de.md) · [Español](HYDRAULIC_SPOOL.es.md) · [Italiano](HYDRAULIC_SPOOL.it.md) · **Português**

`hydraulic_spool_valve` dosa uma porta hidráulica a partir do deslocamento real de um `hydraulic_piston` explícito. O pistão fornece massa, volume de fluido varrido, força de pressão e extremidades de curso flexíveis; uma `linear_spring` separada fornece força de retorno, pré-carga e amortecimento. Vários ressaltos de dosagem podem referenciar o mesmo pistão.

## Equações e limites

As posições fechada e toda aberta definem um curso com sinal L. Com deslocamento x:

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K é um coeficiente explícito de toda aberta em m3/(s*sqrt(Pa)); a pressão de transição é positiva. A implementação escala o denominador para evitar elevar ao quadrado diferenças de pressão enormes. O curso com sinal suporta os dois sentidos de abertura. Um ressalto fechado veda exatamente; o vazamento precisa de um caminho adicional explícito. Só a abertura satura: posição, pressão, velocidade e energia armazenada não são limitadas.

O ressalto é equilibrado em pressão, com a força de jato axial desprezada. A diferença de pressão da porta de dosagem não aplica uma força axial extra ao pistão. As pressões das câmaras frontal/traseira do atuador explícito fornecem a força de acionamento. O calor da restrição e o trabalho do pistão/mola usam os livros conservativos existentes. Este modelo exclui atrito de vedação, efeitos de momento da força de escoamento, cavitação, desgaste e geometria/viscosidade dependentes da temperatura. É uma redução de pesquisa declarada, não uma válvula calibrada.

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html) documenta área de abertura variável e uma opção separada de força axial de escoamento. O Power! usa o próprio ressalto linear normalizado e a lei de restrição passiva existente; nenhuma geometria, padrão de propriedade de fluido ou código de implementação foi copiado.

## Solução compartilhada e contratos

A válvula lê `x_old + dx/2` na mesma solução de Newton conjunta das pressões do fluido, da força do pistão e das restrições mecânicas. As derivadas analíticas incluem tanto a pressão quanto o deslocamento do ressalto. Dentro do curso de dosagem, `dOpening/dx=1/L`; fora dele a derivada é zero. Em cada extremo, o jacobiano usa a inclinação unilateral média. Isso preserva um laço de realimentação simultâneo, em vez de um comando de abertura atrasado.

| Parâmetro | Significado |
|---|---|
| `piston_component` | ID estável de um pistão hidráulico explícito |
| `closed_position`, `full_open_position` | Posições distintas em m ou mm, ambas dentro do curso nominal do pistão |
| `coefficient` | Coeficiente não negativo de toda aberta em `m3_s_sqrt_pa` |
| `transition_pressure` | Pressão positiva de regularização em Pa ou bar |
| `reservoir_pressure` | Fronteira de pressão manométrica exigida quando o hidráulico B é omitido/zero |

As portas hidráulicas A/B e um sumidouro térmico opcional seguem o contrato da restrição. A válvula não tem `input_channel` nem `initial_input`; observe o canal `opening` e comande o circuito real do atuador. Escoamento/potência médios e calor hidráulico acumulado são observáveis. Unidades, tipo do componente referenciado e limites de curso produzem erros de validação acionáveis. Os contratos ordinários de rollback do lote inteiro, cancelamento, ramificação, relógio inteiro e replay exato no mesmo runtime incluem todos os estados e históricos. As impressões digitais dos modelos físicos existentes permanecem inalteradas.

O asset v15 acrescenta um registro de 32 bytes de geometria de dosagem. JSON, CLI e MCP conservam as mesmas definições. `get_example_model("spool-regulated-pump")` demonstra uma bomba elétrica, um desvio governado mecanicamente e enchimento/dreno programado da embreagem de pressão. A pré-carga estática de fechamento de 200 N vem de uma mola de retorno de 200 kN/m a 1 mm de compressão e de uma área explícita de atuador de 1000 mm2. O acionamento/freio rotacional é de 2 N*m; um experimento de três segundos deixa tempo suficiente para a embreagem de pressão mais baixa capturar. Os parâmetros são sintéticos e não verificados.

## Evidência e desempenho

As verificações cobrem curso de dosagem com sinal, escoamento bidirecional passivo, derivadas analíticas de pressão/posição, uma raiz independente de pressão em regime, um transitório RK4 separado de três estados, refinamento suave de segunda ordem, erro decrescente através da abertura do ressalto, equalização de porta finita, volume varrido, energia independente de movimento/fluido e transações completas. O avanço aquecido e as leituras de instantâneo alocam zero bytes gerenciados. Inclinações de ressalto e buffers de Newton/LU pertencem a cada simulação; nenhum relógio nem trabalhador novo é acrescentado.

Veja [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) para erros medidos, escopo de runtime e tempo decorrido. Vistas de válvula/atuador do Studio e testes de importação/Play estão preparados em fonte C# 9; a evidência real de Unity Editor e Player continua pendente.
