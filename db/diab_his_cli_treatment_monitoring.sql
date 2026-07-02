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

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '0cde9779-8b67-11ef-b09a-0242ac130002:1-22227761';

--
-- Table structure for table `cli_treatment_monitoring`
--

DROP TABLE IF EXISTS `cli_treatment_monitoring`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cli_treatment_monitoring` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `CODE` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Treatment monitoring code',
  `PATIENT_ID` int NOT NULL COMMENT 'Reference to pat_patients',
  `VISIT_ID` int DEFAULT NULL COMMENT 'Reference to cli_visits',
  `EMR_HEADER_ID` int DEFAULT NULL COMMENT 'Reference to cli_emr_headers (if linked to EMR)',
  `MONITORING_DATE` datetime NOT NULL COMMENT 'Date of monitoring entry',
  `DISEASE_PROGRESSION` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Disease progression description',
  `MEDICATION_AND_TREATMENT` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Medication and treatment method description',
  `NOTES` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT 'Additional notes',
  `CREATED_BY` int DEFAULT NULL COMMENT 'Created by user',
  `LAST_UPDATED_BY` int DEFAULT NULL COMMENT 'Last updated by user',
  `CREATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `LAST_UPDATED_AT` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `STATUS` int NOT NULL DEFAULT '1' COMMENT 'Status: 1=ACTIVE, 0=DELETED',
  `STATUS_FLAG` int NOT NULL DEFAULT '1' COMMENT 'Status flag',
  `LAST_UPDATED_PROGRAM` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UK_CODE` (`CODE`),
  KEY `IDX_PATIENT_ID` (`PATIENT_ID`),
  KEY `IDX_VISIT_ID` (`VISIT_ID`),
  KEY `IDX_EMR_HEADER_ID` (`EMR_HEADER_ID`),
  KEY `IDX_MONITORING_DATE` (`MONITORING_DATE`),
  KEY `IDX_STATUS` (`STATUS`),
  KEY `IDX_STATUS_FLAG` (`STATUS_FLAG`),
  KEY `IDX_CREATED_AT` (`CREATED_AT`),
  KEY `FK_TREATMENT_MONITORING_CREATED_BY` (`CREATED_BY`),
  KEY `FK_TREATMENT_MONITORING_UPDATED_BY` (`LAST_UPDATED_BY`),
  CONSTRAINT `FK_TREATMENT_MONITORING_CREATED_BY` FOREIGN KEY (`CREATED_BY`) REFERENCES `sec_users` (`ID`),
  CONSTRAINT `FK_TREATMENT_MONITORING_EMR_HEADER` FOREIGN KEY (`EMR_HEADER_ID`) REFERENCES `cli_emr_headers` (`ID`),
  CONSTRAINT `FK_TREATMENT_MONITORING_PATIENT` FOREIGN KEY (`PATIENT_ID`) REFERENCES `pat_patients` (`ID`),
  CONSTRAINT `FK_TREATMENT_MONITORING_UPDATED_BY` FOREIGN KEY (`LAST_UPDATED_BY`) REFERENCES `sec_users` (`ID`),
  CONSTRAINT `FK_TREATMENT_MONITORING_VISIT` FOREIGN KEY (`VISIT_ID`) REFERENCES `cli_visits` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Treatment Monitoring / Progress Notes';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cli_treatment_monitoring`
--

