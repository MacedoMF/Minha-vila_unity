# Minha Vila — Unity

Port para Unity e C# do jogo Minha Vila. O projeto mantém as regras principais da versão 0.34.0 e substitui a execução em navegador/WebView por uma cena 3D isométrica gerada em tempo de execução.

## Abrir o projeto

1. Instale o Unity Hub e o Unity **6000.3.26f1 LTS**.
2. No Hub, escolha **Add project from disk** e selecione a pasta do projeto.
3. Abra `Assets/Scenes/MinhaVila.unity` e pressione **Play**.
4. Para Android, instale o módulo **Android Build Support** para essa versão do editor e selecione Android em **Build Profiles**.

A cena inicia o `GameBootstrap`, que monta a câmera, o terreno, os edifícios e a interface sem depender de pacotes de modelos externos.

## Jogar

- Arraste o mouse ou um dedo para mover a câmera; use a roda do mouse ou o gesto de pinça para aproximar e afastar.
- Abra **Construir**, escolha um edifício e toque em um lote vazio. Edifícios de trabalho e casas precisam estar ligados por ruas para conectar moradores.
- Use a barra inferior para abrir a vila, pesquisa, missões, segurança, finanças, território e opções.
- O relógio começa às 08:00. Na velocidade 1×, um minuto do jogo passa a cada segundo real. As velocidades disponíveis são 1×, 2×, 5× e 10×.
- Janelas de gestão pausam o relógio. A partida também para quando o aplicativo fica fechado; não há progresso offline.
- Salve em **Mais → Salvar vila**. Os arquivos ficam no armazenamento persistente local do dispositivo.

## O que está incluído

- Construção e melhoria de casas, ruas, serviços, edifícios de trabalho e lazer.
- Conexões por ruas, empregos, produção de ouro e materiais, agricultura, imigração e felicidade.
- Pesquisa com pesquisadores trabalhando durante o expediente, além de desbloqueios de construções.
- Empréstimos, compra de recursos, expansão de território, missões e expedições.
- Guardas, desordem, ataques de goblins e bandidos, ameaça de dragão e melhorias reais.
- Ciclo de dia e noite, aldeões visíveis andando entre suas casas, trabalhos e espaços de lazer, oito cenários e múltiplos arquivos locais.
- Interface em português com controles de toque e janelas compactas para manter a vila visível.

## Estrutura

- `Assets/Scripts/Core`: regras, catálogos, estado serializável e simulação sem dependência da Unity.
- `Assets/Scripts/Unity`: cena procedural, controles, interface, iluminação e salvamento local.
- `Assets/Shaders`: material iluminado que utiliza cores por vértice.
- `Assets/Tests/Editor`: testes de regras de simulação para o Unity Test Framework.

## Base e limitações desta migração

A migração foi iniciada a partir de `MacedoMF/minha-vila`, versão `0.34.0`, commit `66e88de91dde3e707ac7e752549c352da41afcc8`. Os arquivos de salvamento da versão WebView/localStorage não são importados automaticamente; as gravações Unity usam outro formato e ficam no dispositivo.

Os modelos, aldeões, árvores e edifícios são montados por código e usam formas low-poly; esta entrega não depende de modelos, fontes ou texturas externos. O reprodutor procura faixas em `Assets/Resources/Audio/music-menu` e `Assets/Resources/Audio/music-village`, mas os arquivos de áudio da versão original ainda não foram incluídos. Os personagens são representações visuais simples, sem esqueleto ou animações de personagem.

O ambiente desta conversão não tem o Unity Editor nem o compilador .NET instalados. Portanto, o projeto e os testes foram preparados, mas a compilação, execução da cena e testes do Unity Test Framework ainda precisam ser verificados no Editor.
