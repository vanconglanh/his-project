-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: 57.155.1.252    Database: diab_his
-- ------------------------------------------------------
-- Server version	8.0.23

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
SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

--
-- GTID state at the beginning of the backup 
--

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '0cde9779-8b67-11ef-b09a-0242ac130002:1-22227800';

--
-- Table structure for table `sec_data_masks`
--

DROP TABLE IF EXISTS `sec_data_masks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sec_data_masks` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `CODE` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Mask rule code',
  `NAME` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Mask rule name',
  `DESCRIPTION` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Mask rule description',
  `TABLE_NAME` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Target table name',
  `COLUMN_NAME` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Target column name',
  `MASK_TYPE` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Type: REPLACE, HASH, ENCRYPT, REDACT, etc.',
  `MASK_PATTERN` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Mask pattern (e.g., XXX-XXX-1234)',
  `MASK_CHARACTER` char(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT 'X' COMMENT 'Character to use for masking',
  `PRESERVE_LENGTH` tinyint(1) DEFAULT '1' COMMENT 'Preserve original length',
  `PRESERVE_FORMAT` tinyint(1) DEFAULT '0' COMMENT 'Preserve format (e.g., keep dashes)',
  `ALLOW_ORIGINAL_ACCESS` tinyint(1) DEFAULT '0' COMMENT 'Allow access to original data',
  `ACCESS_ROLES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Roles allowed to see original data (JSON)',
  `PURPOSE` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purpose: TESTING, TRAINING, ANALYTICS, etc.',
  `RETENTION_DAYS` int DEFAULT NULL COMMENT 'How long to retain masked data',
  `IS_ACTIVE` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Is rule active',
  `PRIORITY` int DEFAULT '0' COMMENT 'Rule priority (higher = applied first)',
  `TEST_DATA_ONLY` tinyint(1) DEFAULT '0' COMMENT 'Apply only to test data',
  `COMPLIANCE_STANDARD` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Compliance standard: HIPAA, GDPR, etc.',
  `CREATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `LAST_UPDATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `STATUS` int NOT NULL DEFAULT '1',
  `CREATED_BY` int DEFAULT NULL,
  `LAST_UPDATED_BY` int DEFAULT NULL,
  `LAST_UPDATED_PROGRAM` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UK_CODE` (`CODE`),
  UNIQUE KEY `UK_TABLE_COLUMN` (`TABLE_NAME`,`COLUMN_NAME`),
  KEY `IDX_MASK_TYPE` (`MASK_TYPE`),
  KEY `IDX_TABLE_NAME` (`TABLE_NAME`),
  KEY `IDX_IS_ACTIVE` (`IS_ACTIVE`),
  KEY `IDX_PURPOSE` (`PURPOSE`),
  KEY `IDX_STATUS` (`STATUS`),
  KEY `IDX_CREATED_AT` (`CREATED_AT`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Data Masking Rules';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sec_data_masks`
--

LOCK TABLES `sec_data_masks` WRITE;
/*!40000 ALTER TABLE `sec_data_masks` DISABLE KEYS */;
/*!40000 ALTER TABLE `sec_data_masks` ENABLE KEYS */;
UNLOCK TABLES;
SET @@SESSION.SQL_LOG_BIN = @MYSQLDUMP_TEMP_LOG_BIN;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-22 22:20:17
