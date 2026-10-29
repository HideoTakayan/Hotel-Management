USE `hotel-management`;

-- 1. BẢNG TÀI KHOẢN NGƯỜNG DÙNG (Users)
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Username` VARCHAR(50) NOT NULL UNIQUE,
    `Password` VARCHAR(255) NOT NULL,
    `FullName` VARCHAR(50) NOT NULL,
    `Email` VARCHAR(100) NOT NULL,
    `Phone` VARCHAR(12),
    `Role` INT NOT NULL -- 0: Quản lý (Admin), 1: Nhân viên
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. BẢNG DANH MỤC PHÒNG (Rooms)
CREATE TABLE IF NOT EXISTS `Rooms` (
    `RoomId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoomNumber` VARCHAR(20) NOT NULL UNIQUE,
    `NumberPeople` INT NOT NULL,
    `NumberBed` INT NOT NULL,
    `Quality` INT NOT NULL, -- 1 đến 5 sao
    `BedType` VARCHAR(50) NOT NULL, -- 'Giường đơn', 'Giường đôi', 'Nệm Futon (Tatami)'
    `Price` DECIMAL(18,2) NOT NULL,
    `RoomType` VARCHAR(50) NOT NULL, -- 'Phòng đơn', 'Phòng đôi', 'Phòng gia đình'
    `RoomArea` FLOAT NOT NULL,
    `Status` INT NOT NULL DEFAULT 0 -- 0: Trống, 1: Đã đặt, 2: Đang sử dụng, 3: Chờ dọn dẹp, 4: Đang dọn dẹp
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. BẢNG HÓA ĐƠN & ĐẶT PHÒNG (Bills)
CREATE TABLE IF NOT EXISTS `Bills` (
    `BillId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoomId` INT NOT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `FullName` VARCHAR(50) NOT NULL,
    `Email` VARCHAR(100),
    `Phone` VARCHAR(12) NOT NULL,
    `StartDate` DATETIME NOT NULL,
    `EndDate` DATETIME NOT NULL,
    `NumberPeople` INT NOT NULL,
    `Total` DECIMAL(18,2),
    `Status` INT NOT NULL, -- 0: Chưa thanh toán, 1: Đã thanh toán, 2: Hủy, 3: Check-in, 4: Check-out
    FOREIGN KEY (`RoomId`) REFERENCES `Rooms`(`RoomId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. BẢNG DANH MỤC DỊCH VỤ SUỐI KHOÁNG NÓNG (Services)
