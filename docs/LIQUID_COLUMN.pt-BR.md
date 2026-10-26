# Referência de quantidade de movimento na entrada líquida

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · **Português**

## Fronteira física

`LiquidColumn` é uma referência C# independente para tubo horizontal rígido com seção, velocidade uniforme, densidade líquida e temperatura constantes. As pressões absolutas são prescritas por intervalo. Resistências linear e quadrática são entradas de pesquisa explícitas. Não é componente do grafo e não altera rede ou bocal.

## Quantidade de movimento e energia

Ambas as portas têm mesma vazão e velocidade; fluxos cinéticos advectados se cancelam. O trabalho de pressão muda a energia cinética armazenada e produz calor de atrito. Um banho isotérmico recebe o calor; temperatura e energia interna ficam constantes. A produção de entropia é calor dividido pela temperatura declarada. Não representa inventário térmico finito.

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

O ponto médio implícito resolve a equação escalar monótona. A reversão se divide no evento de vazão zero com a mesma equação, preservando volumes direto/inverso e calor. A chamada aceita usa até dois intervalos. Ultrapassar o equilíbrio estacionário retorna `StepTooLarge`; reduza duração. Energia e impulso são verificados na escala de arredondamento binary64. Um passo grosso aceito ainda exige verificar exatidão.

## API do núcleo e evidências numéricas

Construa `LiquidColumn` com geometria SI, densidade, resistências, temperatura e pressão absoluta mínima explícitas. Passe `LiquidColumnState` e segundos para `Advance`. As propriedades incluem unidades SI. O resultado contém transporte com sinal e direcional, trabalho, variação cinética, calor e entropia. A rejeição deixa resultado padrão e entrada intacta, sem alocações gerenciadas. A duração local não substitui o relógio inteiro da simulação.

Aceleração sem perdas, exponencial linear e tangente hiperbólica quadrática independentes verificam limites. Uma RK4 independente com pressão variável verifica segunda ordem com reversão. Decaimento passivo, equilíbrio, energia completa, entropia não negativa, rejeições, uso imutável e alocações são testados nos dois alvos.

## Próximo acoplamento

Os parâmetros seguem `unverified`. A pressão mínima é um limite líquido declarado pelo chamador, não previsão de pressão de vapor ou NPSH. Compressibilidade, acústica, gravidade, geometria móvel, armazenamento térmico, cavitação, deslizamento e relaxação ficam fora. Os enlaces bifásicos seguem quase estáticos. Unity real e calibração medida são separados.

Um `fuel_inertial_pipe` conecta dois nós finitos de combustível e uma parede térmica finita. A linha horizontal rígida possui massa líquida, energia calórica e cinética próprias além dos vasos. Densidade constante e temperatura variável perfeitamente misturada. Portas e linha ficam líquidas; conexões quase estáticas mantêm seu modelo bifásico separado. [Quantidade de movimento da linha finita de combustível líquido](FUEL_INERTIAL_PIPE.pt-BR.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
