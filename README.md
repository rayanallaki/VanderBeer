# VanderBeer

Projeto acadêmico de uma loja de bebidas alcoólicas e não alcoólicas.

O cliente consulta o catálogo e faz pedidos pelo aplicativo. A loja utiliza a aplicação web para gerenciar produtos, estoque e pedidos. As duas aplicações se comunicam com a API, responsável pelas regras de negócio e pelo acesso ao MySQL.

## Tecnologias utilizadas

- API: C# e ASP.NET Core.
- Aplicativo: .NET MAUI e SQLite.
- Aplicação web: ASP.NET Core MVC.
- Banco central: MySQL.
- Ambiente de desenvolvimento: Visual Studio Code.
- Controle de versão: Git e GitHub.

O projeto utiliza .NET 10. A versão do aplicativo utilizada na apresentação é a versão Windows do MAUI.

## Organização

- `Loja.Api`: endpoints, validações e acesso ao MySQL.
- `Loja.Mobile`: catálogo, cadastro, login, carrinho e pedidos.
- `Loja.Web`: gerenciamento da loja.
- `Banco/vanderbeer.sql`: estrutura do banco de dados.

## Funcionalidades

### Aplicativo

- Cadastro e login de clientes.
- Consulta de bebidas e informações de estoque.
- Sincronização do catálogo com o SQLite.
- Consulta do catálogo salvo sem conexão com a API.
- Carrinho com alteração de quantidades e remoção de itens.
- Envio de pedidos.
- Consulta do histórico e dos status dos pedidos.

### Aplicação web

- Login administrativo.
- Cadastro e edição de produtos.
- Ativação e desativação de produtos.
- Ajuste de estoque.
- Consulta e filtragem de pedidos.
- Atualização do status dos pedidos.

### API

- Autenticação por cookies.
- Controle de acesso de clientes e administradores.
- Validação dos preços e das quantidades.
- Cálculo dos subtotais e do total do pedido.
- Registro do pedido e desconto do estoque em uma transação.
- Controle de compras simultâneas.
- Devolução do estoque ao cancelar um pedido.
- Verificação da chave de requisição para evitar pedidos duplicados em reenvios.

## Status dos pedidos

As alterações permitidas são:

- Recebido → EmSeparacao ou Cancelado.
- EmSeparacao → Pronto ou Cancelado.
- Pronto → Finalizado.

Pedidos finalizados ou cancelados não podem ser reabertos.

## Preparação do ambiente

É necessário ter:

- Windows.
- .NET SDK 10.
- Suporte ao .NET MAUI para Windows.
- MySQL Server 8.0.
- MySQL Workbench ou outro cliente SQL.

### 1. Criar o banco

Abra `Banco/vanderbeer.sql` no MySQL Workbench e execute o script em um ambiente de desenvolvimento.

O arquivo contém a estrutura das tabelas. Os cadastros e pedidos usados nos testes não estão incluídos.

### 2. Criar o usuário do banco

Em uma conexão administrativa do MySQL, execute os comandos abaixo, substituindo `SUA_SENHA_LOCAL` por uma senha escolhida para o ambiente:

```sql
CREATE USER 'vanderbeer_api'@'127.0.0.1'
IDENTIFIED BY 'SUA_SENHA_LOCAL';

GRANT SELECT, INSERT, UPDATE, DELETE
ON vanderbeer.*
TO 'vanderbeer_api'@'127.0.0.1';
```

Essa criação é necessária quando o usuário ainda não existe no servidor.

### 3. Configurar a conexão da API

No terminal, dentro da pasta `Loja.Api`, execute:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MySql" "Server=127.0.0.1;Port=3306;Database=vanderbeer;User ID=vanderbeer_api;Password=SUA_SENHA_LOCAL;"
```

Utilize a mesma senha definida no MySQL. A configuração é armazenada localmente pelo User Secrets.

## Executar o projeto

Abra um terminal para cada aplicação. Execute os comandos a partir da pasta principal do projeto.

### API

```powershell
cd Loja.Api
dotnet run --urls http://localhost:5103
```

### Aplicação web

Em outro terminal:

```powershell
cd Loja.Web
dotnet run --urls http://localhost:5200
```

Acesse:

http://localhost:5200/Conta/Login

### Aplicativo MAUI no Windows

Em outro terminal:

```powershell
cd Loja.Mobile
dotnet run -f net10.0-windows10.0.19041.0
```

Mantenha a API em execução para cadastrar usuários, sincronizar produtos e enviar ou consultar pedidos.

## Primeiro acesso administrativo

1. Cadastre uma conta pelo aplicativo.
2. No MySQL Workbench, altere o perfil dessa conta:

```sql
USE vanderbeer;

UPDATE usuarios
SET Perfil = 'Administrador'
WHERE Email = 'EMAIL_DA_CONTA_CADASTRADA';
```

3. Entre na aplicação web com o e-mail e a senha dessa conta.
4. Cadastre os produtos pela aplicação web.
5. Atualize o catálogo no aplicativo.

A promoção de uma conta para administrador é feita manualmente para a demonstração acadêmica.

## Armazenamento dos dados

O MySQL mantém os dados oficiais dos usuários, produtos, pedidos e itens dos pedidos.

O SQLite mantém uma cópia local do catálogo no aplicativo. Assim, os produtos já sincronizados podem ser consultados mesmo com a API desligada.

Os preços e o estoque exibidos no catálogo local podem mudar. Ao receber um pedido, a API verifica os dados atuais antes de registrar a compra.

## Testes realizados

- Cadastro e login de usuários.
- Restrição de operações administrativas.
- Cadastro, edição e ativação de produtos.
- Criação de pedidos e atualização de estoque.
- Reenvio de um pedido sem duplicação.
- Rejeição de pedidos com estoque insuficiente.
- Duas compras simultâneas disputando a última unidade.
- Cancelamento com devolução do estoque.
- Atualização do status entre a aplicação web e o aplicativo.
- Consulta do catálogo após fechar o aplicativo e desligar a API.