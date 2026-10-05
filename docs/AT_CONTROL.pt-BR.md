# Realimentação de AT hidráulica

[English](AT_CONTROL.md) · [简体中文](AT_CONTROL.zh-CN.md) · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · [Deutsch](AT_CONTROL.de.md) · [Español](AT_CONTROL.es.md) · [Italiano](AT_CONTROL.it.md) · **Português**

## Contrato

`at_controller` aceita uma marcha solicitada inteira em [-1,4]; zero indica neutro. Ele controla cinco pares de válvulas de enchimento/drenagem e o bloqueio opcional do conversor. A ordem é entrada do portasatélites, solar pequeno, solar grande, freio do portasatélites, freio do solar grande e bloqueio.

Antes de aplicar uma marcha incompatível, a força real das pastilhas confirma a liberação. O PI de pressão limitado usa a pressão medida nas câmaras. Uma marcha fica ativa somente após confirmar contatos e travamento físico das embreagens. Solicitações fracionárias e escritas diretas nas válvulas controladas retornam erros úteis.

Fase amostrada, falha, integrais de pressão e tempos pertencem ao estado transacional completo. Cancelamento, falhas tardias e ramificações preservam os mesmos históricos. As falhas incluem tempo de liberação/aplicação, baixa alimentação, mudança de direção e perda do travamento confirmado. Um comando de drenagem não libera um dreno fisicamente bloqueado.

O bloqueio opcional usa limites de marcha à frente, velocidade de entrada, deslizamento e espera com histerese de desbloqueio separada. A saída descreve o estado real Released/Applying/Locked/Releasing. É uma embreagem física com pistão, não uma igualdade de velocidade imposta.

## Evidências e limites

`controlled-hydraulic-ravigneaux` e `controlled-fired-hydraulic-ravigneaux` usam o canal 900 e o ID 1400. Preservam 99 e 122 estados relatados dentro do limite inalterado de 128. v25 preserva rotas, ganhos e relógios e lê v1-v24.

São controles de pesquisa e os parâmetros continuam `unverified`. Coordenação de torque ECU, sensores/válvulas detalhados, falhas completas do veículo e calibração OEM permanecem pendentes. Verificações managed e Standard não comprovam aceitação real Unity Editor/Play/Player/IL2CPP.
