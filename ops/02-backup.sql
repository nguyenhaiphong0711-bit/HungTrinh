:ON ERROR EXIT
USE [master];
GO
-- Use a NEW destination each run. NO INIT/FORMAT: never overwrite retained media.
BACKUP DATABASE [HungTrinh]
TO DISK = '$(BackupFile)'
WITH COPY_ONLY, CHECKSUM,
    ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = [HungTrinhBackupCertificate]),
    STATS = 10;
GO
RESTORE VERIFYONLY FROM DISK = '$(BackupFile)' WITH CHECKSUM;
GO
