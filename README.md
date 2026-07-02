# Spotiwake

Aplicativo de bandeja (system tray) para Windows 11 que **impede o computador de entrar em suspensão enquanto o Spotify estiver tocando**. Quando a música para (ou o Spotify é fechado), o controle de energia volta ao normal e o Windows pode suspender como de costume.

Projeto de **código aberto** sob a licença MIT — todo o código-fonte está neste repositório e pode ser auditado e compilado por qualquer pessoa.

## Como funciona

A cada 5 segundos o Spotiwake verifica se o aplicativo desktop do Spotify está reproduzindo algo, usando duas estratégias combinadas:

1. **Título da janela** — quando o Spotify está tocando, o título da janela vira `Artista - Música`; pausado, volta a ser apenas `Spotify`, `Spotify Free` ou `Spotify Premium`.
2. **Medidor de áudio (WASAPI)** — verifica se o processo do Spotify tem uma sessão de áudio ativa com som saindo no dispositivo padrão. Isso cobre casos em que o título não ajuda (anúncios, janela fechada para a bandeja etc.).

Se estiver tocando, o app chama a API do Windows `SetThreadExecutionState` com `ES_SYSTEM_REQUIRED`, o mecanismo oficial para manter o sistema acordado — o mesmo usado por players de vídeo. Nenhuma configuração de energia do Windows é alterada: quando a música para, basta o próprio timeout normal do sistema agir.

## Ícone da bandeja

| Ícone | Significado |
|---|---|
| 🟢 Verde | Spotify tocando — suspensão bloqueada |
| ⚪ Cinza | Monitorando — Spotify pausado ou fechado |
| 🚫 Cinza com traço | Proteção desativada pelo usuário |

## Menu (clique com o botão direito)

- **Status** — mostra o estado atual da detecção.
- **Ativado** — liga/desliga a proteção (duplo clique no ícone também alterna).
- **Manter a tela ligada também** — além de impedir a suspensão, impede que o monitor desligue.
- **Iniciar com o Windows** — registra o app na inicialização do usuário atual (sem precisar de administrador).
- **Sair** — encerra o app e libera imediatamente o controle de energia.

As preferências ficam salvas em `%AppData%\Spotiwake\settings.json`.

## Download

⬇️ **[Baixar Spotiwake.exe (última versão)](https://github.com/ggaspari/Spotiwake/releases/latest/download/Spotiwake.exe)**

O aplicativo é **portátil**: é um único `Spotiwake.exe` autossuficiente — não tem instalador, não precisa do .NET nem de nada mais instalado. Basta salvar o arquivo em qualquer pasta e executar. Para remover, é só apagar o arquivo (e, se tiver ativado "Iniciar com o Windows", desmarcar a opção no menu antes).

Todas as versões ficam na página de [releases](https://github.com/ggaspari/Spotiwake/releases). Builds de desenvolvimento também são gerados a cada push, na aba **Actions** (artefato `Spotiwake-win-x64`).

## Aviso do SmartScreen e verificação de integridade

Na primeira execução, o **Windows SmartScreen pode exibir um alerta** ("O Windows protegeu o computador"). Isso acontece porque o executável não tem assinatura digital de um certificado comercial — não porque haja algo de errado com ele. Para executar, clique em **Mais informações → Executar assim mesmo**.

Este é um projeto de código aberto: se preferir não confiar no binário, você pode [compilar a partir do código-fonte](#compilando-localmente) e obter o mesmo aplicativo.

Você também pode verificar se o arquivo baixado é exatamente o que foi publicado, conferindo o hash SHA-256 no PowerShell:

```powershell
Get-FileHash .\Spotiwake.exe -Algorithm SHA256
```

| Versão | SHA-256 do `Spotiwake.exe` |
|---|---|
| v1.0.0 | `0680322cdfca317f7cdf731c3609d2b29d3c999f80b163d2947bd578f443dc95` |

A partir da próxima versão, cada release também inclui um arquivo `Spotiwake.exe.sha256` gerado automaticamente pelo CI, com o hash correspondente.

## Compilando localmente

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):

```powershell
dotnet publish src/Spotiwake/Spotiwake.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

O executável fica em `publish\Spotiwake.exe`. Para desenvolvimento, basta `dotnet run --project src/Spotiwake`.

## Limitações

- Detecta apenas o **aplicativo desktop** do Spotify (instalador tradicional ou Microsoft Store). O Spotify Web Player no navegador não é detectado.
- O medidor de áudio considera o dispositivo de saída **padrão**; se o Spotify tocar em outro dispositivo, a detecção por título da janela continua funcionando.

## Licença

[MIT](LICENSE)
