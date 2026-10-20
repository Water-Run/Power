# Equilíbrio de fases do combustível e limites de suprimento

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · **Português**

## Contrato

A opção `fuel_film.parameters.phase_equilibrium` declara `reference_pressure` em `pa` ou `bar` e `minimum_temperature` / `maximum_temperature` em `k`. `saturation_temperature` é a temperatura de referência; `latent_internal_energy` é a diferença de energia interna vapor-líquido nela. Cada receptor em equilíbrio possui um filme. O snapshot inicial é mantido; intervalos aceitos equilibram gás e líquido com troca de calor com a parede finita.

`mass_flow` é a transferência líquida com sinal do passo físico completo. `evaporated_fuel_mass` e `condensed_fuel_mass` acumulam suas massas positiva e negativa sem duplicar projeções que se cancelam. `vapor_partial_pressure` mostra a pressão parcial do combustível. Balanços térmicos, de massa e químicos incluem ambas as fases.

## Referência termodinâmica

O modelo usa uma pseudoespécie ideal, a constante gasosa comum, calores específicos constantes e deslocamento líquido desprezível. Potenciais químicos iguais fornecem a saturação abaixo. A volume fixo, combustível total e energia interna determinam temperatura e massa de vapor. O inventário químico é transferido internamente; a reação consome só vapor.

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## Limites de suprimento e numéricos

Uma solução escalar limitada usa energia de equilíbrio monótona. Calor da parede usa Euler implícito. Transporte e trabalho do cilindro móvel têm evidência de refinamento de primeira ordem. A coexistência líquida fica no intervalo declarado. Após evaporação completa dentro dele, o gás pode superaquecer sem extrapolar a curva.

O suprimento padrão rejeita líquido sob saturação. O tanque finito opcional resolve vapor da fonte com potenciais corrigidos por pressão. Trilho e captação ainda levam líquido separado; bolhas e fluxo cavitante precisam de mais física.

A comparação só admite arredondamento binary64: `64 * epsilon * max(p, p_sat)`, com `epsilon = 2^-52`. A margem informada não é limitada. É uma verificação de saturação global; perdas locais e NPSH da bomba exigem modelo de fluxo resolvido. As verificações independentes também exigem entropia total não decrescente no flash adiabático e na troca com parede finita.

## Evidência

`equilibrium-fuel-cell` mostra condensação, evaporação por ventilação e nova condensação. `equilibrium-injected-cylinder` acrescenta equilíbrio reversível do receptor e margens ao motor com tanque ventilado. Verificam-se máximo independente de entropia, limites analíticos, refinamento ODE simultâneo, replay completo, rollback e alocações. Asset v31 preserva propriedades e lê v1-v30.

Parâmetros `unverified`. Não são fornecidas propriedades medidas de gasolina, cinética multicomponente ou de velocidade finita, deslocamento líquido/spray resolvidos ou calibração OEM. A aceitação real Unity Editor/Play/Player/IL2CPP continua pendente.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Equilíbrio do tanque com volume líquido finito

Definir `liquid_fuel_tank.parameters.headspace.phase_equilibrium` como `true` para resolver evaporação e condensação do tanque. O filme pareado fornece propriedades `phase_equilibrium`; o gás rastreado compartilha R, cv, poder calorífico e estequiometria. Tanque molhado declara temperatura inicial comum e pressão parcial saturada; tanque seco começa no máximo em saturação. A densidade total fica abaixo da líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` com sinal contam transferência líquida de fase por passo físico. Ler juntos `vapor_partial_pressure`, `saturation_pressure` corrigida, inventário líquido e volume gasoso. A bomba capta líquido separado; transporte bifásico, arraste de bolhas e NPSH local seguem abertos.

[Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md)
