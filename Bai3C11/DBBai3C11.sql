IF DB_ID(N'QL_DaNgoai') IS NULL CREATE DATABASE QL_DaNgoai;
GO
USE QL_DaNgoai;
GO
IF OBJECT_ID(N'dbo.ChuyenDi','U') IS NOT NULL DROP TABLE dbo.ChuyenDi;
CREATE TABLE dbo.ChuyenDi(
  MaChuyen    int IDENTITY(1,1) PRIMARY KEY,
  TenChuyen   nvarchar(100) NOT NULL,
  DiaDiem     nvarchar(100) NOT NULL,
  NgayDi      date          NOT NULL,
  SoNguoi     int           NOT NULL,
  NganSach    decimal(18,0) NOT NULL,
  PhuongTien  nvarchar(30)  NOT NULL,
  DaHoanThanh bit           NOT NULL DEFAULT 0,
  GhiChu      nvarchar(200) NULL
);
GO
DECLARE @t date = CAST(GETDATE() AS date);
INSERT dbo.ChuyenDi(TenChuyen,DiaDiem,NgayDi,SoNguoi,NganSach,PhuongTien,DaHoanThanh,GhiChu) VALUES
(N'Cắm trại đồi chè',N'Đà Lạt',DATEADD(day,3,@t),6,3500000,N'Ô tô',0,NULL),
(N'Săn mây Sa Pa',N'Sa Pa',DATEADD(day,5,@t),4,5000000,N'Xe khách',0,NULL),
(N'Tắm biển cuối tuần',N'Vũng Tàu',DATEADD(day,7,@t),8,2500000,N'Ô tô',0,NULL),
(N'Phố cổ Hội An',N'Hội An',DATEADD(day,9,@t),3,6000000,N'Tàu hỏa',0,N'Đặt vé sớm'),
(N'Biển Nha Trang',N'Nha Trang',DATEADD(day,11,@t),5,7000000,N'Xe khách',0,NULL),
(N'Phượt Hà Giang',N'Hà Giang',DATEADD(day,14,@t),4,4500000,N'Xe máy',0,NULL),
(N'Đảo Phú Quốc',N'Phú Quốc',DATEADD(day,18,@t),6,9000000,N'Ô tô',0,NULL),
(N'Cố đô Huế',N'Huế',DATEADD(day,21,@t),2,3000000,N'Tàu hỏa',1,N'Đã đi thử tuyến');
GO
