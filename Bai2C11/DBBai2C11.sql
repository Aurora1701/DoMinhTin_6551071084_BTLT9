IF DB_ID(N'QL_NhaTro') IS NULL CREATE DATABASE QL_NhaTro;
GO
USE QL_NhaTro;
GO
IF OBJECT_ID(N'dbo.KhachThue','U') IS NOT NULL DROP TABLE dbo.KhachThue;
IF OBJECT_ID(N'dbo.PhongTro','U')  IS NOT NULL DROP TABLE dbo.PhongTro;
CREATE TABLE dbo.PhongTro(
  MaPhong   int IDENTITY(1,1) PRIMARY KEY,
  TenPhong  nvarchar(20)  NOT NULL,
  GiaThue   decimal(18,0) NOT NULL,
  TinhTrang nvarchar(20)  NOT NULL DEFAULT N'Trống'
);
CREATE TABLE dbo.KhachThue(
  MaKhach        int IDENTITY(1,1) PRIMARY KEY,
  HoTen          nvarchar(100) NOT NULL,
  GioiTinh       nvarchar(5)   NOT NULL,
  NgaySinh       date          NOT NULL,
  SoDienThoai    varchar(15)   NOT NULL,
  MaTinh         int           NOT NULL DEFAULT 0,
  TenTinh        nvarchar(100) NOT NULL,
  MaPhuongXa     int           NOT NULL DEFAULT 0,
  TenPhuongXa    nvarchar(100) NOT NULL,
  DaDangKyTamTru bit           NOT NULL DEFAULT 0,
  MaPhong        int           NOT NULL,
  CONSTRAINT FK_KhachThue_PhongTro FOREIGN KEY(MaPhong) REFERENCES dbo.PhongTro(MaPhong)
);
GO
INSERT dbo.PhongTro(TenPhong,GiaThue,TinhTrang) VALUES
(N'P01',1500000,N'Đang thuê'),(N'P02',1600000,N'Đang thuê'),(N'P03',1800000,N'Đang thuê'),
(N'P04',2000000,N'Đang thuê'),(N'P05',1700000,N'Đang thuê'),(N'P06',1900000,N'Trống');
INSERT dbo.KhachThue(HoTen,GioiTinh,NgaySinh,SoDienThoai,MaTinh,TenTinh,MaPhuongXa,TenPhuongXa,DaDangKyTamTru,MaPhong) VALUES
(N'Nguyễn Văn An',N'Nam','2004-05-12','0912345678',1, N'Thành phố Hà Nội',0,N'Phường Ba Đình',1,3),
(N'Nguyễn Văn Bảo',N'Nam','2003-03-20','0912345679',1,N'Thành phố Hà Nội',0,N'Phường Ngọc Hà',0,3),
(N'Trần Thị Bình',N'Nữ','2002-11-02','0876543210',37,N'Tỉnh Ninh Bình',0,N'Phường Nam Định',0,1),
(N'Lê Văn Cường',N'Nam','2001-07-15','0901234567',38,N'Tỉnh Thanh Hóa',0,N'Phường Hạc Thành',0,2),
(N'Phạm Minh Đức',N'Nam','2004-01-09','0987654321',38,N'Tỉnh Thanh Hóa',0,N'Phường Hạc Thành',1,4),
(N'Hoàng Thị Dung',N'Nữ','2003-09-30','0765432109',27,N'Tỉnh Bắc Ninh',0,N'Phường Kinh Bắc',0,5);
GO
