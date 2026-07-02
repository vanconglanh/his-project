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

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '0cde9779-8b67-11ef-b09a-0242ac130002:1-22227755';

--
-- Table structure for table `sta_salary_info`
--

DROP TABLE IF EXISTS `sta_salary_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sta_salary_info` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `CODE` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Salary info code',
  `STAFF_ID` int NOT NULL COMMENT 'Reference to sta_staff',
  `SALARY_TYPE` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Type: MONTHLY, HOURLY, CONTRACT',
  `BASE_SALARY` decimal(15,2) NOT NULL COMMENT 'Base salary amount',
  `CURRENCY_CODE` varchar(3) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT 'VND' COMMENT 'Currency code',
  `PAY_FREQUENCY` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT 'MONTHLY' COMMENT 'Payment frequency',
  `EFFECTIVE_DATE` date NOT NULL COMMENT 'Effective date',
  `END_DATE` date DEFAULT NULL COMMENT 'End date (for historical records)',
  `HOURLY_RATE` decimal(10,2) DEFAULT NULL COMMENT 'Hourly rate (if applicable)',
  `OVERTIME_RATE` decimal(10,2) DEFAULT NULL COMMENT 'Overtime rate multiplier',
  `BONUS_ELIGIBLE` tinyint(1) DEFAULT '0' COMMENT 'Eligible for bonus',
  `BONUS_PERCENTAGE` decimal(5,2) DEFAULT NULL COMMENT 'Bonus percentage',
  `COMMISSION_ELIGIBLE` tinyint(1) DEFAULT '0' COMMENT 'Eligible for commission',
  `COMMISSION_STRUCTURE` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Commission structure (JSON)',
  `ALLOWANCES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Allowances breakdown (JSON)',
  `DEDUCTIONS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Deductions breakdown (JSON)',
  `BENEFITS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Benefits package (JSON)',
  `TAX_INFORMATION` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Tax information (JSON)',
  `PAYROLL_ACCOUNT_NUMBER` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Payroll account number',
  `PAYROLL_BANK_DETAILS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Payroll bank details (JSON)',
  `APPROVED_BY` int DEFAULT NULL COMMENT 'Salary approver',
  `APPROVED_AT` datetime DEFAULT NULL COMMENT 'Approval timestamp',
  `NEXT_REVIEW_DATE` date DEFAULT NULL COMMENT 'Next salary review date',
  `PERFORMANCE_MULTIPLIER` decimal(3,2) DEFAULT '1.00' COMMENT 'Performance multiplier',
  `NOTES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Additional notes',
  `CREATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `LAST_UPDATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `STATUS` int NOT NULL DEFAULT '1',
  `CREATED_BY` int DEFAULT NULL,
  `LAST_UPDATED_BY` int DEFAULT NULL,
  `LAST_UPDATED_PROGRAM` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UK_CODE` (`CODE`),
  KEY `IDX_STAFF_ID` (`STAFF_ID`),
  KEY `IDX_SALARY_TYPE` (`SALARY_TYPE`),
  KEY `IDX_EFFECTIVE_DATE` (`EFFECTIVE_DATE`),
  KEY `IDX_END_DATE` (`END_DATE`),
  KEY `IDX_STATUS` (`STATUS`),
  KEY `IDX_CREATED_AT` (`CREATED_AT`),
  KEY `FK_SALARY_APPROVER` (`APPROVED_BY`),
  KEY `IDX_STA_SALARY_STAFF_ID` (`STAFF_ID`),
  CONSTRAINT `FK_SALARY_APPROVER` FOREIGN KEY (`APPROVED_BY`) REFERENCES `sec_users` (`ID`),
  CONSTRAINT `FK_SALARY_STAFF` FOREIGN KEY (`STAFF_ID`) REFERENCES `sta_staff` (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Salary and Compensation Information';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sta_salary_info`
--

LOCK TABLES `sta_salary_info` WRITE;
/*!40000 ALTER TABLE `sta_salary_info` DISABLE KEYS */;
/*!40000 ALTER TABLE `sta_salary_info` ENABLE KEYS */;
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

-- Dump completed on 2026-05-22 22:19:48
