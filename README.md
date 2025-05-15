# 🧠 MasterDegree: Hybrid Brain-Computer Interface for Virtual Prosthesis Control

# **US English version below**

This repository contains the code, data, and models for the master's research project developed at the Biomedical Engineering Graduate Program at UNIFESP. The main objective is to **implement and validate a non-invasive Hybrid Brain-Computer Interface (hBCI)** based on **corticomuscular coherence (CMC)** for real-time control of a virtual prosthesis using synchronized **EEG and EMG signals**.

The application is aimed at motor rehabilitation and neurofeedback, initially tested with healthy participants. The prosthesis is controlled in a **Virtual Reality (Unity)** environment with real-time visual feedback, closing the sensorimotor loop.

---

## 📂 Folder Structure
'''
MasterDegree/
├── data/
├── scripts/
├── models/
├── results/
├── notebooks/
├── unity_project/
├── requirements.txt
├── README.md
└── .gitignore
'''


### `data/`
Contains the EEG and EMG data used in the project:
- `raw/`: raw signals collected during acquisition.
- `preprocessed/`: filtered and segmented signals.

### `scripts/`
Python scripts divided by experimental pipeline stages:
- `acquisition/`: acquisition of EEG and EMG signals via LSL or serial devices.
- `preprocessing/`: filtering, artifact removal, and signal synchronization.
- `features/`: feature extraction including corticomuscular coherence, RMS, PSD, etc.
- `classification/`: SVM, LSTM, and neural network models for motor intent prediction.
- `realtime_control/`: real-time signal inference and prosthesis control.
- `utils/`: utility functions and helpers.
- `protocol/`: experimental task protocols implemented in PsychoPy.
- `interface_control/`: Unity prosthesis control via LSL or socket communication.

### `models/`
Trained models and saved checkpoints.

### `results/`
Experimental results, performance metrics, graphs, and statistical analyses.

### `notebooks/`
Jupyter notebooks for data exploration, visualization, and model validation.

### `unity_project/`
Unity project of the virtual prosthesis with control scripts and interactions.

### `requirements.txt`
Python dependencies required to run the project.

### `.gitignore`
Files and folders ignored by Git (e.g., temporary files, caches, large models).

---

# **Português**

# 🧠 MasterDegree: Interface Cérebro-Máquina Híbrida para Controle de Prótese Virtual

Este repositório contém os códigos, dados e modelos do projeto de mestrado desenvolvido no Programa de Pós-Graduação em Engenharia Biomédica da UNIFESP. O objetivo central do projeto é **implementar e validar uma Interface Cérebro-Máquina Híbrida (ICMH) não invasiva**, baseada na **coerência cortico-muscular (CCM)**, para controle em tempo real de uma prótese virtual, utilizando sinais de **EEG e EMG sincronizados**.

A aplicação visa contribuir com a reabilitação motora e o neurofeedback, sendo testada inicialmente com indivíduos hígidos. O controle da prótese ocorre em um ambiente de **Realidade Virtual (Unity)** com retorno visual responsivo, fechando o loop sensório-motor.

---

## 📂 Estrutura de Pastas
'''
'''MasterDegree/
├── data/
├── scripts/
├── models/
├── results/
├── notebooks/
├── unity_project/
├── requirements.txt
├── README.md
└── .gitignore'''

### `data/`
Contém os dados de EEG e EMG utilizados no projeto:
- `raw/`: sinais brutos adquiridos durante os experimentos.
- `preprocessed/`: sinais filtrados e segmentados.

### `scripts/`
Scripts organizados por etapas do pipeline experimental:
- `acquisition/`: aquisição de sinais EEG e EMG via LSL ou dispositivos seriais.
- `preprocessing/`: filtragem, remoção de artefatos e sincronização dos dados.
- `features/`: extração de características como coerência cortico-muscular, RMS, PSD, etc.
- `classification/`: modelos SVM, LSTM e redes neurais para predição de intenção motora.
- `realtime_control/`: inferência em tempo real e controle da prótese virtual.
- `utils/`: funções auxiliares gerais.
- `protocol/`: protocolos experimentais implementados em PsychoPy.
- `interface_control/`: controle da prótese em Unity via LSL ou sockets.

### `models/`
Modelos treinados e checkpoints salvos.

### `results/`
Resultados experimentais, métricas de desempenho, gráficos e análises estatísticas.

### `notebooks/`
Notebooks Jupyter para exploração, visualização e validação dos dados e modelos.

### `unity_project/`
Projeto Unity da prótese virtual com os scripts de controle e interação.

### `requirements.txt`
Lista de bibliotecas e dependências necessárias para executar o projeto em Python.

### `.gitignore`
Arquivos e pastas ignorados pelo Git (e.g., cache, arquivos temporários, modelos grandes).
