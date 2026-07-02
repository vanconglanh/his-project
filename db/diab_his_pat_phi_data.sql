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

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '0cde9779-8b67-11ef-b09a-0242ac130002:1-22227886';

--
-- Table structure for table `pat_phi_data`
--

DROP TABLE IF EXISTS `pat_phi_data`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pat_phi_data` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `CODE` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'PHI data code',
  `PATIENT_ID` int NOT NULL COMMENT 'Reference to pat_patients',
  `MEDICAL_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted medical history summary',
  `SURGICAL_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted surgical history',
  `CURRENT_MEDICATIONS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted current medications (JSON)',
  `ALLERGIES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted allergies (JSON)',
  `CHRONIC_CONDITIONS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted chronic conditions (JSON)',
  `PAST_HOSPITALIZATIONS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted past hospitalizations (JSON)',
  `FAMILY_MEDICAL_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted family medical history',
  `SOCIAL_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted social history',
  `MENTAL_HEALTH_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted mental health history',
  `SUBSTANCE_ABUSE_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted substance abuse history',
  `IMMUNIZATION_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted immunization history (JSON)',
  `PREGNANCY_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted pregnancy history (for females)',
  `MENSTRUAL_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted menstrual history (for females)',
  `SEXUAL_HEALTH_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted sexual health history',
  `DEVELOPMENTAL_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted developmental history (for pediatrics)',
  `FUNCTIONAL_STATUS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted functional status',
  `COGNITIVE_STATUS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted cognitive status',
  `BEHAVIORAL_HEALTH` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted behavioral health information',
  `PAIN_ASSESSMENT` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted pain assessment',
  `QUALITY_OF_LIFE` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted quality of life assessment',
  `ADVANCE_DIRECTIVE_DETAILS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted advance directive details',
  `LIVING_WILL` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted living will content',
  `POWER_OF_ATTORNEY_MEDICAL` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted medical power of attorney',
  `DNAR_DETAILS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted DNR/DNAR details',
  `ORGAN_DONATION_CONSENT` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted organ donation consent',
  `RESEARCH_CONSENT` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted research consent details',
  `CLINICAL_TRIALS_PARTICIPATION` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted clinical trials participation (JSON)',
  `GENETIC_INFORMATION` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted genetic information',
  `HIV_STATUS` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Encrypted HIV status',
  `STD_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted STD history',
  `MENTAL_HEALTH_DIAGNOSIS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted mental health diagnoses (JSON)',
  `PSYCHIATRIC_MEDICATIONS` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted psychiatric medications (JSON)',
  `THERAPY_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted therapy history',
  `SUBSTANCE_ABUSE_TREATMENT` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted substance abuse treatment history',
  `REHABILITATION_HISTORY` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Encrypted rehabilitation history',
  `ENCRYPTION_KEY_ID` int NOT NULL COMMENT 'Encryption key used',
  `ENCRYPTION_VERSION` int DEFAULT '1' COMMENT 'Encryption version',
  `LAST_ACCESSED_AT` datetime DEFAULT NULL COMMENT 'Last access timestamp',
  `ACCESS_COUNT` int DEFAULT '0' COMMENT 'Number of times accessed',
  `SENSITIVITY_LEVEL` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT 'STANDARD' COMMENT 'Sensitivity: LOW, STANDARD, HIGH, CRITICAL',
  `REQUIRES_SPECIAL_ACCESS` tinyint(1) DEFAULT '0' COMMENT 'Requires special access approval',
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
  KEY `IDX_ENCRYPTION_KEY_ID` (`ENCRYPTION_KEY_ID`),
  KEY `IDX_SENSITIVITY_LEVEL` (`SENSITIVITY_LEVEL`),
  KEY `IDX_REQUIRES_SPECIAL_ACCESS` (`REQUIRES_SPECIAL_ACCESS`),
  KEY `IDX_STATUS` (`STATUS`),
  KEY `IDX_CREATED_AT` (`CREATED_AT`),
  KEY `IDX_PAT_PHI_PATIENT_ID` (`PATIENT_ID`),
  CONSTRAINT `FK_PHI_ENCRYPTION_KEY` FOREIGN KEY (`ENCRYPTION_KEY_ID`) REFERENCES `sec_encryption_keys` (`ID`),
  CONSTRAINT `FK_PHI_PATIENT` FOREIGN KEY (`PATIENT_ID`) REFERENCES `pat_patients` (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Encrypted PHI Data';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pat_phi_data`
--

LOCK TABLES `pat_phi_data` WRITE;
/*!40000 ALTER TABLE `pat_phi_data` DISABLE KEYS */;
/*!40000 ALTER TABLE `pat_phi_data` ENABLE KEYS */;
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

-- Dump completed on 2026-05-22 22:21:06
