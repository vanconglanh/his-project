/* =============================================================================
   ROLLBACK đăng ký client SSO STAGING/DEV
   DB: DiaB_Identity_Server (dev-db-mssql)
   Xóa ĐÚNG các client mới theo ClientId + bản ghi con. KHÔNG đụng client cũ.
   Không cần restart IS.
   ============================================================================= */
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @ids TABLE (Id INT);
INSERT INTO @ids (Id)
  SELECT Id FROM Clients
  WHERE ClientId IN (N'prodiab-his-staging', N'prodiab-his-dev', N'diab-timesheet-staging');

DELETE FROM ClientSecrets                WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientGrantTypes             WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientScopes                 WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientRedirectUris           WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientPostLogoutRedirectUris WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientCorsOrigins            WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientClaims                 WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientProperties             WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM ClientIdPRestrictions        WHERE ClientId IN (SELECT Id FROM @ids);
DELETE FROM Clients                      WHERE Id       IN (SELECT Id FROM @ids);

COMMIT TRANSACTION;
PRINT 'Da rollback client SSO staging/dev.';
