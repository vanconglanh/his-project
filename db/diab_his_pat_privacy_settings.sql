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

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '0cde9779-8b67-11ef-b09a-0242ac130002:1-22227765';

--
-- Table structure for table `pat_privacy_settings`
--

DROP TABLE IF EXISTS `pat_privacy_settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pat_privacy_settings` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `CODE` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Privacy settings code',
  `PATIENT_ID` int NOT NULL COMMENT 'Reference to pat_patients',
  `DATA_SHARING_CONSENT` tinyint(1) DEFAULT '0' COMMENT 'Consent for data sharing',
  `RESEARCH_OPT_IN` tinyint(1) DEFAULT '0' COMMENT 'Opt in for research',
  `MARKETING_OPT_OUT` tinyint(1) DEFAULT '1' COMMENT 'Opt out of marketing',
  `FAMILY_ACCESS_ALLOWED` tinyint(1) DEFAULT '0' COMMENT 'Family access allowed',
  `ALLOWED_FAMILY_MEMBERS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Allowed family members (JSON)',
  `EMERGENCY_DISCLOSURE_ALLOWED` tinyint(1) DEFAULT '1' COMMENT 'Emergency disclosure allowed',
  `LAW_ENFORCEMENT_DISCLOSURE_ALLOWED` tinyint(1) DEFAULT '0' COMMENT 'Law enforcement disclosure allowed',
  `INSURANCE_DISCLOSURE_ALLOWED` tinyint(1) DEFAULT '1' COMMENT 'Insurance disclosure allowed',
  `EMPLOYER_DISCLOSURE_ALLOWED` tinyint(1) DEFAULT '0' COMMENT 'Employer disclosure allowed',
  `MEDIA_DISCLOSURE_ALLOWED` tinyint(1) DEFAULT '0' COMMENT 'Media disclosure allowed',
  `PHOTO_USAGE_CONSENT` tinyint(1) DEFAULT '0' COMMENT 'Photo usage consent',
  `DATA_RETENTION_PERIOD` int DEFAULT '7' COMMENT 'Data retention period in years',
  `DATA_DELETION_REQUESTED` tinyint(1) DEFAULT '0' COMMENT 'Data deletion requested',
  `DELETION_REQUEST_DATE` datetime DEFAULT NULL COMMENT 'Deletion request date',
  `ANONYMIZATION_REQUESTED` tinyint(1) DEFAULT '0' COMMENT 'Anonymization requested',
  `ANONYMIZATION_DATE` datetime DEFAULT NULL COMMENT 'Anonymization date',
  `HIPAA_AUTHORIZATION_ON_FILE` tinyint(1) DEFAULT '0' COMMENT 'HIPAA authorization on file',
  `LAST_REVIEW_DATE` datetime DEFAULT NULL COMMENT 'Last privacy settings review',
  `NEXT_REVIEW_DATE` datetime DEFAULT NULL COMMENT 'Next privacy settings review',
  `PREFERENCES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Additional privacy preferences (JSON)',
  `NOTES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Privacy notes',
  `CREATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `LAST_UPDATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `STATUS` int NOT NULL DEFAULT '1',
  `CREATED_BY` int DEFAULT NULL,
  `LAST_UPDATED_BY` int DEFAULT NULL,
  `LAST_UPDATED_PROGRAM` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UK_CODE` (`CODE`),
  UNIQUE KEY `UK_PATIENT_ID` (`PATIENT_ID`),
  KEY `IDX_PATIENT_ID` (`PATIENT_ID`),
  KEY `IDX_DATA_SHARING_CONSENT` (`DATA_SHARING_CONSENT`),
  KEY `IDX_RESEARCH_OPT_IN` (`RESEARCH_OPT_IN`),
  KEY `IDX_DATA_DELETION_REQUESTED` (`DATA_DELETION_REQUESTED`),
  KEY `IDX_STATUS` (`STATUS`),
  KEY `IDX_CREATED_AT` (`CREATED_AT`),
  KEY `IDX_PAT_PRIVACY_PATIENT_ID` (`PATIENT_ID`),
  CONSTRAINT `FK_PRIVACY_PATIENT` FOREIGN KEY (`PATIENT_ID`) REFERENCES `pat_patients` (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Patient Privacy Settings and Preferences';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pat_privacy_settings`
--

LOCK TABLES `pat_privacy_settings` WRITE;
/*!40000 ALTER TABLE `pat_privacy_settings` DISABLE KEYS */;
/*!40000 ALTER TABLE `pat_privacy_settings` ENABLE KEYS */;
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

-- Dump completed on 2026-05-22 22:19:52
