# Alimentation hydraulique entraînée par arbre

[English](HYDRAULIC_PUMP.md) · [简体中文](HYDRAULIC_PUMP.zh-CN.md) · **Français** · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · [Deutsch](HYDRAULIC_PUMP.de.md) · [Español](HYDRAULIC_PUMP.es.md) · [Italiano](HYDRAULIC_PUMP.it.md) · [Português](HYDRAULIC_PUMP.pt-BR.md)

Le graphe managé prend en charge une pompe à cylindrée idéale réversible et une décharge de pression unidirectionnelle quasi stationnaire. Le [laboratoire de pompe allumée](../assets/labs/fired-pump.power.json) relie le vilebrequin à une ligne d'alimentation compliante, à des vannes de passage et à des embrayages commandés par la pression. Ses paramètres sont synthétiques et `unverified`.

## Équations et puissance

La cylindrée `D > 0` est en m³/rad. Une vitesse d'arbre positive délivre du volume de référence de l'admission vers le refoulement :

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

L'écoulement inverse et le fonctionnement hydraulique en moteur sont permis. Il n'y a pas de clapet anti-retour, de fuite, de frottement ni de carte de rendement inférés. L'inertie appartient au nœud d'arbre explicite. Une admission finie perd exactement le volume délivré au refoulement. Une admission par réservoir contribue `p_in * Q` au travail hydraulique externe ; le travail arbre-vers-fluide est un transfert interne et n'est pas ajouté au travail de source global.

