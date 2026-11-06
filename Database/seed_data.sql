USE `hotel-management`;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE `Bill_Services`;
TRUNCATE TABLE `Tasks`;
TRUNCATE TABLE `Shifts`;
TRUNCATE TABLE `Bills`;
TRUNCATE TABLE `Services`;
TRUNCATE TABLE `Rooms`;
TRUNCATE TABLE `Users`;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO `Users` (`Id`, `Username`, `Password`, `FullName`, `Email`, `Phone`, `Role`) VALUES
(1, 'admin', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Quản Trị Viên', 'admin@kinkakuji.com', '0901234567', 0),
(2, 'nhanvien1', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Nguyễn Văn An', 'an.nguyen@kinkakuji.com', '0912345678', 1),
(3, 'nhanvien2', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Trần Thị Bình', 'binh.tran@kinkakuji.com', '0923456789', 1),
(4, 'nhanvien3', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Lê Hoàng Long', 'long.le@kinkakuji.com', '0934567890', 1),
(5, 'nhanvien4', '$2b$12$e1F2zgOWSah4w89At5pXxulrRJEbj4zZn1ZyPDa7zcFKgIFz1FuNq', 'Trần Tiến Đức', 'duc.tran@kinkakuji.com', '0945678901', 1);

INSERT INTO `Rooms` (`RoomId`, `RoomNumber`, `NumberPeople`, `NumberBed`, `Quality`, `BedType`, `Price`, `RoomType`, `RoomArea`, `Status`) VALUES
(1, '101', 1, 1, 3, 'Giường đơn', 1500000.00, 'Phòng đơn', 25.0, 0),
(2, '102', 2, 1, 4, 'Nệm Futon (Tatami)', 1800000.00, 'Phòng đơn', 28.0, 0),
(3, '103', 2, 1, 4, 'Giường đôi', 2400000.00, 'Phòng đôi', 36.0, 1),
(4, '104', 2, 2, 4, 'Nệm Futon (Tatami)', 2800000.00, 'Phòng đôi', 40.0, 2),
(5, '105', 4, 2, 5, 'Nệm Futon (Tatami)', 4200000.00, 'Phòng gia đình', 55.0, 3),
(6, '201', 1, 1, 3, 'Giường đơn', 1500000.00, 'Phòng đơn', 25.0, 0),
(7, '202', 2, 1, 4, 'Nệm Futon (Tatami)', 1800000.00, 'Phòng đơn', 28.0, 0),
(8, '203', 2, 1, 4, 'Giường đôi', 2400000.00, 'Phòng đôi', 36.0, 0),
(9, '204', 2, 2, 4, 'Nệm Futon (Tatami)', 2800000.00, 'Phòng đôi', 40.0, 1),
(10, '205', 4, 2, 5, 'Nệm Futon (Tatami)', 4200000.00, 'Phòng gia đình', 55.0, 4);

INSERT INTO `Services` (`ServiceId`, `ServiceName`, `Price`, `Unit`) VALUES
(1, 'Vé tắm khoáng nóng Public Onsen (Bể Rotenburo & Jacuzzi)', 350000.00, 'Lượt'),
(2, 'Thuê bồn khoáng riêng tư gia đình (Private Kashikiri Hinoki)', 750000.00, 'Giờ'),
(3, 'Xông hơi trị liệu đá muối Himalaya & Tuyết lạnh (Ganbanyoku)', 250000.00, 'Lượt'),
(4, 'Ngâm bùn khoáng nóng dưỡng da & Xông thảo dược ngải cứu', 230000.00, 'Lượt'),
(5, 'Ngâm bồn gỗ Bách Mộc thảo mộc bưởi vàng Yuzu (Yuzuyu Onsen)', 300000.00, 'Lượt'),
(6, 'Ngâm chân khoáng nóng ngoài trời (Ashiyu) ngắm hoa anh đào', 120000.00, 'Lượt'),
(7, 'Trải nghiệm tắm rừng trị liệu thiên nhiên (Shinrin-yoku)', 200000.00, 'Lượt'),
(8, 'Vé tham quan vườn thiền Karesansui & Trải nghiệm thiền trà Zen', 180000.00, 'Lượt'),
(9, 'Thực đơn tối thượng hạng Kaiseki Ryori truyền thống (9 món)', 850000.00, 'Suất'),
(10, 'Set Bò Wagyu A5 thượng hạng nướng đá nham thạch tại phòng', 950000.00, 'Suất'),
(11, 'Buffet Ẩm thực Nhật Bản (Sushi, Sashimi cá hồi & Lẩu Shabu)', 550000.00, 'Suất'),
(12, 'Set bữa sáng Ryokan truyền thống (Cá hồi nướng, Súp Miso)', 220000.00, 'Suất'),
(13, 'Mì Ramen đêm truyền thống phục vụ tại phòng (Yonaki Soba)', 130000.00, 'Bát'),
(14, 'Trứng trường thọ luộc suối khoáng nóng (Onsen Tamago 4 quả)', 65000.00, 'Phần'),
(15, 'Trà đạo Uji Matcha thượng hạng & Bánh ngọt Wagashi theo mùa', 120000.00, 'Phần'),
(16, 'Rượu Sake Junmai Daiginjo ướp lạnh (Chai 720ml cao cấp)', 680000.00, 'Chai'),
(17, 'Rượu Mơ Nhật Bản Umeshu thủ công kèm đá viên tinh khiết', 320000.00, 'Bình'),
(18, 'Bia thủ công Nhật Bản Asahi Super Dry / Sapporo ướp lạnh', 55000.00, 'Lon'),
(19, 'Trị liệu bấm huyệt Shiatsu Nhật Bản chuyên sâu (60 phút)', 550000.00, 'Lượt'),
(20, 'Thuê trang phục Kimono / Yukata cao cấp (kèm guốc Geta & quạt)', 180000.00, 'Bộ'),
(21, 'Dịch vụ giặt hấp & Là hơi trang phục cao cấp lấy nhanh', 80000.00, 'Kg'),
(22, 'Thêm bộ nệm Futon & Chiếu cói Tatami dự phòng cho khách', 250000.00, 'Bộ'),
(23, 'Bóng bàn Onsen truyền thống (Onsen Takkyu - kèm vợt & bóng)', 80000.00, 'Giờ'),
(24, 'Phòng Bida lỗ giải trí (Billiards Club - Bàn & Cơ cao cấp)', 120000.00, 'Giờ'),
(25, 'Quầy bắn súng nút bấc trúng thưởng kiểu Nhật (Shateki Game)', 50000.00, 'Lượt'),
(26, 'Thuê bàn cờ Tướng Nhật Bản Shogi & Cờ Vây (kèm trà xanh)', 60000.00, 'Buổi'),
(27, 'Khu máy trò chơi điện tử Retro & Gắp thú Onsen (Xu chơi game)', 50000.00, '10 Xu'),
(28, 'Phòng hát Karaoke Ryokan cách âm cao cấp (kèm micro & dàn loa)', 250000.00, 'Giờ'),
(29, 'Trò chơi ném vòng trúng thưởng dân gian Onsen (Wanage Game)', 40000.00, 'Lượt');

INSERT INTO `Shifts` (`ShiftId`, `WorkDate`, `ShiftType`, `Employee1_Id`, `Employee2_Id`) VALUES
(1, CURDATE(), 0, 2, 3),
(2, CURDATE(), 1, 4, 5),
(3, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 0, 2, 3),
(4, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 1, 4, 5);

INSERT INTO `Bills` (`BillId`, `RoomId`, `CreatedAt`, `FullName`, `Email`, `Phone`, `StartDate`, `EndDate`, `NumberPeople`, `Total`, `Status`) VALUES
(1, 3, NOW(), 'Phạm Minh Tuấn', 'tuan.pham@gmail.com', '0987654321', NOW(), DATE_ADD(NOW(), INTERVAL 2 DAY), 2, 4800000.00, 0),
(2, 4, DATE_SUB(NOW(), INTERVAL 1 DAY), 'Hoàng Thị Mai', 'mai.hoang@gmail.com', '0971234567', DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_ADD(NOW(), INTERVAL 1 DAY), 2, 8370000.00, 3),
(3, 5, DATE_SUB(NOW(), INTERVAL 2 DAY), 'Trần Đức Nam', 'nam.tran@yahoo.com', '0969888999', DATE_SUB(NOW(), INTERVAL 2 DAY), NOW(), 4, 12100000.00, 4),
(4, 9, NOW(), 'Đỗ Quang Hải', 'hai.do@gmail.com', '0911223344', NOW(), DATE_ADD(NOW(), INTERVAL 1 DAY), 2, 2800000.00, 0),
(5, 2, DATE_SUB(NOW(), INTERVAL 3 DAY), 'Vũ Thu Hằng', 'hang.vu@gmail.com', '0933445566', DATE_SUB(NOW(), INTERVAL 5 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY), 2, 5580000.00, 1),
(6, 10, DATE_SUB(NOW(), INTERVAL 1 DAY), 'Nguyễn Minh Châu', 'chau.nguyen@gmail.com', '0944556677', DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY), 4, 13800000.00, 1);

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

INSERT INTO `Tasks` (`TaskId`, `RoomId`, `AssignedTo`, `ShiftId`, `TaskName`, `TaskStatus`, `CreatedAt`, `CompletedAt`) VALUES
(1, 5, NULL, 1, 'Dọn phòng 105, thay chiếu Tatami & khử khuẩn bồn gỗ Hinoki sau Check-out', 0, NOW(), NULL),
(2, 10, 2, 1, 'Vệ sinh phòng 205, trải nệm Futon & chuẩn bị bộ trà Matcha chào khách', 1, NOW(), NULL),
(3, 4, 3, 1, 'Giao dịch vụ: 2x Trà đạo Uji Matcha & Wagashi, 2x Trứng Onsen Tamago (Phòng 104)', 2, DATE_SUB(NOW(), INTERVAL 30 MINUTE), NULL);
