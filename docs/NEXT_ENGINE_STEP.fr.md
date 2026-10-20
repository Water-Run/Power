# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

Les chambres finies échangent désormais le travail de pression avec les arbres et se remplissent/livrent par clapets idéaux, avec changement de phase en sous-alimentation. L'injecteur physique et le récepteur restent à coupler.

[Pompe carburant diphasique entraînée par arbre](FUEL_CHAMBER_PUMP.fr.md)

## Prochaine étape

Relier la pompe carburant à l'injecteur physique et aux récepteurs cylindre/film avec transport diphasique, déplacement et travail cinétique explicites. Résoudre inertie locale d'entrée et blocage sonique par références indépendantes. Continuer allumage, admission/échappement, pertes, actionnement de transmission et coordination ECU/TCU vers l'objectif complet.

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Les paramètres restent `unverified`. Les limites de température, pression et volume représentable s'appliquent sur tout le cycle. Hypothèses : liquide incompressible, vapeur idéale pseudo-espèce, équilibre instantané à température commune et pression extérieure de carter nulle. Inertie d'entrée/NPSH, blocage sonique, nucléation retardée, inertie/hystérésis de clapet, mélanges réels et calibration mesurée restent ouverts. Raccordement physique d'injecteur et travail de déplacement/cinétique du récepteur restent distincts.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)
