IF DB_ID(N'QL_ChiTieu') IS NULL CREATE DATABASE QL_ChiTieu;
GO
USE QL_ChiTieu;
GO
IF OBJECT_ID(N'dbo.ChiTieu','U') IS NOT NULL DROP TABLE dbo.ChiTieu;
CREATE TABLE dbo.ChiTieu(
  MaCT    int IDENTITY(1,1) PRIMARY KEY,
  NgayChi date           NOT NULL,
  NoiDung nvarchar(100)  NOT NULL,
  LoaiChi nvarchar(30)   NOT NULL,
  SoTien  decimal(18,0)  NOT NULL,
  GhiChu  nvarchar(200)  NULL
);
GO
DECLARE @t date = CAST(GETDATE() AS date);
INSERT dbo.ChiTieu(NgayChi,NoiDung,LoaiChi,SoTien,GhiChu) VALUES
(DATEADD(day,-11,@t),N'Cơm trưa',N'Ăn uống',35000,NULL),
(DATEADD(day,-10,@t),N'Xăng xe',N'Đi lại',60000,NULL),
(DATEADD(day,-9,@t), N'Photo giáo trình',N'Học tập',25000,N'Môn Lập trình'),
(DATEADD(day,-8,@t), N'Xem phim',N'Giải trí',90000,N'Đi với bạn'),
(DATEADD(day,-7,@t), N'Trà sữa',N'Ăn uống',40000,NULL),
(DATEADD(day,-6,@t), N'Mua sắm vặt',N'Khác',150000,NULL),
(DATEADD(day,-5,@t), N'Vé xe buýt tháng',N'Đi lại',120000,NULL),
(DATEADD(day,-4,@t), N'Ăn sáng',N'Ăn uống',25000,NULL),
(DATEADD(day,-3,@t), N'Mua sách tham khảo',N'Học tập',180000,NULL),
(DATEADD(day,-2,@t), N'Game online',N'Giải trí',50000,NULL),
(DATEADD(day,-1,@t), N'Cơm tối',N'Ăn uống',45000,NULL),
(@t,                 N'Cà phê với bạn',N'Ăn uống',45000,NULL);
GO
