# Rete di trasporto del carburante in equilibrio

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · **Italiano** · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Ambito del modello

Il dominio `fuel` trasporta insieme liquido puro e vapore in recipienti elastici finiti. Ogni recipiente conserva massa totale ed energia calorica più elastica indipendenti; il flash determina temperatura, pressione e frazioni di vapore. `fuel_restriction` scambia fluido donatore omogeneo, `fuel_heat_link` scambia calore con un nodo termico finito. L’energia chimica del potere calorifico resta separata.

Il nodo usa `storage` per volume di riferimento in `m3` o `l`, `initial` per temperatura in `k`, `position` per massa totale in `kg`. L’oggetto `fuel` dichiara proprietà, cedevolezza e limiti di pressione/temperatura. Recipienti collegati condividono densità, riferimenti calorico e di fase, limiti termici e potere calorifico. Entrambi i porti della restrizione sono finiti; il collegamento termico unisce carburante e nodo termico. Apertura in [0,1] con canale opzionale.

## Flusso conservativo

La restrizione usa una legge dissipativa quasi statica del volume di riferimento con conduttanza K esplicita in `m3_s_pa`. Trascura inerzia del collegamento, accumulo cinetico e scorrimento tra fasi. La densità donatrice converte flusso volumetrico in massa totale; l’entalpia donatrice porta entrambe le fasi. Non ci sono lavoro di pressione esterno aggiuntivo né un termine termico separato per il salto di pressione. Il lavoro elastico è già nel recipiente.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## Integrazione temporale ed entropia

Euler implicito limitato risolve le stesse massa q ed entalpia H accettate per entrambi i recipienti. I collegamenti avanzano secondo ID stabili; il bagno finito usa un altro sottopasso conservativo. L’accoppiamento è del primo ordine. Violazioni dei limiti di massa, temperatura e pressione e fallimenti rifiutano l’intero batch con ingressi, storie compensate e intervalli speculativi della frizione.

Termodinamica compatibile e conduttanza positiva guidata dalla pressione danno produzione continua di entropia della coppia non negativa. Euler implicito aggiunge entropia numerica. `entropy_produced` registra la variazione completa accettata, miscela e discretizzazione incluse; non è una mappa misurata delle perdite. Verifiche indipendenti completano conservazione e raffinamento.

## Uso ed evidenze

Richiedere `equilibrium-fuel-network` o `electrothermal-fuel-network` via MCP oppure eseguire lo stesso JSON con CLI. Leggere `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy`, `specific_enthalpy`. Qui `internal_energy` include la parete elastica e l’energia chimica è separata. I collegamenti mostrano portate con segno, massa cumulativa, `transport_energy` e variazione di entropia della coppia.

I laboratori coprono scarico liquido in coesistenza, flusso inverso, aperture e calore finito/elettromeccanico. Esponenziale indipendente della pressione liquida, ODE bifase simultanea massa/entalpia/calore, inversione termica e cattura speculativa della frizione verificano accuratezza e composizione. Asset e MCP effettivo mantengono tutti i campioni e gli hash. Asset v32 conserva proprietà e legge v1-v31. Unity Editor/Play/Player/IL2CPP effettivo resta non verificato.

## Lavoro rimanente

Restano i limiti del materiale e della deformazione rappresentabile della chiusura. I parametri restano `unverified`. Riempimento pompa e lavoro d’albero, collegamento iniettore, inerzia locale, flusso critico, ritardo di nucleazione, scorrimento e misure di miscele reali rimangono separati. I rail iniettore alimentati da pompa mantengono il percorso liquido separato fino all’accoppiamento.

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.it.md) · [VALIDATION.md](VALIDATION.it.md)
