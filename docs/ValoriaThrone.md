# Trono de Valoria

O plugin `MUnique.OpenMU.PlugIns.ValoriaThrone` controla o evento, o reinado imperial, as Eras e o acesso a Lands of Trials.

## Configuração

Use o painel administrativo para editar a configuração customizada do plugin. Um exemplo completo está em `docs/valoria-throne.example.json`.

Antes de habilitar, confirme:

- `EventServerId` usa o identificador interno baseado em zero. O texto mostrado ao jogador usa `EventServerId + 1`.
- `GuardianMonsterId` e `SeniorNpcId` referenciam definições existentes.
- `GuardianSpawnX/Y`, os monstros de apoio e a posição da Coroa estão em células caminháveis.
- `CrownVisualItemLevel` deve permanecer entre `0` e `15`. O cliente customizado reconhece por padrão o nível `15`.
- Os horários do agendamento são interpretados em `Schedule.TimeZone`.

As durações são `TimeSpan`. No JSON, use o formato `d.hh:mm:ss`.

## Comandos

Comandos de GM:

- `/valoriathrone start`: inicia uma execução quando o estado é `Idle`.
- `/valoriathrone stop`: encerra, limpa entidades temporárias e restaura as regras normais do mapa.
- `/valoriathrone status`: mostra estado, execução e Guardião.
- `/valoriathrone spawn`: cria o Guardião somente durante a inscrição.
- `/valoriathrone evacuate`: retira os jogadores do mapa no próprio servidor.
- `/valoriathrone reset`: encerra uma execução inconsistente.
- `/valoriathrone era <Ascension|Fortune|Freedom|Luck>`: alternativa administrativa para selecionar a Era.

Comandos de jogador:

- `/era <Ascension|Fortune|Freedom|Luck>`: permite ao Imperador escolher uma vez durante o prazo.
- `/imperador`: mostra Imperador, Guild Imperial, Era, benefício e término do reinado.

## Etapas

O fluxo é `Announcing`, `Preparing`, `RegistrationOpen`, `GuardianBattle`, `CrownOnGround`, `CrownCarried`, `CoronationInProgress`, `Finishing` e `Cooldown`. Os anúncios informam a duração e o servidor envia contagens regressivas.

Ao fechar Valley of Loren, jogadores de outros servidores são movidos apenas para o mapa de fallback do próprio servidor. Não ocorre troca forçada de servidor.

## Eras

- **Ascension**: multiplica a experiência concedida.
- **Fortune**: multiplica a chance de drops comuns.
- **Freedom**: permite warps normalmente bloqueados pelo estado PK.
- **Luck**: multiplica taxas falíveis da Chaos Machine e das joias configuradas.

Na Chaos Machine, o servidor calcula a taxa autoritativa no momento da mistura. Os filtros separam combinações regulares, tickets de evento e combinações customizadas. O máximo efetivo é limitado por `MaximumChaosMachineSuccessRate`.

Nas joias, somente Soul, Life e Harmony são afetadas por padrão. Chances garantidas de zero ou cem por cento não recebem multiplicador. O máximo efetivo é limitado por `MaximumJewelSuccessRate`.

## Lands of Trials

Durante um reinado válido, a Guild Imperial entra quando `AllowImperialGuild` está ativo. Alianças atuais entram quando `AllowAlliances` está ativo. O Gatekeeper configurado teleporta jogadores autorizados para `EntryPositionX/Y`.

Jogadores sem autorização são recusados. Personagens carregados diretamente no mapa são redirecionados ao mapa de fallback. Quando o reinado expira ou é substituído, jogadores presentes são evacuados.

## Persistência e migrações

O estado operacional usa snapshots e o reinado é armazenado dentro da configuração customizada do plugin. Esta versão não adiciona tabelas ou colunas relacionais, portanto não exige migration Entity Framework. A publicação deve aplicar normalmente qualquer migration pendente do OpenMU antes de iniciar o servidor.

## Limitações conhecidas

- O cliente oficial sem as extensões `0xFA` e `0xFB` continua jogável, mas não mostra a taxa autoritativa nem o marcador customizado.
- A taxa exibida é atualizada ao abrir a Chaos Machine e ao alterar o conteúdo. Uma mudança de Era com a janela já aberta aparece após a próxima alteração ou reabertura.
- O marcador do portador é enviado aos observadores presentes quando a posse muda. Um jogador que entra depois pode recebê-lo somente na próxima atualização da posse.
- O modelo da Coroa no chão depende dos recursos existentes de `MODEL_NPC_CROWN`; falhas de recurso não alteram a lógica de coleta no servidor.

## Validação manual

1. Habilite a configuração e valide Guardião, Senior, servidor e posições.
2. Inicie com `/valoriathrone start` e acompanhe anúncios e contagens regressivas.
3. Durante `RegistrationOpen`, entre pelo servidor configurado e use `/valoriathrone spawn`.
4. Mate o Guardião e confirme o modelo Crown no chão.
5. Colete a Coroa e confirme o marcador visual sem ocupar pet ou helm.
6. Morra, desconecte ou saia do mapa com o portador e confirme remoção do marcador e respawn.
7. Conclua a coroação, selecione cada Era e valide `/imperador`.
8. Na Era Luck, altere itens na Chaos Machine e compare a taxa exibida com o resultado efetivo.
9. Teste Soul, Life, Harmony e uma joia não configurada.
10. Valide entrada e expulsão de Lands of Trials para guild, aliança e jogador externo.
11. Encerre com `/valoriathrone stop` e confirme que Valley of Loren volta a aceitar acesso normal.
