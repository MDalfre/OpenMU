# C1 FA - Taxa autoritativa da Chaos Machine

Extensão usada pelo MuMain para substituir o número de sucesso já existente na interface.

## Requisição do cliente

| Offset | Tamanho | Campo |
| --- | --- | --- |
| 0 | 1 | `0xC1` |
| 1 | 1 | tamanho `0x05` |
| 2 | 1 | código `0xFA` |
| 3 | 2 | revisão da interface, `UInt16` little-endian |

## Resposta do servidor

| Offset | Tamanho | Campo |
| --- | --- | --- |
| 0 | 1 | `0xC1` |
| 1 | 1 | tamanho `0x07` |
| 2 | 1 | código `0xFA` |
| 3 | 2 | revisão recebida, `UInt16` little-endian |
| 5 | 2 | taxa efetiva em basis points, `UInt16` little-endian |

Dez mil basis points representam `100,00%`. O cliente descarta respostas cuja revisão não corresponde ao conteúdo atual da Chaos Machine.
