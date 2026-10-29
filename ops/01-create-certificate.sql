:ON ERROR EXIT
USE [master];
GO
-- Run once by a DBA. Passwords are sqlcmd scripting variables from a secure session.
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = '$(MasterKeyPassword)';
GO
IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE name = N'HungTrinhBackupCertificate')
    CREATE CERTIFICATE [HungTrinhBackupCertificate]
        WITH SUBJECT = N'HungTrinh encrypted backup', EXPIRY_DATE = '20351231';
GO
-- SQL Server service must write here; this directory MUST be separate from .bak storage.
-- Existing exports are intentionally not overwritten. Archive/version old certificates.
BACKUP CERTIFICATE [HungTrinhBackupCertificate]
TO FILE = '$(CertificateDirectory)\HungTrinhBackupCertificate.cer'
WITH PRIVATE KEY (
    FILE = '$(CertificateDirectory)\HungTrinhBackupCertificate.pvk',
    ENCRYPTION BY PASSWORD = '$(CertificateExportPassword)'
);
GO