La décharge utilise une caractéristique linéaire explicite d'excès de pression :

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` a pour unités m³/(s·Pa), et `p_crack >= 0` est une pression différentielle. Sous le seuil, elle obture exactement. Un débit fini exige une surpression ; la pression n'est jamais ramenée au réglage. C'est une approximation constitutive, pas la mécanique d'un coulisseau ni une courbe ajustée d'aire d'ouverture de vanne. Toute sa chute de pression produit de la chaleur, y compris la part de pression d'ouverture.

Les équations de pompe idéale suivent la limite sans pertes de la [description de pompe à cylindrée fixe MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). Le comportement de seuil est cohérent avec la [description de soupape de décharge](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html) ; la loi linéaire d'excès de pression de Power! est un choix de modélisation plus simple et explicite. Ces références fournissent des équations et un périmètre, pas des mesures de paramètres OEM ni du code source.

## Contrats partagés

`hydraulic_pump` exige un `node_a` de rotation, un refoulement hydraulique `node_b`, et `parameters.inlet_node` (zéro sélectionne le réservoir). L'admission doit différer du refoulement. Les paramètres incluent une `displacement` positive en `m3_rad`, plus une `reservoir_pressure` explicite en `pa` ou `bar` seulement lorsque l'admission est nulle. Il n'a ni entrée, ni puits thermique, ni `node_c` planétaire.

`hydraulic_relief` utilise un `node_a` hydraulique, un `node_b` hydraulique optionnel (zéro ou omis sélectionne un réservoir), un `heat_node` thermique optionnel, et les paramètres `coefficient`, `cracking_pressure`, et `reservoir_pressure` seulement pour le réservoir. Il n'a pas d'entrée d'ouverture. Les séquences de vanne utilisent encore des restrictions commandées séparées.

Les sorties de pompe sont le `volume_flow` moyen du dernier tick, le `torque` de réaction d'arbre, la `hydraulic_power` signée, et le `hydraulic_work` signé cumulé. Les historiques initiaux sont nuls ; les changements d'entrée laissent les moyennes acceptées inchangées. Le `hydraulic_work` global reste le travail externe de réservoir. Les sorties de décharge réutilisent le débit de restriction, la puissance thermique moyenne et la chaleur de fluide cumulée. Toutes les entrées, les historiques et les termes de compensation participent aux forks, aux hachages, à l'annulation et au rollback de lot entier. L'asset v11 conserve les nouvelles définitions et tous les lecteurs v1–v10. L'agent 0.13.0 annonce `shaft_driven_hydraulics` et `fired-pump`.

## Preuves numériques et limites

La vitesse de pompe, les pressions de chambre, les vitesses de port du convertisseur et le travail du cylindre partagent un système de Newton, en utilisant les réponses mécaniques projetées par les engrenages. Les capacités d'embrayage à pression sont rafraîchies dans l'itération de contrainte bornée. Les transferts de fluide acceptés mettent à jour les deux ports et le registre de volume de référence. Le chemin hydraulique indépendant existant est conservé pour les modèles sans pompe, ce qui préserve les hachages de rejeu antérieurs.

La résolution de Newton conjointe permet 24 itérations et 12 dichotomies de recherche linéaire. La tolérance de résidu de pression hydraulique est `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)` ; les tolérances mécaniques et d'embrayage conservent leurs contrats existants. Des états non finis, des pressions manométriques acceptées négatives ou des budgets de solveur épuisés rejettent l'appel complet. Réduisez `step_ns` et inspectez la pression, la compliance, la cylindrée, l'inertie et les échelles d'embrayage avant de réessayer. Il n'y a pas de bornage de cavitation.

Les tests couvrent l'oscillation analytique arbre/compliance et le raffinement du second ordre, la conservation à admission fermée, le fonctionnement inverse en moteur, les réactions de pompe engrenée, la décroissance analytique de décharge, une charge d'arbre stationnaire régulée, et une solution analytique indépendante d'embrayage glissant dépendant de la pression. La capture, les branches, l'annulation, l'échec tardif, le nouvel essai et l'avance sans allocation sont contrôlés. Le rejeu portable et MCP compare les 89 bornes de la pompe allumée ; les contrats mal formés et les rétrogradations de version sont rejetés.

Dans l'expérience allumée de 0.8 s, l'arbre délivre 53.94250162 J au fluide, le travail hydraulique externe est nul, et la décharge dissipe 45.02640514 J. L'énergie hydraulique initiale est explicitement 3 J. La pression de ligne finale est 1.06972624 MPa, la vitesse vilebrequin/turbine 69.75553569 rad/s, et la vitesse de charge 6.64338435 rad/s. Le résidu d'énergie totale est d'environ `1.07e-9 J` ; le résidu de volume de référence est `3.05e-20 m³`. Empreinte `d0bd8f29a706fd89`, hachage final `572150ab5d66a2f6`.

Les cartes de pertes mesurées, la commande de cylindrée, la dynamique de batterie et de commande de tension, la course et l'inertie de coulisseau et de piston, les accumulateurs à gaz, la cavitation, les propriétés dépendantes de la température et la coordination ECU/TCU restent ouverts. Ce point de contrôle n'établit pas une DCT/AT complète, une performance de véhicule calibrée ni l'acceptation Unity Editor/Player.

## Fuite explicite, frottement d'arbre et alimentation électrique

`HydraulicPumpAssembly` fournit une réduction de pompe réutilisable à coefficients constants. Elle accepte une cylindrée D en m³/rad, une conductance de fuite G en m³/(s·Pa), et un frottement visqueux d'arbre B en N·m·s/rad. D doit être positif ; G et B doivent être non négatifs et finis. Aucun rendement nominal ni propriété d'huile n'est inféré.

Pour la pression différentielle `dp = p_out - p_in` :

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

Les signes prennent en charge le pompage et le fonctionnement hydraulique en moteur dans les deux sens, ainsi que la fuite à travers une pompe arrêtée. La fuite reste un chemin passif du refoulement vers l'admission même lorsqu'elle dépasse le débit de cylindrée. La réduction de fuite à conductance constante suit la description analytique des pertes de la [référence de pompe MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). La traînée visqueuse linéaire est un choix constitutif explicite de Power! ; ce n'est pas le modèle de frottement dépendant de la pression de cette référence, ni une carte de rendement OEM.

`TryEvaluate` renvoie le débit net instantané, la réaction totale d'arbre, la puissance signée arbre/fluide et les deux puissances de perte non négatives. Il rejette les pressions manométriques négatives, les entrées non finies et le dépassement, sans renvoyer une réaction partielle.

`CreateComponents` renvoie une liste immuable avec des ID explicites et distincts pour une `hydraulic_pump` idéale, une `hydraulic_resistance` à ouverture fixe du refoulement vers l'admission, et un `shaft` de raideur nulle de l'arbre de pompe vers le bâti. Spécifiez un puits thermique, ou laissez les pertes entrer dans le rejet thermique externe. Le compilateur de modèle contrôle les ports, les domaines, les unités et les ID globaux. Les composants ordinaires conservent la résolution de point milieu couplée, les transactions, les canaux, le schéma JSON et l'asset v11 ; il n'y a pas d'état d'assemblage caché ni de nouveau format. Les canaux de pompe décrivent la branche idéale. Soustrayez le débit de fuite pour obtenir le débit de l'assemblage ; incluez la traînée d'arbre lorsque vous interprétez la charge totale d'arbre. Ne comptez pas le travail de pompe idéal à la fois comme travail de source externe et comme transfert interne.

`fired-pump-losses` relie la fuite et la traînée à la transmission allumée existante. Un nœud thermique de pompe distinct reçoit les deux pertes. La limite sans pertes reproduit tous les observables partagés de `fired-pump` dans la tolérance physique. Les G et B constants sont des entrées de recherche et restent `unverified`.

`electric-pump` relie un moteur CC RL de 12 V à un arbre de pompe distinct, avec une force contre-électromotrice, une inductance, un couple et une chaleur cuivre explicites. Une ligne d'alimentation compliante, une décharge et des vannes de remplissage et de vidange planifiées actionnent un embrayage entre un arbre entraîné et une charge. Les changements de tension et les événements de vanne utilisent des ticks exacts. La pompe n'a pas de liaison au vilebrequin et le travail hydraulique externe est nul. Le travail électrique est inclus dans le travail de source global ; l'arbre entraîné et le couple de charge sont des frontières de puissance externes distinctes. Une tension et des commandes de vanne prescrites n'implémentent ni batterie, ni ECU/TCU, ni régulateur en boucle fermée.

Le mouvement analytique amorti arbre/pression et une EDO à trois états moteur RL/arbre/pression intégrée indépendamment contrôlent le raffinement lisse du second ordre. Les admissions fermées, le travail de réservoir, le fonctionnement signé, les pertes passives, le routage de chaleur, zéro allocation, les branches, l'annulation et le rollback d'échec tardif sont contrôlés sur les deux assemblys du cœur. JSON, les assets portables et le MCP comparent les 89 bornes du rapport de pertes allumées et les 106 bornes du rapport électrique. Les tests d'import et Play Unity préparés exigent une exécution séparée de l'éditeur.

<a id="sampled-pressure-regulation"></a>
## Régulation de pression échantillonnée

`pressure_controller` lit un nœud de pression manométrique hydraulique et possède un canal de tension de moteur CC existant. C'est un régulateur PI discret, avec un gain proportionnel explicite en `v_pa`, un gain intégral en `v_pa_s`, des bornes de tension et une tension intégrale initiale. L'entrée de consigne a des unités de pression. Son `sample_period_ns` entier va de 1 ns à 1 s et doit être un multiple exact du tick du modèle. Les gains et les consignes de pression sont non négatifs ; les bornes de tension sont finies et strictement croissantes. Aucun réglage n'est inféré.

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

L'intégration conditionnelle est la stratégie d'anti-windup par saturation décrite par la [référence de contrôle MathWorks](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html). La transition discrète précise de Power! ci-dessus est son modèle déclaré, pas du code d'implémentation copié ni une preuve de réglage OEM. La saturation seule n'établit pas le suivi : une cible inaccessible peut s'exécuter et se rejouer avec succès tout en échouant aux KPI.

Les échantillons ont lieu au temps zéro et aux multiples absolus de la période configurée. Le premier échantillon conserve l'intégrale initiale fournie explicitement ; les échantillons suivants utilisent la période. La commande est maintenue entre les échantillons. Les événements à tick exact sont appliqués avant un échantillon au même tick. Un événement à l'extrémité d'un appel met à jour la consigne avant l'instantané ; l'échantillonnage à cette extrémité n'a lieu que lorsque le tick physique suivant commence. Les intervalles internes de capture et d'inversion d'embrayage ne déclenchent pas de mises à jour supplémentaires du contrôleur.

Le compilateur vérifie que la cible est une entrée de tension de moteur CC et a exactement un propriétaire, que le capteur est hydraulique, et que la tension initiale du moteur est dans les bornes. Le canal de tension possédé reste dans la définition du composant, mais il est absent de la liste d'entrées externes. Les écritures directes et les dérogations de tension planifiées sont rejetées ; changez plutôt l'entrée `pressure_setpoint` du contrôleur. Les autres canaux de moteur, de pompe et de vanne conservent leur sémantique existante. Plusieurs boucles indépendantes peuvent partager un capteur de pression.

Les canaux observables sont `sampled_pressure`, `pressure_error`, `integral_voltage` et `command_voltage`. Les historiques de pression et d'erreur commencent à zéro ; la commande initiale est la tension configurée du moteur, et l'intégrale initiale est explicite. Les historiques décrivent le dernier échantillon, plutôt qu'une erreur de pression recalculée en continu. Les quatre états du contrôleur et l'entrée moteur maintenue participent aux hachages, aux forks et au rollback de lot complet. L'échantillonnage et l'avance réussie n'allouent pas de mémoire managée après échauffement. Une arithmétique PI non finie rejette l'appel complet ; inspectez les échelles de gain, de consigne et d'intégrale.

Le contrôleur n'ajoute ni énergie stockée physique ni frontière de puissance. Sa commande change la frontière de tension du moteur existant, dont le courant, le travail et la chaleur cuivre restent dans la résolution couplée et le registre de conservation. Les modèles sans contrôleur conservent leurs empreintes et leur avance. Les modèles commandés ajoutent la balise d'empreinte 13. L'asset v12 conserve la définition complète du contrôleur ; un scénario de pompe authentique v11 conserve son condensé d'origine, son empreinte et son rejeu sur le même runtime après mise à niveau.

`pressure-regulated-pump` utilise un contrôleur de 5 ms et des ticks physiques de 100 µs, avec des perturbations planifiées de remplissage et de vidange d'embrayage et des cibles de 300/350/200 kPa. Les gains, les limites d'actionneur et tous les autres paramètres restent `unverified`. Il a 757 bornes de rapport JSON, asset et MCP concordantes. Les tests comparent un contrôleur échantillonné et une installation RK4 séparés, contrôlent le raffinement du tick physique à période de contrôleur fixe, les règles exactes d'horloge et d'extrémité, le rétablissement après saturation, les diagnostics d'unité et de propriété, le rollback de l'état du contrôleur, les branches, zéro allocation et le registre complet de travail électrique et hydraulique.

Cela fournit une boucle de retour de pression. La dynamique de batterie et de boucle PWM ou de courant, le filtrage, le retard et la quantification de capteur, la dynamique de vanne, de coulisseau et de piston, la coordination ECU/TCU, la DCT/AT complète, les défauts et la calibration mesurée restent un travail séparé inachevé.

<a id="finite-battery-supply-and-duty-regulation"></a>
## Alimentation par batterie finie et régulation de rapport cyclique

Un nœud `battery` possède deux états : la fraction de charge z et la tension de polarisation v_p. Son stockage est une capacité de charge explicite Q en C ou Ah (1 Ah = 3600 C), l'état initial est le SOC en `fraction`, et la position est la tension de polarisation initiale en V. Son enregistrement de batterie fournit les OCV à l'état vide et à l'état plein, la résistance série R0, la résistance de polarisation Rp, la capacité Cp et un puits thermique ou le rejet thermique externe. La tension à vide est affine en SOC :

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

Un courant positif décharge ; un courant négatif charge. La topologie suit la [description de circuit équivalent de batterie](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html). La tension à vide affine et les paramètres constants sont des réductions explicites de Power!, plutôt que des tables de température ou de vieillissement, une chimie mesurée, une perte de capacité ou un BMS. Le SOC reste dans [0,1]. Dépasser l'inventaire de charge ou obtenir une tension de bus négative rejette le lot entier ; il n'y a ni bornage silencieux ni réserve inventée. Raccourcissez le lot, arrêtez la décharge ou la charge, ou fournissez d'autres conditions initiales déclarées.

`battery_motor` relie un arbre de rotation à un bus de batterie et conserve une résistance moteur, une inductance, un coefficient de couple et de force contre-électromotrice, et un courant initial explicites. Son entrée de rapport cyclique bidirectionnel moyenné est dans [-1,1] : la tension moteur est le rapport cyclique fois la tension de bus, et le courant côté batterie est le rapport cyclique fois le courant moteur. Ce transfert de puissance est interne et n'est pas ajouté à `source_work`. L'énergie inductive du moteur et l'énergie de polarisation et chimique de la batterie participent à l'énergie stockée totale. La chaleur cuivre, série et de polarisation est routée vers leurs puits explicites. C'est un convertisseur moyenné idéal, pas une commutation PWM, des pertes de convertisseur, des contacteurs ni une boucle de régulation de courant.

`resistive_load` fournit une résistance positive explicite, une entrée d'ouverture optionnelle dans [0,1] et un puits thermique. L'ouverture met la conductance à l'échelle ; une ouverture nulle déconnecte exactement. Pour une conductance de charge totale G et un courant de bus côté moteur I_m :

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

La résistance de batterie partagée couple tous les consommateurs. La matrice de point milieu couplée inclut la charge, la polarisation, le courant moteur et les réponses mécaniques. Les facteurs possédés par la simulation se mettent à jour lorsque les rapports cycliques, les ouvertures d'accessoires ou les durées d'intervalle interne changent. Les réponses d'engrenage, de cylindre, de convertisseur et d'embrayage utilisent ces mêmes facteurs. Les modèles plus anciens non alimentés conservent leur chemin de solveur et leurs empreintes précédents. L'énergie chimique affine et l'énergie RC sont quadratiques, donc les transferts électriques de point milieu ont des contrôles de conservation indépendants. La même physique prend en charge la récupération du moteur.

`pressure_duty_controller` utilise la transition PI entière et de saturation existante, avec des gains en `fraction_pa` et `fraction_pa_s`, des bornes de rapport cyclique explicites dans [-1,1], et un rapport cyclique intégral initial. Il possède un canal de rapport cyclique de `battery_motor`. Les agents changent `pressure_setpoint` ; les dérogations directes de rapport cyclique renvoient `controlled_input`. Lisez `sampled_pressure`, `pressure_error`, `integral_duty` et `command_duty`. Le rapport cyclique maintenu, la charge, la polarisation et la mémoire de contrôle partagent les instantanés, les forks, l'annulation et le rollback complet. L'échantillonnage et l'avance réussie restent sans allocation.

`battery-regulated-pump` combine une alimentation par batterie finie, des impulsions de charge accessoire et un régulateur de rapport cyclique de 5 ms avec le laboratoire d'embrayage à pression. Sa capacité de 50 C est un petit inventaire d'essai synthétique, pas une mesure de batterie véhicule. À 15 s le SOC tombe de 0.8 à environ 0.627, tandis que la pression se termine à environ 200.828 kPa pour une cible de 200 kPa. Les 761 bornes JSON/asset/MCP concordent. Les tests contrôlent séparément la relaxation RC analytique, l'inventaire de charge résistive, l'intégration RK4 indépendante moteur/circuit, le raffinement du tick physique, le rapport cyclique signé et la récupération, l'équivalence d'enroulements en parallèle, le couplage engrenage/embrayage/pompe, le rollback d'épuisement tardif, les branches, l'annulation et zéro allocation.

Le BMS, la chimie, le vieillissement et le retour de température de la batterie, les défauts et les contacteurs, la commande PWM et de courant, la dynamique de capteur, la mécanique d'actionneur, l'ECU/TCU complète et la calibration restent ouverts. Les paramètres de batterie et toutes les entrées de laboratoire restent `unverified`.
