USE `hotel-management`;

-- 1. XÓA DỮ LIỆU CŨ THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI (NẾU CÓ)
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE `Bill_Services`;
TRUNCATE TABLE `Tasks`;
TRUNCATE TABLE `Shifts`;
TRUNCATE TABLE `Bills`;
TRUNCATE TABLE `Services`;
TRUNCATE TABLE `Rooms`;
TRUNCATE TABLE `Users`;
SET FOREIGN_KEY_CHECKS = 1;

-- 2. TẠO TÀI KHOẢN NGƯỜI DÙNG (Mật khẩu mặc định: 123456)
-- Hash BCrypt cho '123456': $2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq
INSERT INTO `Users` (`Id`, `Username`, `Password`, `FullName`, `Email`, `Phone`, `Role`) VALUES
(1, 'admin', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Quản Trị Viên', 'admin@kinkakuji.com', '0901234567', 0),
(2, 'nhanvien1', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Nguyễn Văn An', 'an.nguyen@kinkakuji.com', '0912345678', 1),
(3, 'nhanvien2', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Trần Thị Bình', 'binh.tran@kinkakuji.com', '0923456789', 1),
(4, 'nhanvien3', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Lê Hoàng Long', 'long.le@kinkakuji.com', '0934567890', 1);

-- 3. TẠO DANH SÁCH 10 PHÒNG CHIA 2 TẦNG ĐỒNG NHẤT (TẦNG 1: 101-105 | TẦNG 2: 201-205)
-- Status: 0: Trống, 1: Đã đặt, 2: Đang sử dụng, 3: Chờ dọn dẹp, 4: Đang dọn dẹp
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

-- 4. TẠO DANH MỤC 14 DỊCH VỤ SUỐI KHOÁNG NÓNG ONSEN & RYOKAN THỰC TẾ
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

-- 5. TẠO LỊCH PHÂN CA LÀM VIỆC (Hôm nay và ngày mai)
-- ShiftType: 0: Sáng (07:00 - 15:00), 1: Tối (15:00 - 23:00)
INSERT INTO `Shifts` (`ShiftId`, `WorkDate`, `ShiftType`, `Employee1_Id`, `Employee2_Id`) VALUES
(1, CURDATE(), 0, 2, 3),
(2, CURDATE(), 1, 3, 4),
(3, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 0, 2, 4),
(4, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 1, 3, 4);

-- 6. TẠO HÓA ĐƠN MẪU (Khớp chính xác 100% công thức: Số đêm * Giá phòng + Tổng dịch vụ)
-- Status: 0: Chưa thanh toán, 1: Đã thanh toán, 2: Đã hủy, 3: Check-in, 4: Check-out
INSERT INTO `Bills` (`BillId`, `RoomId`, `CreatedAt`, `FullName`, `Email`, `Phone`, `StartDate`, `EndDate`, `NumberPeople`, `Total`, `Status`) VALUES
-- Bill 1: Khách đặt phòng 103 (2 đêm * 2,400,000 = 4,800,000 VND - Trạng thái: 0 - Chưa thanh toán)
(1, 3, NOW(), 'Phạm Minh Tuấn', 'tuan.pham@gmail.com', '0987654321', NOW(), DATE_ADD(NOW(), INTERVAL 2 DAY), 2, 4800000.00, 0),

-- Bill 2: Khách đang ở phòng 104 (2 đêm * 2,800,000 = 5,600,000 + 2,770,000 dịch vụ = 8,370,000 VND - Trạng thái: 3 - Check-in)
(2, 4, DATE_SUB(NOW(), INTERVAL 1 DAY), 'Hoàng Thị Mai', 'mai.hoang@gmail.com', '0971234567', DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_ADD(NOW(), INTERVAL 1 DAY), 2, 8370000.00, 3),

-- Bill 3: Khách vừa trả phòng 105 (2 đêm * 4,200,000 = 8,400,000 + 3,700,000 dịch vụ = 12,100,000 VND - Trạng thái: 4 - Check-out)
(3, 5, DATE_SUB(NOW(), INTERVAL 2 DAY), 'Trần Đức Nam', 'nam.tran@yahoo.com', '0969888999', DATE_SUB(NOW(), INTERVAL 2 DAY), NOW(), 4, 12100000.00, 4),

-- Bill 4: Khách đặt phòng 204 (1 đêm * 2,800,000 = 2,800,000 VND - Trạng thái: 0 - Chưa thanh toán)
(4, 9, NOW(), 'Đỗ Quang Hải', 'hai.do@gmail.com', '0911223344', NOW(), DATE_ADD(NOW(), INTERVAL 1 DAY), 2, 2800000.00, 0),

-- Bill 5: Hóa đơn đã thanh toán phòng 102 (2 đêm * 1,800,000 = 3,600,000 + 1,980,000 dịch vụ = 5,580,000 VND - Trạng thái: 1 - Đã thanh toán)
(5, 2, DATE_SUB(NOW(), INTERVAL 3 DAY), 'Vũ Thu Hằng', 'hang.vu@gmail.com', '0933445566', DATE_SUB(NOW(), INTERVAL 5 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY), 2, 5580000.00, 1),

-- Bill 6: Hóa đơn đã thanh toán phòng 205 (2 đêm * 4,200,000 = 8,400,000 + 5,400,000 dịch vụ = 13,800,000 VND - Trạng thái: 1 - Đã thanh toán)
(6, 10, DATE_SUB(NOW(), INTERVAL 1 DAY), 'Nguyễn Minh Châu', 'chau.nguyen@gmail.com', '0944556677', DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY), 4, 13800000.00, 1);

-- 7. CHI TIẾT DỊCH VỤ TRONG HÓA ĐƠN (Bill_Services)
INSERT INTO `Bill_Services` (`BillServiceId`, `BillId`, `ServiceId`, `Quantity`, `Total`) VALUES
-- Dịch vụ của Bill 2 (Phòng 104 - Đang Check-in): Tổng 2,770,000 VND
(1, 2, 1, 2, 700000.00),   -- 2 lượt Vé tắm khoáng nóng Public Onsen
(2, 2, 6, 2, 1700000.00),  -- 2 suất Thực đơn tối thượng hạng Kaiseki Ryori
(3, 2, 10, 2, 240000.00),  -- 2 phần Trà đạo Uji Matcha & Bánh Wagashi
(4, 2, 9, 2, 130000.00),   -- 2 phần Trứng trường thọ Onsen Tamago

-- Dịch vụ của Bill 3 (Phòng 105 - Đã Check-out): Tổng 3,700,000 VND
(5, 3, 2, 2, 1500000.00),  -- 2 giờ Thuê phòng tắm khoáng riêng tư Kashikiri Hinoki
(6, 3, 7, 4, 2200000.00),  -- 4 suất Buffet Ẩm thực Nhật Bản

-- Dịch vụ của Bill 5 (Phòng 102 - Đã thanh toán): Tổng 1,980,000 VND
(7, 5, 3, 2, 500000.00),   -- 2 lượt Xông hơi trị liệu Ganbanyoku
(8, 5, 5, 2, 1100000.00),  -- 2 lượt Trị liệu bấm huyệt Shiatsu Nhật Bản
(9, 5, 11, 1, 380000.00),  -- 1 bình Rượu Sake Junmai Daiginjo

-- Dịch vụ của Bill 6 (Phòng 205 - Đã thanh toán): Tổng 5,400,000 VND
(10, 6, 6, 4, 3400000.00), -- 4 suất Thực đơn tối thượng hạng Kaiseki Ryori
(11, 6, 1, 4, 1400000.00), -- 4 lượt Vé tắm khoáng nóng Public Onsen
(12, 6, 12, 4, 600000.00); -- 4 bộ Thuê trang phục Yukata cao cấp

-- 8. TẠO DANH SÁCH NHIỆM VỤ (Tasks)
-- TaskStatus: 0: Chờ nhận việc, 1: Đang thực hiện, 2: Chờ xác nhận, 3: Hoàn thành, 4: Yêu cầu làm lại
INSERT INTO `Tasks` (`TaskId`, `RoomId`, `AssignedTo`, `ShiftId`, `TaskName`, `TaskStatus`, `CreatedAt`, `CompletedAt`) VALUES
-- Phòng 105 vừa check-out: Chờ nhân viên nhận việc dọn phòng
(1, 5, NULL, 1, 'Dọn phòng 105, thay chiếu Tatami & khử khuẩn bồn gỗ Hinoki sau Check-out', 0, NOW(), NULL),

-- Phòng 205: Nhân viên đang tiến hành dọn dẹp
(2, 10, 2, 1, 'Vệ sinh phòng 205, trải nệm Futon & chuẩn bị bộ trà Matcha chào khách', 1, NOW(), NULL),

-- Phòng 104: Đã phục vụ Trà đạo & Trứng Onsen, chờ quản lý xác nhận
(3, 4, 3, 1, 'Giao dịch vụ: 2x Trà đạo Uji Matcha & Wagashi, 2x Trứng Onsen Tamago (Phòng 104)', 2, DATE_SUB(NOW(), INTERVAL 30 MINUTE), NULL);
