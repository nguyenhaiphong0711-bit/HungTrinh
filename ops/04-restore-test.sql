:ON ERROR EXIT
-- This file never restores over HungTrinh. Discover logical names with FILELISTONLY first.
USE [master];
GO
IF DB_ID(N'HungTrinh_RestoreTest') IS NOT NULL
    THROW 51000, 'Test database already exists. DBA must inspect it; no overwrite is allowed.', 1;
GO
RESTORE DATABASE [HungTrinh_RestoreTest]
FROM DISK = '$(BackupFile)'
WITH MOVE '$(LogicalDataName)' TO '$(TestDataDirectory)\HungTrinh_RestoreTest.mdf',
     MOVE '$(LogicalLogName)' TO '$(TestDataDirectory)\HungTrinh_RestoreTest_log.ldf',
     CHECKSUM, RECOVERY, STATS = 10;
GO
DBCC CHECKDB ([HungTrinh_RestoreTest]) WITH NO_INFOMSGS;
GO
USE [HungTrinh_RestoreTest];
GO
-- Verify representative records against counts/known IDs recorded immediately before backup.
SELECT N'AspNetUsers' AS TableName, COUNT_BIG(*) AS RowCount FROM dbo.AspNetUsers
UNION ALL SELECT N'AspNetRoles', COUNT_BIG(*) FROM dbo.AspNetRoles
UNION ALL SELECT N'Products', COUNT_BIG(*) FROM dbo.Products
UNION ALL SELECT N'Categories', COUNT_BIG(*) FROM dbo.Categories;
GO
