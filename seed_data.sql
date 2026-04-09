-- DỌN SẠCH DB (Bao gồm cả bảng trung gian mới)
TRUNCATE TABLE "Users", "Categories", "Products", "ProductRecommendations", "ProductImages", "ImportLogs", "ImportLogDetails", "Orders", "OrderItems" RESTART IDENTITY CASCADE;

-- 1. BƠM DANH MỤC
INSERT INTO "Categories" ("Id", "Name") VALUES 
('11111111-1111-1111-1111-111111111111', 'Nước ngọt'),
('22222222-2222-2222-2222-222222222222', 'Bia & Đồ có cồn'),
('33333333-3333-3333-3333-333333333333', 'Bánh kẹo'),
('44444444-4444-4444-4444-444444444444', 'Mì ăn liền'),
('55555555-5555-5555-5555-555555555555', 'Gia vị');

-- 2. BƠM SẢN PHẨM
INSERT INTO "Products" ("Id", "CategoryId", "SKU", "Name", "ImportPrice", "SalePrice", "StockQuantity", "AvailableStockQuantity", "MinimumStockQuantity", "IsDraft", "CreatedAt", "UpdatedAt") VALUES 
('aaaa0001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN01', 'Coca Cola Lon 320ml', 8000, 10000, 100, 100, 10, false, NOW(), NOW()),
('aaaa0002-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN02', 'Pepsi Lon 320ml', 8000, 10000, 100, 100, 10, false, NOW(), NOW()),
('aaaa0003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN03', 'Redbull Thái 250ml', 12000, 15000, 50, 50, 10, false, NOW(), NOW()),
('aaaa0004-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN04', 'Sting Dâu Chai 330ml', 8500, 12000, 80, 80, 10, false, NOW(), NOW()),
('aaaa0005-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN05', '7Up Lon 320ml', 7500, 10000, 60, 60, 10, false, NOW(), NOW()),
('aaaa0006-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA01', 'Bia Tiger Nâu Lon 330ml', 16000, 18000, 200, 200, 10, false, NOW(), NOW()),
('aaaa0007-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA02', 'Bia Tiger Bạc Lon 330ml', 18000, 21000, 150, 150, 10, false, NOW(), NOW()),
('aaaa0008-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA03', 'Bia Heineken Lon 330ml', 19000, 22000, 100, 100, 10, false, NOW(), NOW()),
('aaaa0009-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA04', 'Bia 333 Lon 330ml', 13000, 15000, 120, 120, 10, false, NOW(), NOW()),
('aaaa0010-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA05', 'Bia Sài Gòn Xanh Lon 330ml', 14000, 16000, 80, 80, 10, false, NOW(), NOW()),
('aaaa0011-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK01', 'Bánh Chocopie Hộp 12 Cái', 45000, 55000, 30, 30, 10, false, NOW(), NOW()),
('aaaa0012-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK02', 'Snack Oishi Tôm Cay', 4500, 6000, 100, 100, 10, false, NOW(), NOW()),
('aaaa0013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK03', 'Bánh Quy Danisa 454g', 120000, 145000, 20, 20, 10, false, NOW(), NOW()),
('aaaa0014-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK04', 'Kẹo Dẻo Chupa Chups', 8000, 10000, 50, 50, 10, false, NOW(), NOW()),
('aaaa0015-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK05', 'Bánh Cosy Marie 144g', 15000, 20000, 40, 40, 10, false, NOW(), NOW()),
('aaaa0016-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'MI01', 'Mì Hảo Hảo Tôm Chua Cay', 3500, 4500, 500, 500, 10, false, NOW(), NOW()),
('aaaa0017-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'MI02', 'Mì Omachi Xốt Vang', 7500, 9000, 200, 200, 10, false, NOW(), NOW()),
('aaaa0018-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'GV01', 'Nước mắm Nam Ngư 750ml', 35000, 42000, 40, 40, 10, false, NOW(), NOW()),
('aaaa0019-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'GV02', 'Dầu ăn Neptune 1 Lít', 45000, 52000, 30, 30, 10, false, NOW(), NOW()),
('aaaa0020-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'GV03', 'Tương ớt Chinsu 250g', 12000, 15000, 60, 60, 10, false, NOW(), NOW());

-- 3. BƠM HÌNH ẢNH
INSERT INTO "ProductImages" ("Id", "ProductId", "ImagePath", "IsPrimary") VALUES 
(gen_random_uuid(), 'aaaa0001-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0002-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0003-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0004-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0005-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0006-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0007-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0008-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0009-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0010-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0011-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0012-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0013-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0014-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0015-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0016-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/indomie.jpg', true),
(gen_random_uuid(), 'aaaa0017-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/indomie.jpg', true),
(gen_random_uuid(), 'aaaa0018-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/muoi.jpg', true),
(gen_random_uuid(), 'aaaa0019-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/muoi.jpg', true),
(gen_random_uuid(), 'aaaa0020-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/muoi.jpg', true);

-- 4. BƠM LỊCH SỬ NHẬP HÀNG 
INSERT INTO "ImportLogs" ("Id", "CreatedAt", "TotalAmount", "Status", "IsAutoSaved") VALUES 
('bbbb0001-0000-0000-0000-000000000000', NOW(), 4800000, 1, false);

INSERT INTO "ImportLogDetails" ("Id", "ImportLogId", "ProductId", "QuantityAdded", "ActualImportPrice") VALUES 
(gen_random_uuid(), 'bbbb0001-0000-0000-0000-000000000000', 'aaaa0001-0000-0000-0000-000000000000', 100, 8000),
(gen_random_uuid(), 'bbbb0001-0000-0000-0000-000000000000', 'aaaa0002-0000-0000-0000-000000000000', 100, 8000),
(gen_random_uuid(), 'bbbb0001-0000-0000-0000-000000000000', 'aaaa0006-0000-0000-0000-000000000000', 200, 16000);

-- 5. BƠM ĐƠN HÀNG VÀ CHI TIẾT
-- Lưu ý: Status giờ là 'Paid' theo đúng class Order.Statuses
-- Không truyền cột ReceiptNumber vì nó là Serial tự tăng
INSERT INTO "Orders" ("Id", "Status", "Note", "TotalAmount", "TotalProfit", "IsDraft", "OrderDate", "UpdatedAt") VALUES 
-- 2 Đơn hàng của ngày hôm nay (giữ nguyên, đổi thành Paid)
('ffff0001-0000-0000-0000-000000000000', 'Paid', NULL, 19000, 4000, false, NOW(), NOW()),
('ffff0002-0000-0000-0000-000000000000', 'Paid', 'Khách VIP', 180000, 20000, false, NOW(), NOW()),

-- 3 Đơn hàng test thống kê (Trước ngày 27/03/2026)
-- Đơn 3: Ngày 25/03/2026 - Khách mua 5 lon Coca
('ffff0003-0000-0000-0000-000000000000', 'Paid', NULL, 50000, 10000, false, '2026-03-25 10:30:00+07', '2026-03-25 10:30:00+07'),
-- Đơn 4: Ngày 25/03/2026 - Khách mua 2 hộp Chocopie và 10 gói mì
('ffff0004-0000-0000-0000-000000000000', 'Paid', 'Giao tận nơi', 155000, 30000, false, '2026-03-25 15:45:00+07', '2026-03-25 15:45:00+07'),
-- Đơn 5: Ngày 26/03/2026 - Khách mua 1 thùng Bia Tiger (24 lon)
('ffff0005-0000-0000-0000-000000000000', 'Paid', 'Tiệc công ty', 432000, 48000, false, '2026-03-26 19:20:00+07', '2026-03-26 19:20:00+07');

INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Quantity", "UnitSalePrice", "UnitImportPrice", "TotalPrice") VALUES 
-- Chi tiết Đơn 1
(gen_random_uuid(), 'ffff0001-0000-0000-0000-000000000000', 'aaaa0001-0000-0000-0000-000000000000', 1, 10000, 8000, 10000), -- Coca
(gen_random_uuid(), 'ffff0001-0000-0000-0000-000000000000', 'aaaa0016-0000-0000-0000-000000000000', 2, 4500, 3500, 9000),   -- Mì Hảo Hảo

-- Chi tiết Đơn 2
(gen_random_uuid(), 'ffff0002-0000-0000-0000-000000000000', 'aaaa0006-0000-0000-0000-000000000000', 10, 18000, 16000, 180000), -- Bia Tiger Nâu

-- Chi tiết Đơn 3 (Của ngày 25/03)
(gen_random_uuid(), 'ffff0003-0000-0000-0000-000000000000', 'aaaa0001-0000-0000-0000-000000000000', 5, 10000, 8000, 50000), -- 5 Coca

-- Chi tiết Đơn 4 (Của ngày 25/03)
(gen_random_uuid(), 'ffff0004-0000-0000-0000-000000000000', 'aaaa0011-0000-0000-0000-000000000000', 2, 55000, 45000, 110000), -- 2 Chocopie
(gen_random_uuid(), 'ffff0004-0000-0000-0000-000000000000', 'aaaa0016-0000-0000-0000-000000000000', 10, 4500, 3500, 45000),  -- 10 Mì

-- Chi tiết Đơn 5 (Của ngày 26/03)
(gen_random_uuid(), 'ffff0005-0000-0000-0000-000000000000', 'aaaa0006-0000-0000-0000-000000000000', 24, 18000, 16000, 432000); -- 24 Bia Tiger Nâu

-- 6. BƠM USER
INSERT INTO "Users" ("Id", "Username", "PasswordHash", "HasCompletedOnboarding") VALUES 
(gen_random_uuid(), 'admin', '$2a$11$hZ2XA8fwjUeq42eIvn.QEeOaV1VbPVENZz5/.Wn1RIgqYvbXurWLS', true);

-- 7. BƠM SẢN PHẨM GỢI Ý MUA KÈM
INSERT INTO "ProductRecommendations" ("ProductId", "PairProductId") VALUES 
('aaaa0001-0000-0000-0000-000000000000', 'aaaa0012-0000-0000-0000-000000000000'), -- Mua Coca gợi ý Snack
('aaaa0001-0000-0000-0000-000000000000', 'aaaa0011-0000-0000-0000-000000000000'), -- Mua Coca gợi ý Chocopie
('aaaa0006-0000-0000-0000-000000000000', 'aaaa0012-0000-0000-0000-000000000000'), -- Mua Bia Tiger gợi ý Snack
('aaaa0016-0000-0000-0000-000000000000', 'aaaa0020-0000-0000-0000-000000000000'), -- Mua Mì Hảo Hảo gợi ý Tương ớt
('aaaa0016-0000-0000-0000-000000000000', 'aaaa0003-0000-0000-0000-000000000000'), -- Mua Mì Hảo Hảo gợi ý Redbull
('aaaa0017-0000-0000-0000-000000000000', 'aaaa0018-0000-0000-0000-000000000000'); -- Mua Mì Omachi gợi ý Nước mắm