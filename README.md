# MVP de E-commerce - Validação do Padrão Saga Coreografado

Este repositório contém a implementação do MVP de um e-commerce desenvolvido em **.NET (C#)** para o Trabalho de Conclusão de Curso (TCC). O objetivo principal do projeto é validar o **Padrão Saga Baseado em Coreografia (Event-Driven)** para gerenciar transações distribuídas, assegurando a **consistência eventual** entre microsserviços sem a necessidade de bloqueios síncronos como o protocolo Two-Phase Commit (2PC).

---

## 📖 1. Visão Geral do Projeto e o Problema Resolvido

No modelo tradicional monolítico, a consistência dos dados é garantida por transações ACID locais controladas pelo banco de dados relacional. Contudo, ao migrar para uma arquitetura de microsserviços com o padrão **Database per Service** (um banco de dados para cada serviço), as transações ACID clássicas não podem ser aplicadas diretamente entre diferentes limites físicos de rede.

### O Desafio do Acoplamento e Bloqueio (2PC)
O protocolo *Two-Phase Commit (2PC)* é uma alternativa clássica para consistência forte distribuída, mas apresenta sérias desvantagens:
1. **Ponto Único de Falha:** O coordenador centralizado pode se tornar um gargalo ou falhar.
2. **Latência Elevada:** Exige o bloqueio de recursos em todos os nós participantes até a conclusão de todas as fases da transação.
3. **Falta de Escalabilidade:** Reduz drasticamente a tolerância a falhas e a performance global do sistema sob alta carga.

### A Solução: Padrão Saga Coreografado
O **Padrão Saga** resolve o problema dividindo uma transação de negócios global em uma série de transações locais sequenciais em cada microsserviço relevante. Cada transação local atualiza o banco de dados do respectivo serviço e publica um evento de integração.

Adotamos a **Coreografia** (em vez da Orquestração), o que significa que os microsserviços interagem de forma descentralizada e autônoma por meio da troca de eventos assíncronos:
- **Desacoplamento Temporal e Espacial:** Os serviços não sabem da existência direta um do outro; eles apenas reagem e publicam eventos em filas.
- **Rollback Semântico:** Se um passo da transação falhar (ex: falta de estoque), a Saga dispara de forma reversa uma série de **transações compensatórias** para desfazer os efeitos das transações anteriores, mantendo o sistema em um estado consistente eventual.

---

## 🏗️ 2. Arquitetura e Stack Tecnológica

O projeto foi projetado utilizando princípios de **Domain-Driven Design (DDD)** e **Clean Architecture** para manter o domínio de negócios limpo e testável.

- **Linguagem & Runtime:** C# (.NET 8+ / .NET 10)
- **Framework ORM:** Entity Framework Core
- **Bancos de Dados:** PostgreSQL (Instâncias isoladas via Docker para cada serviço, respeitando *Database per Service*)
- **Mensageria & Filas:** RabbitMQ (utilizando a biblioteca assíncrona moderna `RabbitMQ.Client` e troca do tipo `Topic` para a coreografia)
- **Ambiente de Execução:** Docker e Docker Compose

### Arquitetura de Camadas (DDD/Clean Architecture)
Cada microsserviço segue a seguinte divisão interna (exemplo de `OrderAPI` / `Pedido`):
- `Domain`: Regras e entidades puras de negócio (ex: entidade rica `Pedido` com setters privados e métodos expressivos como `MarcarComoAprovado()`).
- `Application`: Casos de uso, DTOs, validações (`FluentValidation`) e orquestração de fluxo de domínio.
- `Infrastructure`: Detalhes técnicos, contexto do EF Core (`PedidoDbContext`) e envio de eventos para o RabbitMQ.
- `API`: Pontos de entrada HTTP controladores REST.

---

## 🧩 3. Descrição dos Microsserviços

O sistema é composto por três microsserviços essenciais para o fluxo de compra:

### A. `OrderAPI` (Serviço de Pedidos / PedidoAPI)
- **Papel:** Ponto de entrada da jornada de compra do usuário.
- **Responsabilidades:**
  - Recebe a requisição HTTP POST para criação do pedido.
  - Salva o pedido no `OrderDb` com o status inicial `Pendente`.
  - Publica o evento `OrderCreatedEvent` (`pedido.criado`).
  - Escuta os eventos de sucesso e falha da Saga para atualizar o status do pedido para `Aprovado` (sucesso) ou `Rejeitado/Cancelado` (compensação).

### B. `InventoryAPI` (Serviço de Estoque)
- **Papel:** Garante a disponibilidade física das mercadorias.
- **Responsabilidades:**
  - Escuta o evento `OrderCreatedEvent`.
  - Executa a validação e reserva dos itens no `InventoryDb`.
  - Caso haja estoque suficiente: realiza a reserva e publica `InventoryReservedEvent` (`estoque.reservado`).
  - Caso o estoque esteja zerado ou insuficiente: publica `OutOfStockEvent` (`estoque.insuficiente`).

### C. `PaymentAPI` (Serviço de Pagamento)
- **Papel:** Processa a transação financeira da compra.
- **Responsabilidades:**
  - Escuta o evento `InventoryReservedEvent`.
  - Executa o processamento do pagamento no `PaymentDb`.
  - Caso o pagamento seja processado com sucesso: publica `PaymentApprovedEvent` (`pagamento.aprovado`).
  - Caso o pagamento seja recusado (saldo insuficiente, cartão inválido): publica `PaymentRejectedEvent` (`pagamento.rejeitado`), o que acionará a compensação no estoque e no pedido.

---

## 🔄 4. Funcionamento do Padrão Saga Coreografado

A transação distribuída ocorre de forma reativa guiada pela coreografia baseada em eventos. A seguir, detalhamos os dois fluxos principais:

### 🟢 Fluxo de Sucesso (Happy Path)
1. O cliente envia uma requisição `POST /api/pedidos` para a `OrderAPI`.
2. A `OrderAPI` cria o registro do pedido no `OrderDb` com status `Pendente` e publica `OrderCreatedEvent` na fila.
3. A `InventoryAPI` consome `OrderCreatedEvent`, valida a disponibilidade física do item, realiza a reserva no `InventoryDb` e publica `InventoryReservedEvent`.
4. A `PaymentAPI` consome `InventoryReservedEvent`, processa a cobrança no `PaymentDb` e publica `PaymentApprovedEvent`.
5. A `OrderAPI` consome `PaymentApprovedEvent`, atualiza o status do pedido no banco para `Aprovado` e encerra a Saga com sucesso.

### 🔴 Fluxo de Falha e Compensação (Rollback Semântico)
1. O cliente envia uma requisição `POST /api/pedidos` para a `OrderAPI`.
2. A `OrderAPI` cria o registro do pedido no `OrderDb` com status `Pendente` e publica `OrderCreatedEvent`.
3. A `InventoryAPI` consome `OrderCreatedEvent`, mas identifica que não há saldo disponível no estoque para os itens selecionados.
4. A `InventoryAPI` publica o evento `OutOfStockEvent` sinalizando a falha.
5. A `OrderAPI` consome o `OutOfStockEvent` e executa a **transação compensatória**: altera o status do pedido no banco `OrderDb` para `Rejeitado` (ou `Cancelado`), notificando o usuário que a compra não pôde ser processada devido à indisponibilidade de estoque.

---

## 🛠️ 5. Instruções para Execução e Testes

### Pré-requisitos
- .NET 8+ / .NET 10 SDK instalado
- Docker e Docker Compose instalados

### Passo 1: Inicializar a Infraestrutura (Bancos e Broker)
No terminal na raiz do projeto, execute o comando para subir os containers do PostgreSQL e do RabbitMQ em segundo plano:
```bash
docker-compose up -d
```
Isso iniciará:
- **RabbitMQ:** Acessível na porta `5673` (AMQP) e console de administração na porta `15673` (Credenciais: `guest` / `guest`).
- **OrderDb (PostgreSQL):** Porta `5432`
- **PaymentDb (PostgreSQL):** Porta `5433`
- **InventoryDb (PostgreSQL):** Porta `5434`

### Passo 2: Executar a API de Pedidos
Execute a API de Pedidos (OrderAPI) a partir do diretório raiz:
```bash
dotnet run --project src/Services/Pedido/SagaEcommerce.Pedido.API/SagaEcommerce.Pedido.API.csproj --launch-profile http
```
*(Nota: As migrations do banco de dados são aplicadas automaticamente no startup do serviço, gerando a tabela `Pedidos` no PostgreSQL)*

### Passo 3: Testar a Criação de Pedido (Publicação de Evento)
Dispare a chamada HTTP POST utilizando curl:
```bash
curl -i -X POST -H "Content-Type: application/json" \
  -d '{"clienteId": "807c4b4d-91b7-4c31-8bc6-9faeb65239a5", "total": 129.99}' \
  http://localhost:5200/api/pedidos
```
Para inspecionar o evento gerado:
1. Acesse o console do RabbitMQ: [http://localhost:15673/](http://localhost:15673/)
2. Navegue até a fila `pedido-criado-queue` e consuma as mensagens para visualizar o payload JSON do evento.

---

## 🗺️ 6. Diagramas Arquiteturais (Lucidchart)

Os diagramas abaixo detalham a arquitetura do MVP e a dinâmica das mensagens da Saga Coreografada. Você pode visualizá-los e editá-los nos links abaixo:

### 🌐 Diagrama de Arquitetura de Contêineres / Infraestrutura
Este diagrama ilustra a divisão física dos serviços, seus respectivos bancos de dados PostgreSQL (Database per Service) e a comunicação assíncrona por meio do RabbitMQ.
*   **[Visualizar Diagrama no Lucidchart](https://lucid.app/lucidchart/008c3f7a-4a96-49af-bfbb-a5178f6ec780/view)**
*   **[Editar Diagrama no Lucidchart](https://lucid.app/lucidchart/008c3f7a-4a96-49af-bfbb-a5178f6ec780/edit)**

### 🔄 Diagrama de Sequência da Saga (Fluxos de Sucesso e Compensação)
Este diagrama demonstra o fluxo temporal de mensagens entre a OrderAPI, InventoryAPI, PaymentAPI e o RabbitMQ, ilustrando tanto o caminho feliz (Happy Path) quanto o rollback semântico (compensação).
*   **[Visualizar Diagrama de Sequência](https://lucid.app/lucidchart/e3f2d28d-93cf-4f43-b61d-9566cf2d40f7/view)**
*   **[Editar Diagrama de Sequência](https://lucid.app/lucidchart/e3f2d28d-93cf-4f43-b61d-9566cf2d40f7/edit)**