LOCK TABLES `cli_treatment_monitoring` WRITE;
/*!40000 ALTER TABLE `cli_treatment_monitoring` DISABLE KEYS */;
INSERT INTO `cli_treatment_monitoring` VALUES (1,'TREAT2026013004572501526C37C',105,113,NULL,'2025-12-16 16:55:00','- BỆNH NHÂN ĐAU NHỨC XƯƠNG KHỚP CẦN Ở LẠI PHÒNG KHÁM ĐỂ THEO DÕI 5 NGÀY\n- BỆNH NHÂN KHÔNG CÓ BHYT','Phương pháp điều trị: - CHÂM CỨU\n- MASSAGE\n- TRUYỀN NƯỚC\n- UỐNG THUỐC \n\nThuốc sử dụng:\nFEMARA 2.5 (TS) (Letrozole 2.5mg) (2.5mg) - 1-1-1-1 x 5 ngày\nDDVS MUỐI BIỂN XANH SHEGAN () - 0-1-1-0 x 5 ngày\nAVALO DAY (0.03 mg) - 1-0-0-1 x 5 ngày','- BỆNH NHÂN ĐIỀU TRỊ NGÀY ĐẦU LÀ NGÀY 16/12\n- CÒN LẠI 4 NGÀY ĐIỀU TRỊ',1,1,'2026-01-30 04:57:25','2026-01-30 11:35:23',1,1,'Web.Application'),(2,'TREAT202601300457250364BC3D5',105,113,NULL,'2025-12-16 17:01:00','- BỆNH NHÂN ĐAU NHỨC XƯƠNG KHỚP CẦN Ở LẠI PHÒNG KHÁM ĐỂ THEO DÕI 5 NGÀY\n- BỆNH NHÂN KHÔNG CÓ BHYT\n- HÔM NAY LÀ NGÀY 17/12','Phương pháp điều trị: - CHÂM CỨU\n- MASSAGE\n- TRUYỀN NƯỚC\n- UỐNG THUỐC \n\nThuốc sử dụng:\nFEMARA 2.5 (TS) (Letrozole 2.5mg) (2.5mg) - 1-1-1-1 x 5 ngày\nDDVS MUỐI BIỂN XANH SHEGAN () - 0-1-1-0 x 5 ngày\nAVALO DAY (0.03 mg) - 1-0-0-1 x 5 ngày','- BỆNH NHÂN ĐIỀU TRỊ NGÀY ĐẦU LÀ NGÀY 16/12\n- HÔM NAY LÀ THỨ 2 NGÀY 17/12\n- CÒN LẠI 3 NGÀY ĐIỀU TRỊ',1,1,'2026-01-30 04:57:25','2026-01-30 11:35:23',1,1,'Web.Application'),(3,'TREAT20260130045725044167029',105,113,NULL,'2025-12-17 08:45:00','HÔM NAY LÀ NGÀY 17 RIÊNG BIỆT CỦA PHIẾU ĐIỀU TRỊ','Phương pháp điều trị: CHO 2 LOẠI THUỐC VÀ CHO ĐI CHÂM CỨU NÃO\n\nThuốc sử dụng:\nYOU CARE Cream 5% (Imiquimod 12.5mg) (12.5mg) - 1-0-1-0 x 3 ngày\nNAT B () - 1-0-1-0 x 5 ngày','NGÀY THỨ 2 ĐIỀU TRỊ',1,1,'2026-01-30 04:57:25','2026-01-30 11:35:23',1,1,'Web.Application'),(4,'TREAT202601300457250606D063D',105,113,NULL,'2025-12-18 11:21:00','THêm mới ngày 18','Phương pháp điều trị: thêm mới ngày 18 tách phiếu \n\nThuốc sử dụng:\nPROLUTON ZYDUS 500MG (250ml) - 1-1-0-0 x 1 ngày','thêm mới 2 thuốc ngày 18',1,1,'2026-01-30 04:57:25','2026-01-30 11:35:23',1,1,'Web.Application'),(5,'TREAT202601300457253873C2F23',106,114,NULL,'2025-12-17 14:38:00','TEST SHƠW DIỄN TIẾN BỆNH CHO API','Phương pháp điều trị: TEST SHOW PHƯƠNG PHÁP ĐIỀU TRỊ CHO API\n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-1-1-1 x 5 ngày\nNAT B () - 1-0-1-0 x 5 ngày','CHI CHÚ TEST CHO API',1,1,'2026-01-30 04:57:25','2026-01-30 11:35:23',1,1,'Web.Application'),(6,'TREAT202601300457253905FB59E',106,114,NULL,'2025-12-17 14:39:00','TEST PHIẾU THEO DÕI PHIẾU THỨ 3','Phương pháp điều trị: TEST PHIẾU THEO DÕI PHIẾU THỨ 3\n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-1-1-1 x 5 ngày\nNAT B () - 1-0-1-0 x 5 ngày','TEST PHIẾU THEO DÕI PHIẾU THỨ 3',1,1,'2026-01-30 04:57:25','2026-01-30 11:35:23',1,1,'Web.Application'),(7,'TREAT20260130045728706E1F8D2',106,124,NULL,'2025-12-18 09:48:00','TEST API Diễn biến bệnh:','Phương pháp điều trị: TEST API Phương pháp điều trị:\n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-1-1-1 x 5 ngày\nPROLUTON ZYDUS 500MG (250ml) - 1-1-0-0 x 1 ngày','TEST API Ghi chú:',1,1,'2026-01-30 04:57:29','2026-01-30 11:35:27',1,1,'Web.Application'),(8,'TREAT2026013004572871237FE55',106,124,NULL,'2025-12-18 09:49:00','Diễn biến bệnh: API','Phương pháp điều trị: Phương pháp điều trị:API\n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-1-1-1 x 5 ngày\nPROLUTON ZYDUS 500MG (250ml) - 1-1-0-0 x 1 ngày','Ghi chú:API',1,1,'2026-01-30 04:57:29','2026-01-30 11:35:27',1,1,'Web.Application'),(9,'TREAT202601300457311877649B5',122,134,NULL,'2025-12-19 10:25:00','- Khám nội khoa Tổng quát 2','Phương pháp điều trị: - Khám nội khoa Tổng quát 2\n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-1-1-1 x 5 ngày\nPERDAY ACTIVATED FOLATE & B-VITAMINS (200ml) - 1-0-0-0 x 5 ngày','- Khám nội khoa Tổng quát 2',1,1,'2026-01-30 04:57:31','2026-01-30 11:35:30',1,1,'Web.Application'),(10,'TREAT20260130045734383F9A248',136,148,NULL,'2026-01-05 15:11:00','NGỌC CÓ TEST DIỄN BIẾN BỆNH','Phương pháp điều trị: NGỌC CÓ TEST PHƯƠNG PHÁP ĐIỀU TRỊ VÀ TỰ ĐỘNG LOAD THUỐC\n\nThuốc sử dụng:\nFEMARA 2.5 (TS) (Letrozole 2.5mg) (2.5mg) - 1-1-1-1 x 5 ngày\nDDVS MUỐI BIỂN XANH SHEGAN () - 0-1-0-1 x 5 ngày\nFERROVIT () - 1-0-1-0 x 3 ngày\nAVALO DAY (0.03 mg) - 1-0-0-1 x 1 ngày\nNAT B () - 1-0-1-0 x 5 ngày','GHI CHÚ TESTING ĐỢT 1',1,1,'2026-01-30 04:57:34','2026-01-30 11:35:33',1,1,'Web.Application'),(11,'TREAT2026013004573438623C545',136,148,NULL,'2026-01-05 15:12:00','TESTING DIỄN BIẾN BỆNH ĐỢT 2','Phương pháp điều trị: TESTING PHƯƠNG PHÁP ĐIỀU TRỊ ĐỢT 2\n\nThuốc sử dụng:\nFEMARA 2.5 (TS) (Letrozole 2.5mg) (2.5mg) - 1-1-1-1 x 5 ngày\nDDVS MUỐI BIỂN XANH SHEGAN () - 0-1-0-1 x 5 ngày\nFERROVIT () - 1-0-1-0 x 3 ngày\nAVALO DAY (0.03 mg) - 1-0-0-1 x 1 ngày\nNAT B () - 1-0-1-0 x 5 ngày','TESTING ĐỢT 2',1,1,'2026-01-30 04:57:34','2026-01-30 11:35:33',1,1,'Web.Application'),(12,'TREAT202601300457439567A4505',176,192,NULL,'2025-12-10 16:35:00','đau bụng','Phương pháp điều trị: siêu âm\n\nThuốc sử dụng:\n','Ghi chú',1,1,'2026-01-30 04:57:44','2026-01-30 11:35:41',1,1,'Web.Application'),(13,'TREAT202601300457439596919FC',176,192,NULL,'2025-12-11 15:56:00','mệt Bệnh tả do Vibrio cholerae 01, typ sinh học cholerae(A00.0)\nBệnh tả do Vibrio cholerae 01, typ sinh học cholerae(A00.0)\nBệnh tả do Vibrio cholerae 01, typ sinh học cholerae(A00.0)','Phương pháp điều trị: uống thuốc\n\nThuốc sử dụng:\nFERROVIT () - 2-0-1-0 x 3 ngày\nAVALO DAY (0.03 mg) - 1-0-0-0 x 1 ngày','un nhieu nuoc',1,1,'2026-01-30 04:57:44','2026-01-30 11:35:41',1,1,'Web.Application'),(14,'TREAT20260130045744825A7B7E2',178,196,NULL,'2025-12-12 08:54:00','DIễn biến lần 1','Phương pháp điều trị: phương pháp lần 1\n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-0-0-0 x 1 ngày','ghi chú 1',1,1,'2026-01-30 04:57:45','2026-01-30 11:35:42',1,1,'Web.Application'),(15,'TREAT20260130045744828F1B969',178,196,NULL,'2025-12-12 09:05:00','DIễn biến lần 3','Phương pháp điều trị: \n\nThuốc sử dụng:\nAVALO DAY (0.03 mg) - 1-0-0-0 x 1 ngày','ghi chú 2',1,1,'2026-01-30 04:57:45','2026-01-30 11:35:42',1,1,'Web.Application'),(16,'TREAT202601300457250364BC3D6',180,97,NULL,'2025-12-16 17:01:00','- BỆNH NHÂN ĐAU NHỨC XƯƠNG KHỚP CẦN Ở LẠI PHÒNG KHÁM ĐỂ THEO DÕI 5 NGÀY\n- BỆNH NHÂN KHÔNG CÓ BHYT\n- HÔM NAY LÀ NGÀY 17/12','Phương pháp điều trị: - CHÂM CỨU\n- MASSAGE\n- TRUYỀN NƯỚC\n- UỐNG THUỐC \n\nThuốc sử dụng:\nFEMARA 2.5 (TS) (Letrozole 2.5mg) (2.5mg) - 1-1-1-1 x 5 ngày\nDDVS MUỐI BIỂN XANH SHEGAN () - 0-1-1-0 x 5 ngày\nAVALO DAY (0.03 mg) - 1-0-0-1 x 5 ngày','- BỆNH NHÂN ĐIỀU TRỊ NGÀY ĐẦU LÀ NGÀY 16/12\n- HÔM NAY LÀ THỨ 2 NGÀY 17/12\n- CÒN LẠI 3 NGÀY ĐIỀU TRỊ',1,1,'2026-01-30 04:57:25','2026-01-30 07:07:23',1,1,'Web.Application');
/*!40000 ALTER TABLE `cli_treatment_monitoring` ENABLE KEYS */;
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

-- Dump completed on 2026-05-22 22:19:51
