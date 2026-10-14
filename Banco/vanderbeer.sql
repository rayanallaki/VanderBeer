-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: vanderbeer
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `itens_pedido`
--

DROP TABLE IF EXISTS `itens_pedido`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `itens_pedido` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PedidoId` int NOT NULL,
  `ProdutoId` int NOT NULL,
  `Quantidade` int NOT NULL,
  `PrecoUnitario` decimal(10,2) NOT NULL,
  `Subtotal` decimal(12,2) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uq_itens_produto` (`PedidoId`,`ProdutoId`),
  KEY `fk_itens_produto` (`ProdutoId`),
  CONSTRAINT `fk_itens_pedido` FOREIGN KEY (`PedidoId`) REFERENCES `pedidos` (`Id`),
  CONSTRAINT `fk_itens_produto` FOREIGN KEY (`ProdutoId`) REFERENCES `produtos` (`Id`),
  CONSTRAINT `chk_itens_preco` CHECK ((`PrecoUnitario` >= 0)),
  CONSTRAINT `chk_itens_quantidade` CHECK ((`Quantidade` > 0)),
  CONSTRAINT `chk_itens_subtotal` CHECK ((`Subtotal` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pedidos`
--

DROP TABLE IF EXISTS `pedidos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pedidos` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UsuarioId` int NOT NULL,
  `DataHora` datetime(6) NOT NULL,
  `PrecoTotal` decimal(12,2) NOT NULL,
  `Status` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Recebido',
  `Observacao` varchar(300) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `ChaveRequisicao` varchar(36) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uq_pedidos_requisicao` (`UsuarioId`,`ChaveRequisicao`),
  CONSTRAINT `fk_pedidos_usuario` FOREIGN KEY (`UsuarioId`) REFERENCES `usuarios` (`Id`),
  CONSTRAINT `chk_pedidos_status` CHECK ((`Status` in (_utf8mb4'Recebido',_utf8mb4'EmSeparacao',_utf8mb4'Pronto',_utf8mb4'Finalizado',_utf8mb4'Cancelado'))),
  CONSTRAINT `chk_pedidos_total` CHECK ((`PrecoTotal` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `produtos`
--

DROP TABLE IF EXISTS `produtos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `produtos` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Categoria` varchar(60) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Alcoolico` tinyint(1) NOT NULL DEFAULT '0',
  `Descricao` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `Preco` decimal(10,2) NOT NULL,
  `Estoque` int NOT NULL DEFAULT '0',
  `Ativo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  CONSTRAINT `chk_produtos_estoque` CHECK ((`Estoque` >= 0)),
  CONSTRAINT `chk_produtos_preco` CHECK ((`Preco` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usuarios`
--

DROP TABLE IF EXISTS `usuarios`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuarios` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Documento` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Rua` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Bairro` varchar(60) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Cep` varchar(9) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Numero` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Complemento` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `Telefone` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SenhaHash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Perfil` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Cliente',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uq_usuarios_documento` (`Documento`),
  UNIQUE KEY `uq_usuarios_email` (`Email`),
  CONSTRAINT `chk_usuarios_perfil` CHECK ((`Perfil` in (_utf8mb4'Cliente',_utf8mb4'Administrador')))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-10-14 19:32:43
