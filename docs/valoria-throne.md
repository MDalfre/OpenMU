# Trono de Valoria

O plugin `MUnique.OpenMU.PlugIns.ValoriaThrone` controla a abertura de Valley of Loren para um único servidor PvP, o ciclo do Guardião do Trono e a limpeza segura ao final do evento.

## Escopo de execução

- A implementação é suportada pelo host all-in-one (`src/Startup`).
- Cada servidor registra seu `IGameServerContext` no mesmo processo, permitindo evacuação e mensagens globais entre os servidores locais.
- O host Dapr distribuído não deve ativar este plugin até existir coordenação distribuída para estado, agenda, boss e presença de jogadores.

## Configuração

O plugin fica desabilitado por padrão. Cadastre uma configuração para `ValoriaThronePlugIn` no painel administrativo antes de ativá-lo:

```json
{
  "enabled": true,
  "eventServerId": 2,
  "eventMapId": 30,
  "fallbackMapId": 0,
  "fallbackPositionX": 142,
  "fallbackPositionY": 126,
  "announcementDuration": "00:05:00",
  "preparationDuration": "00:00:00",
  "registrationDuration": "00:05:00",
  "battleDuration": "00:30:00",
  "crownPhaseDuration": "00:02:00",
  "cooldownDuration": "00:05:00",
  "allowLateEntry": false,
  "allowGameMasterBypass": true,
  "guardianMonsterId": 249,
  "guardianSpawnX": 142,
  "guardianSpawnY": 126,
  "guardianDirection": 0,
  "schedule": {
    "enabled": true,
    "timeZone": "America/Sao_Paulo",
    "daysOfWeek": ["Sunday"],
    "startTime": "20:00:00"
  }
}
```

Valide que o servidor de evento tenha PvP habilitado, que o mapa e o monstro existam e que a posição configurada seja caminhável.

## Regras de acesso

- Durante registro, somente personagens no servidor configurado podem entrar em Valley of Loren.
- Durante a batalha, a entrada depende de `allowLateEntry`.
- Game masters podem ignorar a restrição somente se `allowGameMasterBypass` estiver ativo.
- Login ou troca de personagem em Valley of Loren é redirecionado a Lorencia quando a entrada estiver negada.
- O bloqueio é aplicado antes do débito de Zen no warp e também cobre gate, warp interno e seleção de personagem.

## Encerramento e recuperação

No fechamento, timeout, falha de spawn ou comando administrativo, o Guardião rastreado é removido e os personagens no mapa são enviados para o mapa e coordenadas de fallback.

O personagem permanece no mesmo `GameContext`: jogadores de outros servidores nunca são movidos para o servidor PvP, apenas para fora de Valley of Loren. O armazenamento de estado atual é intencionalmente em memória; após reinício, o plugin retorna em estado seguro e o mapa é limpo no próximo ciclo administrativo ou agendado.

## Comandos administrativos

- `/valoriathrone start`
- `/valoriathrone stop`
- `/valoriathrone status`
- `/valoriathrone evacuate`
- `/valoriathrone spawn`
- `/valoriathrone reset`

Use os comandos apenas com permissões de administrador e confirme as mensagens do log do servidor.
