# C1 FB - Marcador visual de objeto

Extensão servidor-cliente para ativar ou remover um marcador visual sem alterar slots de equipamento.

| Offset | Tamanho | Campo |
| --- | --- | --- |
| 0 | 1 | `0xC1` |
| 1 | 1 | tamanho `0x07` |
| 2 | 1 | código `0xFB` |
| 3 | 2 | identificador do objeto, `UInt16` little-endian |
| 5 | 1 | identificador do marcador |
| 6 | 1 | estado: `0` remove, `1` ativa |

O marcador `1` representa o portador da Coroa de Valoria e reutiliza o efeito visual `eBuff_CastleCrown`. Identificadores desconhecidos são ignorados pelo cliente.
