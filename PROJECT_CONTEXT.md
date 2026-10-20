# Contexto do projeto Minha Vila — Unity

## Estado da migração

- Repositório de destino: `MacedoMF/Minha-vila_unity`, branch `main`.
- Projeto-fonte: `MacedoMF/minha-vila`, versão `0.34.0`, commit-base `66e88de91dde3e707ac7e752549c352da41afcc8`.
- Editor alvo definido: Unity `6000.3.26f1` LTS.
- Linguagem: C#. `MinhaVila.Core` contém regras portáveis; `MinhaVila.Unity` contém cenas, entrada, UI, renderização e persistência Unity.
- Entrega: projeto de jogo inicializável com cena principal, construção, gestão e cenários. Importação de saves antigos, áudio original, teste no Editor e validação de build Android seguem pendentes.

## Mecânicas preservadas

- Tempo: começa no dia 1 às 08:00. Em 1×, um minuto de jogo corresponde a um segundo real; opções de velocidade: 1×, 2×, 5× e 10×. Trabalho ocorre das 08:00 às 18:00. Pausar, abrir janelas de gestão ou fechar o jogo interrompe o avanço. Não há progresso offline.
- Economia: ouro, madeira, pedra, ferro, comida, relíquias, produção por trabalhadores, compras, empréstimos com prazo e juros, e registro local por vila.
- Construção: casas, ruas, trabalho, lazer, pesquisa, segurança e melhorias. Emprego, acesso a lazer e pesquisa dependem da conexão ortogonal por ruas. Melhorias preservam edifício e moradores.
- Crescimento: capacidade de moradia, vagas de trabalho, felicidade, serviços, imigração e expansão progressiva do terreno.
- Pesquisa: oito pesquisas encadeadas; progresso depende de pesquisadores nos centros de pesquisa/academias durante o expediente.
- Missões: quatro missões com mercenários, recompensas, relíquias e efeito de expedições na frequência de ataques.
- Segurança: guardas, desordem, alertas, goblins, bandidos, dragão, batalhas, saques e melhorias reais; derrotas não destroem edifícios nem removem moradores.
- Apresentação: câmera isométrica, ciclo de iluminação, aldeões low-poly circulando e terreno/edifícios gerados por código. UI em português, responsiva para toque e projetada para manter o mundo visível.

## Salvamento e compatibilidade

`SaveStore` grava JSON versionado em `Application.persistentDataPath/villages`, com cópia de segurança local. O formato não importa automaticamente dados de `localStorage` do navegador ou do aplicativo Android antigo. Não há sincronização em nuvem.

## Verificação atual

- Testes adicionados em `Assets/Tests/Editor/VillageSimulationTests.cs` cobrem conexão rodoviária inicial, construir/desfazer e avanço do relógio.
- A máquina de conversão não tem Unity Editor, `dotnet`, `csc` ou `mcs`; nenhuma compilação ou execução Unity foi alegada. Abrir o projeto no Unity 6000.3.26f1 e executar os testes continua necessário.
- A música espera arquivos `music-menu` e `music-village` em `Assets/Resources/Audio`; os arquivos originais não acompanham esta migração.

## Diretrizes para próximas alterações

- Manter o código de domínio independente de `UnityEngine` sempre que possível.
- Atualizar este arquivo ao mudar mecânicas, arquitetura, versão de Unity, formato de save ou estado dos testes.
- Manter o repositório Unity separado do repositório WebView de origem.
- Não declarar build, playtest, compatibilidade móvel ou importação de saves sem validar o fluxo correspondente no Editor/dispositivo.
