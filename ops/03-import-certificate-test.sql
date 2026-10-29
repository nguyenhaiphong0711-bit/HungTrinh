:ON ERROR EXIT
-- Run ONLY on an isolated test SQL Server instance that has no application certificate.
USE [master];
GO
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = '$(MasterKeyPassword)';
GO
CREATE CERTIFICATE [HungTrinhBackupCertificate]
FROM FILE = '$(CertificateDirectory)\HungTrinhBackupCertificate.cer'
WITH PRIVATE KEY (
    FILE = '$(CertificateDirectory)\HungTrinhBackupCertificate.pvk',
    DECRYPTION BY PASSWORD = '$(CertificateExportPassword)'
);
GO
