# Retroazione AT idraulica

[English](AT_CONTROL.md) · [简体中文](AT_CONTROL.zh-CN.md) · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · [Deutsch](AT_CONTROL.de.md) · [Español](AT_CONTROL.es.md) · **Italiano** · [Português](AT_CONTROL.pt-BR.md)

## Contratto

`at_controller` accetta una marcia richiesta intera in [-1,4]; zero indica folle. Gestisce cinque coppie di valvole di riempimento/scarico e il blocco facoltativo del convertitore. L'ordine è ingresso del portasatelliti, solare piccolo, solare grande, freno del portasatelliti, freno del solare grande, poi blocco.

Prima di applicare una marcia incompatibile, la forza reale dei pattini conferma il rilascio. Il PI di pressione limitato usa la pressione misurata nelle camere. La marcia è attiva solo dopo la conferma dei contatti e del blocco fisico delle frizioni. Richieste frazionarie e scritture dirette alle valvole controllate restituiscono errori utili.

Fase campionata, guasto, integrali di pressione e tempi appartengono allo stato transazionale completo. Annullamento, errori tardivi e rami mantengono le stesse storie. I guasti includono timeout di rilascio/applicazione, bassa alimentazione, cambio di direzione e perdita del blocco confermato. Un comando di scarico non libera uno scarico fisicamente bloccato.

Il blocco facoltativo usa limiti di marcia avanti, velocità di ingresso, slittamento e attesa con isteresi di sblocco separata. L'uscita descrive lo stato reale Released/Applying/Locked/Releasing. È una frizione a pistone fisica, non una velocità uguale imposta.

## Evidenze e limiti

`controlled-hydraulic-ravigneaux` e `controlled-fired-hydraulic-ravigneaux` usano il canale 900 e l'ID 1400. Conservano 99 e 122 stati dichiarati entro il limite invariato di 128. v28 conserva percorsi, guadagni e clock e legge v1-v27.

Sono controlli di ricerca e i parametri restano `unverified`. Coordinamento della coppia ECU, sensori/valvole dettagliati, guasti completi del veicolo e calibrazione OEM restano aperti. Le verifiche managed e Standard non provano l'accettazione reale Unity Editor/Play/Player/IL2CPP.
