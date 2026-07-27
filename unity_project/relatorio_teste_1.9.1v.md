# Relatório de Análise: Build Unity `teste_1.9.1v` (BCI Híbrida)

Este relatório detalha o funcionamento, estrutura e as mecânicas encontradas no build compilado **`teste_1.9.1v`** (executável `UNIFESP Project.exe`). Como a pasta continha a versão compilada do projeto, foi realizada uma descompilação da biblioteca principal (`Assembly-CSharp.dll`) para analisar diretamente o código-fonte (C#) das lógicas implementadas na engine.

---

## 1. Visão Geral do Projeto
O aplicativo Unity funciona como um **Ambiente de Realidade Virtual / Feedback Visual** para experimentos de Interface Cérebro-Máquina (BCI) Híbrida. Ele exibe um avatar (cujas pernas/próteses podem ser configuradas) e fornece estímulos e feedbacks visuais rigorosamente temporizados. 

A comunicação com os biossinais (EEG/EMG) é feita via **LSL (Lab Streaming Layer)**, o que permite sincronizar os dados processados em Python diretamente com a engine do Unity em tempo real.

## 2. Como Funciona a Integração BCI (LSL)
A ponte de dados entre o processamento de sinais e o Unity é feita pelos scripts de recepção LSL:
- **`LSLReceiver.cs`**: Conecta-se a um stream de nome padrão `"Python_EEG"`. Ele extrai os arrays de dados (canais) a cada frame (via `pull_sample`) e envia os valores resultantes para o `PlayerController`, atualizando os ângulos ou a força aplicada nas pernas do avatar virtual.
- **`MultiLSLReceiver.cs`**: É um script genérico capaz de se conectar a múltiplos fluxos de dados biológicos (ex: frequência cardíaca, temperatura, resistência galvânica, aceleração X/Y). Ele traduz essas métricas em mudanças na cor (material) ou transformações de posição (Translate) no ambiente.

## 3. Protocolo de Graz (`GrazController.cs`)
O coração do experimento é guiado pelo `GrazController`, que orquestra a clássica **paradigma de Imagética Motora de Graz**. Ele divide o experimento em blocos de tentativas (*trials*), iterando estritamente pelas seguintes fases:

1. **Baseline Phase (`BASE`)**: Período de repouso inicial para capturar a linha de base dos sinais. (duração configurável, ex: 10s).
2. **Fixation Phase (`FIX`)**: Exibe uma cruz de fixação na tela, preparando o usuário para o estímulo.
3. **Cue Phase (`CUE`)**: Apresenta uma seta direcional (Esquerda/Direita) indicando qual membro o usuário deve imaginar ou mover. Neste exato momento, o `BallLauncher` é acionado, disparando uma bola em direção ao alvo.
4. **Feedback Phase (`FEED`)**: O usuário tenta realizar a tarefa (via BCI). O sistema avalia o acerto ou erro da predição com base no input LSL. Ao final, o script `FeedbackController` exibe animações na tela ("Parabéns!" ou "Errou") de acordo com o desempenho.
5. **Pause Phase (`PAUSE`)**: Intervalo aleatório de descanso (entre `minPauseDuration` e `maxPauseDuration`) antes do próximo trial.

## 4. Sistema de Feedback e Interação
O feedback visual não se limita a textos; ele possui componentes interativos contínuos e discretos:

### Feedback Contínuo (Medidor / Gauge)
O script **`MedidorMovement.cs`** implementa um medidor (como uma barra de progresso ou de força) que se move verticalmente em resposta à força/intenção detectada pela BCI (os dados do LSL alteram o `inputValue`).
- O jogador precisa manter o cursor dentro de uma zona de alvo (overlap) durante um tempo determinado de precisão (`_tempoPrecisao`).
- Se conseguir, é contabilizado um **"ACERTOU"**; caso saia muito da zona ou não atinja, é logado um **"ERROU"**.

### Feedback Discreto (Lançamento de Bolas)
Scripts como **`BallLauncher.cs`** e **`BallController.cs`** cuidam do disparo físico de projéteis em direção ao jogador no momento do *Cue*.
- A bola viaja com uma parábola calculada matematicamente em direção ao jogador.
- Ao colidir com o jogador (Avatar), verifica-se o acerto temporal. É um estímulo forte e gamificado para o fechamento de loop.

## 5. Avatar e Personalização (`PlayerController.cs`)
A representação física do usuário no ambiente virtual é altamente modular:
- **Escolha de Membros (`LegChoice`)**: Permite selecionar dinamicamente entre pernas orgânicas e próteses. O usuário pode ter uma prótese esquerda, direita, ambas ou pernas normais.
- **Cinemática**: O `PlayerController` pega os valores de contração/imagética recebidos pelo LSL e os envia para os scripts `RaiseLeg.cs`, controlando o ângulo de flexão da perna em tempo real (`GetBothLegsAngles()`).
- **Acessibilidade/Inclusão**: O script permite mudar a cor da pele dos materiais (`ChangeSkinColor`) e até alterar a forma corporal usando *BlendShapes* de gênero (`ChangeGen`).
- **Câmeras**: Controla a troca e angulação entre câmera principal e lateral para melhor observação do experimento.

## 6. Sistema de Log em Tempo Real (`RealtimeLog.cs`)
Para garantir a rigorosidade científica da coleta de dados, o aplicativo registra constantemente (a cada *frame* ou evento):
- A fase atual do protocolo de Graz (Marcadores 1 a 5).
- A posição da bola, horários exatos das colisões.
- A classe predita, os ângulos reais das pernas na engine e os valores crus recebidos do Python.
- O veredito do trial (ACERTOU / ERROU / NEUTRO).
Tudo isso serve para sincronização e validação *post-hoc* com os dados neurais extraídos em Python (MNE/EEGLab).

---

### Conclusão e Próximos Passos
O build `teste_1.9.1v` é um ambiente **altamente especializado** para BCI. O fluxo de dados é:
1. Sinais (EEG/EMG) são captados e processados em um script externo (Python).
2. Python envia predições/features para o LSL.
3. O Unity (`LSLReceiver`) capta o LSL e injeta no `PlayerController` e no `Medidor`.
4. O `GrazController` dita o tempo exato em que o usuário deve tentar mover, garantindo que a bola, as animações e os logs estejam todos sincronizados.

Se formos refatorar ou modificar a interface Unity via **Unity MCP** futuramente, os pontos de injeção de interface seriam primariamente as lógicas atreladas ao `GameUIUm.cs` / `GameUIDois.cs` e os comportamentos de *Cue/Feedback* gerenciados pelo `GrazController.cs`.
