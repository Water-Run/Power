# Stantuffo idraulico e frizione azionata per contatto

[English](HYDRAULIC_PISTON.md) · [简体中文](HYDRAULIC_PISTON.zh-CN.md) · [Français](HYDRAULIC_PISTON.fr.md) · [Русский](HYDRAULIC_PISTON.ru.md) · [日本語](HYDRAULIC_PISTON.ja.md) · [한국어](HYDRAULIC_PISTON.ko.md) · [Deutsch](HYDRAULIC_PISTON.de.md) · [Español](HYDRAULIC_PISTON.es.md) · **Italiano** · [Português](HYDRAULIC_PISTON.pt-BR.md)

`hydraulic_piston` collega una massa traslazionale a una camera idraulica anteriore e o a una camera posteriore o a un serbatoio esplicito di contropressione. `piston_clutch` legge la forza sulla pastiglia dello stantuffo. Una pressione positiva può muovere uno stantuffo attraverso il gioco libero senza trasmettere coppia alla frizione.

## Equazioni ed energia

Per lo spostamento x, la velocità v, le aree efficaci anteriore e posteriore Af/Ab e le pressioni manometriche pf/pb, la forza dello stantuffo è `Af*pf - Ab*pb`. L'espansione anteriore aspira `Af*dx`; la contrazione posteriore eroga `Ab*dx`. Una camera finita immagazzina `C*p*p/2` joule e `C*p` metri cubi di inventario di riferimento. Il registro dei volumi include il volume spazzato dallo stantuffo `(Af-Ab)*(x-x_initial)`. Un serbatoio posteriore contribuisce il lavoro esterno con segno `-pb*Ab*dx` e il volume di riferimento `-Ab*dx`.

Il nodo traslazionale immagazzina `m*v*v/2`. Una `linear_spring` aggiunge `K*(x-rest)^2/2` e la perdita viscosa `D*v_relative^2`, con instradamento termico esplicito. Il suo canale `friction_heat` riporta il calore di smorzamento cumulativo con somma compensata. È indipendente dalle temperature arrotondate del nodo termico.

Il potenziale unilatero della pastiglia è `Kpad*max(x-contact,0)^2/2`. I finecorsa inferiore e superiore aggiungono lo stesso potenziale quadratico fuori dalla corsa nominale. I finecorsa sono cedevoli: la penetrazione immagazzina energia e produce una forza di richiamo. Non bloccano il moto. Per un potenziale a cerniera V, la reazione dell'intervallo usa `-(V(x_next)-V(x_old))/dx`, valutata con un gradiente discreto resistente alla cancellazione. Di conseguenza, il lavoro di contatto è esattamente la variazione di potenziale nelle equazioni discrete. Lo Jacobiano analitico copre cerniere attive, inattive, in attivazione e in rilascio; su una cerniera ferma usa la derivata unilatera media.

Le capacità statica e strisciante della frizione sono `mu*surfaces*radius*Npad`. Il solver usa la forza discreta della pastiglia durante l'intervallo e la forza istantanea della pastiglia per gli snapshot. Il calore di attrito resta non negativo; una frizione ideale bloccata non dissipa potenza di slittamento. La stessa risoluzione congiunta include pressione, inerzia dello stantuffo, smorzamento della molla, alimentazione elettrica e i vincoli meccanici e di frizione esistenti.

## Contratti e confini numerici

| Elemento | Dati richiesti |
|---|---|
| Nodo `translational` | Massa positiva in kg, velocità iniziale in m/s, posizione in m |
| `hydraulic_piston` | Una porta traslazionale A e una porta idraulica anteriore B; aree anteriore e posteriore, nodo e pressione posteriori, limiti di corsa crescenti, rigidezza dei finecorsa, posizione e rigidezza di contatto |
| `linear_spring` | Porte traslazionali A/B, oppure B a massa; rigidezza N/m, smorzamento N·s/m, spostamento di riposo m, pozzo termico facoltativo |
| `piston_clutch` | Porte rotazionali A/B, oppure B a massa, ID del componente stantuffo, raggio m, coefficienti statico e strisciante, superfici di attrito intere |
| `force_source` | Porta traslazionale A e input di forza esterna in N |

`linear_spring.parameters.rest_angle` conserva la chiave del descrittore condiviso, ma porta una quantità di spostamento in metri. Uno stantuffo possiede un dato nodo traslazionale; più elementi di attrito di frizione possono riferirsi in modo esplicito alla sua pastiglia. Una camera posteriore finita deve differire dalla camera anteriore. Un serbatoio ha `back_node=0` e una `back_pressure` esplicita non negativa. L'attrito statico deve essere almeno pari a quello strisciante. Le superfici stanno in 1–128 e il contatto della pastiglia sta dentro la corsa nominale.

La corsa di stantuffo accettata per intervallo è limitata a un quarto della corsa nominale. Riduci il tick fisso se il moto viola questo limite o se la risoluzione congiunta non converge. Una pressione manometrica accettata negativa rifiuta il batch completo; controlla la portata di alimentazione, la cedevolezza, le aree efficaci, l'inerzia e lo smorzamento. Questo modello non ha una saturazione di cavitazione. Rollback completo, annullamento, fork e hash di stato includono le storie di moto, pressione, attrito e smorzamento.

Le equazioni assumono cedevolezza efficace e aree costanti, una massa mobile concentrata, molla e smorzamento di richiamo lineari, una pastiglia elastica ed estremità cedevoli. Attrito delle tenute, cavitazione, modi di deformazione dei dischi, usura, mappe di attrito dettagliate e calibrazione OEM restano fuori da questa implementazione.

## Verifica e laboratorio

I controlli indipendenti coprono l'oscillazione analitica accoppiata di massa, molla e fluido, le camere posteriori finite e a serbatoio, il volume spazzato e il lavoro di pressione, le identità del lavoro di cerniera, le derivate analitiche di contatto e un riferimento di contatto RK4 a tratti. Il moto lineare regolare mostra affinamento del secondo ordine; i test di contatto non regolare controllano l'errore decrescente, senza rivendicare un ordine ibrido uniforme. I controlli della frizione coprono il riempimento libero, il contatto della pastiglia, la cattura, il rilascio e il calore di attrito. Fallimento numerico tardivo, annullamento, indipendenza dei fork, batch esatti e passi senza allocazioni sono verificati.

Il [laboratorio sintetico della frizione azionata a stantuffo](../assets/labs/piston-actuated-clutch.power.json) usa una batteria finita, una pompa elettrica regolata a duty, valvole di riempimento e scarico, uno stantuffo da 20 g, un gioco della pastiglia di 2 mm, una molla di richiamo da 10 kN/m e uno smorzamento di 300 N·s/m. Lo smorzamento è un parametro di ricerca esplicito, scelto per tenere non negativa la camera fornita durante il transitorio. Non è una misura OEM. A 15 s il carico sulla pastiglia è circa 177.28 N, con capacità statica e strisciante di 22.69/11.35 N·m. I replay di CLI, portabile e MCP reale concordano a ogni limite riportato.

Vedi [VALIDATION.md](VALIDATION.it.md) per i limiti numerici, i registri energetici separati, le misure di prestazioni e l'ambito di runtime. L'asset v14 conserva la topologia completa. Le viste Studio di corsoio e di contatto e i test di importazione e Play sono preparati; l'evidenza reale di Unity Editor e Player resta in sospeso.
