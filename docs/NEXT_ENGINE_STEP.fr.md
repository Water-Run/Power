# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

Le récepteur prend en charge l'équilibre réversible et les contrôles de saturation de l'alimentation.

[Équilibre des phases du carburant et limites d'alimentation](FUEL_PHASE_EQUILIBRIUM.fr.md)

## Prochaine étape

Résoudre ensuite transfert vapeur du réservoir et écoulement diphasique pompe/rail avec propriétés explicites et couplage volume/travail conservatif. Garder les mesures OEM manquantes. Allumage, admission/échappement, pertes mécaniques et coordination ECU/TCU restent des travaux distincts.

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Paramètres `unverified`. Propriétés d'essence mesurées, cinétique multiphase multicomposant ou à vitesse finie, déplacement liquide/pulvérisation résolus et calibration OEM ne sont pas fournis. L'acceptation réelle Unity Editor/Play/Player/IL2CPP reste en attente.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)