CREATE TABLE IF NOT EXISTS `Services` (
    `ServiceId` INT AUTO_INCREMENT PRIMARY KEY,
    `ServiceName` VARCHAR(100) NOT NULL,
    `Price` DECIMAL(18,2) NOT NULL,
    `Unit` VARCHAR(20) NOT NULL 
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. BẢNG CHI TIẾT GỌI DỊCH VỤ CỦA HÓA ĐƠN (Bill_Services)
CREATE TABLE IF NOT EXISTS `Bill_Services` (
    `BillServiceId` INT AUTO_INCREMENT PRIMARY KEY,
    `BillId` INT NOT NULL,
    `ServiceId` INT NOT NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    `Total` DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (`BillId`) REFERENCES `Bills`(`BillId`) ON DELETE CASCADE,
    FOREIGN KEY (`ServiceId`) REFERENCES `Services`(`ServiceId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. BẢNG PHÂN CA LÀM VIỆC (Shifts)
CREATE TABLE IF NOT EXISTS `Shifts` (
    `ShiftId` INT AUTO_INCREMENT PRIMARY KEY,
    `WorkDate` DATE NOT NULL,
    `ShiftType` INT NOT NULL, -- 0: Ca Sáng (07:00 - 15:00), 1: Ca Tối (15:00 - 23:00)
    `Employee1_Id` INT NOT NULL,
    `Employee2_Id` INT NOT NULL,
    FOREIGN KEY (`Employee1_Id`) REFERENCES `Users`(`Id`),
    FOREIGN KEY (`Employee2_Id`) REFERENCES `Users`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. BẢNG NHIỆM VỤ BUỒNG PHÒNG & PHỤC VỤ (Tasks)
CREATE TABLE IF NOT EXISTS `Tasks` (
    `TaskId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoomId` INT NOT NULL,
    `AssignedTo` INT,
    `ShiftId` INT NOT NULL,
    `TaskName` VARCHAR(255) NOT NULL DEFAULT 'Dịch vụ/Dọn phòng',
    `TaskStatus` INT NOT NULL DEFAULT 0, -- 0: Chờ nhận việc, 1: Đang thực hiện, 2: Chờ xác nhận, 3: Hoàn thành, 4: Yêu cầu làm lại
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `CompletedAt` DATETIME,
    FOREIGN KEY (`RoomId`) REFERENCES `Rooms`(`RoomId`) ON DELETE CASCADE,
    FOREIGN KEY (`AssignedTo`) REFERENCES `Users`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`ShiftId`) REFERENCES `Shifts`(`ShiftId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================================
-- DỮ LIỆU KHỞI TẠO MẪU CHO KHÁCH SẠN SUỐI KHOÁNG NÓNG (KINKAKU-JI ONSEN & RYOKAN)
-- =========================================================================

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE `Bill_Services`;
TRUNCATE TABLE `Tasks`;
TRUNCATE TABLE `Shifts`;
TRUNCATE TABLE `Bills`;
TRUNCATE TABLE `Services`;
TRUNCATE TABLE `Rooms`;
TRUNCATE TABLE `Users`;
SET FOREIGN_KEY_CHECKS = 1;

-- Tạo tài khoản (Mật khẩu mặc định: 123456)
INSERT INTO `Users` (`Id`, `Username`, `Password`, `FullName`, `Email`, `Phone`, `Role`) VALUES
(1, 'admin', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Quản Trị Viên', 'admin@kinkakuji.com', '0901234567', 0),
(2, 'nhanvien1', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Nguyễn Văn An', 'an.nguyen@kinkakuji.com', '0912345678', 1),
(3, 'nhanvien2', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Trần Thị Bình', 'binh.tran@kinkakuji.com', '0923456789', 1),
(4, 'nhanvien3', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Lê Hoàng Long', 'long.le@kinkakuji.com', '0934567890', 1);

-- Tạo 10 Phòng chia 2 tầng đồng nhất (Tầng 1: 101-105 | Tầng 2: 201-205)
INSERT INTO `Rooms` (`RoomId`, `RoomNumber`, `NumberPeople`, `NumberBed`, `Quality`, `BedType`, `Price`, `RoomType`, `RoomArea`, `Status`) VALUES
-- TẦNG 1: PHÒNG 101 - 105
(1, '101', 1, 1, 3, 'Giường đơn', 1500000.00, 'Phòng đơn', 25.0, 0),
(2, '102', 2, 1, 4, 'Nệm Futon (Tatami)', 1800000.00, 'Phòng đơn', 28.0, 0),
(3, '103', 2, 1, 4, 'Giường đôi', 2400000.00, 'Phòng đôi', 36.0, 1),
(4, '104', 2, 2, 4, 'Nệm Futon (Tatami)', 2800000.00, 'Phòng đôi', 40.0, 2),
(5, '105', 4, 2, 5, 'Nệm Futon (Tatami)', 4200000.00, 'Phòng gia đình', 55.0, 3),

-- TẦNG 2: PHÒNG 201 - 205 (Phân chia cấu trúc giống hệt tầng 1)
(6, '201', 1, 1, 3, 'Giường đơn', 1500000.00, 'Phòng đơn', 25.0, 0),
(7, '202', 2, 1, 4, 'Nệm Futon (Tatami)', 1800000.00, 'Phòng đơn', 28.0, 0),
(8, '203', 2, 1, 4, 'Giường đôi', 2400000.00, 'Phòng đôi', 36.0, 0),
(9, '204', 2, 2, 4, 'Nệm Futon (Tatami)', 2800000.00, 'Phòng đôi', 40.0, 1),
(10, '205', 4, 2, 5, 'Nệm Futon (Tatami)', 4200000.00, 'Phòng gia đình', 55.0, 4);

-- Tạo 14 dịch vụ suối khoáng nóng Onsen & Ryokan thực tế
INSERT INTO `Services` (`ServiceId`, `ServiceName`, `Price`, `Unit`) VALUES
(1, 'Vé tắm khoáng nóng Public Onsen (Bể Kakaroyu, Bể Chum, Jacuzzi)', 350000.00, 'Lượt'),
(2, 'Thuê phòng tắm khoáng riêng tư (Private Kashikiri Hinoki Onsen)', 750000.00, 'Giờ'),
(3, 'Xông hơi trị liệu Ganbanyoku (Đá muối Himalaya & Tuyết lạnh)', 250000.00, 'Lượt'),
(4, 'Ngâm bùn khoáng nóng dưỡng da & Xông thảo dược ngải cứu', 230000.00, 'Lượt'),
(5, 'Trị liệu bấm huyệt Shiatsu Nhật Bản chuyên sâu (60 phút)', 550000.00, 'Lượt'),
(6, 'Thực đơn tối thượng hạng Kaiseki Ryori truyền thống (9 món)', 850000.00, 'Suất'),
(7, 'Buffet Ẩm thực Nhật Bản (Sushi, Sashimi, Tempura & Lẩu Shabu)', 550000.00, 'Suất'),
(8, 'Set bữa sáng Ryokan truyền thống (Cá hồi nướng, Súp Miso)', 220000.00, 'Suất'),
(9, 'Trứng trường thọ luộc suối khoáng nóng (Onsen Tamago)', 65000.00, 'Phần'),
(10, 'Trà đạo Uji Matcha thượng hạng & Bánh ngọt Wagashi theo mùa', 120000.00, 'Phần'),
(11, 'Rượu Sake Junmai Daiginjo (Hâm nóng Atsukan / Ướp lạnh)', 380000.00, 'Bình'),
(12, 'Thuê trang phục Yukata cao cấp kèm đai Obi & Guốc gỗ Geta', 150000.00, 'Bộ'),
(13, 'Ngâm chân khoáng nóng thảo dược (Ashiyu) & Massage bàn chân', 150000.00, 'Lượt'),
(14, 'Dịch vụ giặt hấp & Là hơi trang phục cao cấp', 80000.00, 'Kg');

-- Tạo lịch trực
INSERT INTO `Shifts` (`ShiftId`, `WorkDate`, `ShiftType`, `Employee1_Id`, `Employee2_Id`) VALUES
(1, CURDATE(), 0, 2, 3),
(2, CURDATE(), 1, 3, 4),
(3, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 0, 2, 4),
(4, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 1, 3, 4);

-- Tạo hóa đơn mẫu
INSERT INTO `Bills` (`BillId`, `RoomId`, `CreatedAt`, `FullName`, `Email`, `Phone`, `StartDate`, `EndDate`, `NumberPeople`, `Total`, `Status`) VALUES
(1, 3, NOW(), 'Phạm Minh Tuấn', 'tuan.pham@gmail.com', '0987654321', NOW(), DATE_ADD(NOW(), INTERVAL 2 DAY), 2, 4800000.00, 0),
(2, 4, DATE_SUB(NOW(), INTERVAL 1 DAY), 'Hoàng Thị Mai', 'mai.hoang@gmail.com', '0971234567', DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_ADD(NOW(), INTERVAL 1 DAY), 2, 8370000.00, 3),
(3, 5, DATE_SUB(NOW(), INTERVAL 2 DAY), 'Trần Đức Nam', 'nam.tran@yahoo.com', '0969888999', DATE_SUB(NOW(), INTERVAL 2 DAY), NOW(), 4, 12100000.00, 4),
(4, 9, NOW(), 'Đỗ Quang Hải', 'hai.do@gmail.com', '0911223344', NOW(), DATE_ADD(NOW(), INTERVAL 1 DAY), 2, 2800000.00, 0),
(5, 2, DATE_SUB(NOW(), INTERVAL 3 DAY), 'Vũ Thu Hằng', 'hang.vu@gmail.com', '0933445566', DATE_SUB(NOW(), INTERVAL 5 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY), 2, 5580000.00, 1),
(6, 10, DATE_SUB(NOW(), INTERVAL 1 DAY), 'Nguyễn Minh Châu', 'chau.nguyen@gmail.com', '0944556677', DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY), 4, 13800000.00, 1);

-- Chi tiết dịch vụ trong hóa đơn
INSERT INTO `Bill_Services` (`BillServiceId`, `BillId`, `ServiceId`, `Quantity`, `Total`) VALUES
(1, 2, 1, 2, 700000.00),
(2, 2, 6, 2, 1700000.00),
(3, 2, 10, 2, 240000.00),
(4, 2, 9, 2, 130000.00),
(5, 3, 2, 2, 1500000.00),
(6, 3, 7, 4, 2200000.00),
(7, 5, 3, 2, 500000.00),
(8, 5, 5, 2, 1100000.00),
(9, 5, 11, 1, 380000.00),
(10, 6, 6, 4, 3400000.00),
(11, 6, 1, 4, 1400000.00),
(12, 6, 12, 4, 600000.00);

-- Tạo danh sách nhiệm vụ
INSERT INTO `Tasks` (`TaskId`, `RoomId`, `AssignedTo`, `ShiftId`, `TaskName`, `TaskStatus`, `CreatedAt`, `CompletedAt`) VALUES
(1, 5, NULL, 1, 'Dọn phòng 105, thay chiếu Tatami & khử khuẩn bồn gỗ Hinoki sau Check-out', 0, NOW(), NULL),
(2, 10, 2, 1, 'Vệ sinh phòng 205, trải nệm Futon & chuẩn bị bộ trà Matcha chào khách', 1, NOW(), NULL),
(3, 4, 3, 1, 'Giao dịch vụ: 2x Trà đạo Uji Matcha & Wagashi, 2x Trứng Onsen Tamago (Phòng 104)', 2, DATE_SUB(NOW(), INTERVAL 30 MINUTE), NULL);
