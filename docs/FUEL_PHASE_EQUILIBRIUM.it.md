# Equilibrio di fase del carburante e limiti di alimentazione

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · **Italiano** · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Contratto

L'opzione `fuel_film.parameters.phase_equilibrium` dichiara `reference_pressure` in `pa` o `bar` e `minimum_temperature` / `maximum_temperature` in `k`. `saturation_temperature` è la temperatura di riferimento; `latent_internal_energy` la differenza di energia interna vapore-liquido a tale temperatura. Ogni ricevitore in equilibrio ha un film. Lo snapshot iniziale resta dichiarato; gli intervalli accettati equilibrano gas e liquido con scambio verso la parete finita.

`mass_flow` è il trasferimento netto con segno del passo fisico completo. `evaporated_fuel_mass` e `condensed_fuel_mass` accumulano masse positive e negative senza contare due volte proiezioni che si annullano. `vapor_partial_pressure` indica la pressione parziale del carburante. I bilanci termici, di massa e chimici includono entrambe le fasi.

## Riferimento termodinamico

Il modello usa una pseudospecie ideale, la costante gassosa comune, calori specifici costanti e spostamento liquido trascurabile. L'uguaglianza dei potenziali chimici dà la saturazione seguente. A volume fisso, combustibile totale ed energia interna determinano temperatura e massa di vapore. L'inventario chimico si trasferisce internamente; la reazione consuma solo vapore.

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

## Limiti di alimentazione e numerici

Una soluzione scalare limitata usa l'energia di equilibrio monotona. Il calore della parete usa Euler implicito. Trasporto e lavoro del cilindro mobile hanno evidenza di convergenza del primo ordine. La coesistenza liquida resta nell'intervallo dichiarato. Dopo completa evaporazione al suo interno il gas può surriscaldarsi senza estrapolare la curva.

Rail, serbatoi finiti e ingressi espongono `saturation_pressure` e `cavitation_margin` in Pa. Il margine è pressione assoluta locale meno saturazione alla temperatura liquida. Un margine negativo con liquido respinge l'intero lotto. Questo delimita l'alimentazione monofase; trasferimento vapore del serbatoio, bolle e flusso cavitante della pompa restano aperti.

Il confronto ammette solo arrotondamento binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. Il margine riportato non viene limitato. È un controllo di saturazione globale; perdite locali e NPSH della pompa richiedono un modello di flusso risolto. Le verifiche indipendenti richiedono inoltre entropia totale non decrescente nel flash adiabatico e nello scambio con parete finita.

## Evidenza

`equilibrium-fuel-cell` mostra condensazione, evaporazione da sfiato e nuova condensazione. `equilibrium-injected-cylinder` aggiunge equilibrio reversibile del ricevitore e margini al motore con serbatoio ventilato. Si verificano massimo d'entropia indipendente, limiti analitici, raffinamento ODE simultaneo, replay completo, rollback e allocazioni. Asset v30 conserva le proprietà e legge v1-v29.

Parametri `unverified`. Non sono fornite proprietà misurate della benzina, cinetica multicomponente o a velocità finita, spostamento liquido/spray risolti o calibrazione OEM. L'accettazione reale Unity Editor/Play/Player/IL2CPP resta in attesa.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.it.md](VALIDATION.it.md)
