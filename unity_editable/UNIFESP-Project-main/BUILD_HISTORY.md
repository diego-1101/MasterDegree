# Histórico de builds

Este arquivo registra as builds distribuídas do Unity. A pasta de saída
continua sendo `SecondDisplayIntegrationBuild`; cada rebuild atualiza essa
mesma build. O histórico fica na raiz do projeto para não ser removido pela
geração dos arquivos do Player.

## Como registrar uma alteração

Ao alterar o Unity e gerar uma nova build:

1. Acrescente uma entrada no topo de **Histórico**; não apague entradas antigas.
2. Registre a data, versão do Unity, plataforma, scripts/recursos alterados e
   o comportamento que mudou.
3. Registre o resultado da build e as verificações feitas. Diferencie build
   compilada de comportamento testado no executável.
4. Gere o SHA-256 do executável e do assembly principal depois da build:

   ```powershell
   Get-FileHash ".\SecondDisplayIntegrationBuild\UNIFESP Project.exe" -Algorithm SHA256
   Get-FileHash ".\SecondDisplayIntegrationBuild\UNIFESP Project_Data\Managed\Assembly-CSharp.dll" -Algorithm SHA256
   ```

5. Salve o log de build em `Logs/` e coloque o caminho nesta entrada.
6. Não crie uma pasta de build por versão: atualize `SecondDisplayIntegrationBuild`
   e registre a nova versão aqui.

Use este modelo para cada entrada:

```markdown
## AAAA-MM-DD — identificador curto

- Unity / plataforma:
- Alterações e arquivos:
- Build: sucesso/falha; executável:
- SHA-256 do EXE:
- SHA-256 de Assembly-CSharp.dll:
- Verificações executadas:
- Limitações ou testes pendentes:
- Log:
```

O projeto contém o método `SecondDisplayIntegrationBuilder.Build` em
`Assets/Editor/SecondDisplayIntegrationBuilder.cs`. Ele compila as cenas
habilitadas para Windows 64-bit e usa `SecondDisplayIntegrationBuild` como
destino padrão. Se for necessário escolher outro destino temporário, defina
`UNIFESP_BUILD_OUTPUT` antes de chamar o método; não deixe uma build alternativa
como destino oficial.

## Histórico

## 2026-10-29 — plano de trials automático na aquisição GRAZ

- Unity / plataforma: 6000.4.4f1 / Windows 64-bit.
- Alterações e arquivos: `Experiment2RedesignedMenu.cs` passa a exportar o plano
  JSON e CSV da sequência efetivamente iniciada ao lado do log de eventos do
  Unity, com prefixo igual e sufixo `_events.plan`. A exportação usa o seed da
  sequência já gerada. Se o logger não existir ou a exportação falhar, o
  protocolo não inicia. `SecondDisplayIntegrationBuilder.cs` aceita o destino
  de build por `UNIFESP_BUILD_OUTPUT` e mantém a pasta padrão como destino usual.
- Build: sucesso; executável em `SecondDisplayIntegrationBuild/UNIFESP Project.exe`.
- SHA-256 do EXE: `413A04009D5FC13E03FE4E5C6941A8606C63C103EDD08853EFC6C9BD57E6151B`.
- SHA-256 de Assembly-CSharp.dll: `783C029F3CC70362E18B05F1EBA5E160FCDA79ED2059EF3C5B77B45BBAB4A951`.
- Verificações executadas: Unity batch build terminou com código 0; sem erros
  C# no log; assembly resultante confere com a build temporária de conferência.
- Limitações ou testes pendentes: ainda não foi executada uma aquisição no
  Player para confirmar visualmente a criação do plano ao lado do CSV de eventos.
- Log: `Logs/codex_nan_plan_build_standard.log`.

### Baseline anterior

A build que estava em `SecondDisplayIntegrationBuild` foi substituída por esta
versão. Não havia um hash do executável anterior registrado neste histórico;
por isso, a identidade binária da baseline anterior não pode ser recuperada
com confiança. Em novas alterações, registre ambos os hashes antes de
distribuir a build.
