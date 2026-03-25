-- 1. BƠM DANH MỤC (5 Categories)
INSERT INTO "Categories" ("Id", "Name") VALUES 
('11111111-1111-1111-1111-111111111111', 'Nước ngọt'),
('22222222-2222-2222-2222-222222222222', 'Bia & Đồ có cồn'),
('33333333-3333-3333-3333-333333333333', 'Bánh kẹo'),
('44444444-4444-4444-4444-444444444444', 'Mì ăn liền'),
('55555555-5555-5555-5555-555555555555', 'Gia vị');

-- 2. BƠM SẢN PHẨM (20 Products)
INSERT INTO "Products" ("Id", "CategoryId", "SKU", "Name", "ImportPrice", "SalePrice", "StockQuantity", "IsDraft", "CreatedAt", "UpdatedAt") VALUES 
-- Nước ngọt 
('aaaa0001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN01', 'Coca Cola Lon 320ml', 8000, 10000, 100, false, NOW(), NOW()),
('aaaa0002-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN02', 'Pepsi Lon 320ml', 8000, 10000, 100, false, NOW(), NOW()),
('aaaa0003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN03', 'Redbull Thái 250ml', 12000, 15000, 50, false, NOW(), NOW()),
('aaaa0004-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN04', 'Sting Dâu Chai 330ml', 8500, 12000, 80, false, NOW(), NOW()),
('aaaa0005-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', 'NN05', '7Up Lon 320ml', 7500, 10000, 60, false, NOW(), NOW()),
-- Bia 
('aaaa0006-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA01', 'Bia Tiger Nâu Lon 330ml', 16000, 18000, 200, false, NOW(), NOW()),
('aaaa0007-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA02', 'Bia Tiger Bạc Lon 330ml', 18000, 21000, 150, false, NOW(), NOW()),
('aaaa0008-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA03', 'Bia Heineken Lon 330ml', 19000, 22000, 100, false, NOW(), NOW()),
('aaaa0009-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA04', 'Bia 333 Lon 330ml', 13000, 15000, 120, false, NOW(), NOW()),
('aaaa0010-0000-0000-0000-000000000000', '22222222-2222-2222-2222-222222222222', 'BIA05', 'Bia Sài Gòn Xanh Lon 330ml', 14000, 16000, 80, false, NOW(), NOW()),
-- Bánh kẹo 
('aaaa0011-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK01', 'Bánh Chocopie Hộp 12 Cái', 45000, 55000, 30, false, NOW(), NOW()),
('aaaa0012-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK02', 'Snack Oishi Tôm Cay', 4500, 6000, 100, false, NOW(), NOW()),
('aaaa0013-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK03', 'Bánh Quy Danisa 454g', 120000, 145000, 20, false, NOW(), NOW()),
('aaaa0014-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK04', 'Kẹo Dẻo Chupa Chups', 8000, 10000, 50, false, NOW(), NOW()),
('aaaa0015-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', 'BK05', 'Bánh Cosy Marie 144g', 15000, 20000, 40, false, NOW(), NOW()),
-- Mì 
('aaaa0016-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'MI01', 'Mì Hảo Hảo Tôm Chua Cay', 3500, 4500, 500, false, NOW(), NOW()),
('aaaa0017-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', 'MI02', 'Mì Omachi Xốt Vang', 7500, 9000, 200, false, NOW(), NOW()),
-- Gia vị 
('aaaa0018-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'GV01', 'Nước mắm Nam Ngư 750ml', 35000, 42000, 40, false, NOW(), NOW()),
('aaaa0019-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'GV02', 'Dầu ăn Neptune 1 Lít', 45000, 52000, 30, false, NOW(), NOW()),
('aaaa0020-0000-0000-0000-000000000000', '55555555-5555-5555-5555-555555555555', 'GV03', 'Tương ớt Chinsu 250g', 12000, 15000, 60, false, NOW(), NOW());

-- 3. BƠM HÌNH ẢNH
INSERT INTO "ProductImages" ("Id", "ProductId", "ImagePath", "IsPrimary") VALUES 
-- 5 Nước ngọt
(gen_random_uuid(), 'aaaa0001-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0002-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0003-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0004-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
(gen_random_uuid(), 'aaaa0005-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/coca.jpg', true),
-- 5 Bia
(gen_random_uuid(), 'aaaa0006-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0007-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0008-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0009-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
(gen_random_uuid(), 'aaaa0010-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/heineken.jpg', true),
-- 5 Bánh kẹo
(gen_random_uuid(), 'aaaa0011-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0012-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0013-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0014-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
(gen_random_uuid(), 'aaaa0015-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/chocopie.jpg', true),
-- 2 Mì
(gen_random_uuid(), 'aaaa0016-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/indomie.jpg', true),
(gen_random_uuid(), 'aaaa0017-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/indomie.jpg', true),
-- 3 Gia vị
(gen_random_uuid(), 'aaaa0018-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/muoi.jpg', true),
(gen_random_uuid(), 'aaaa0019-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/muoi.jpg', true),
(gen_random_uuid(), 'aaaa0020-0000-0000-0000-000000000000', 'ms-appx:///Assets/Images/muoi.jpg', true);

-- 4. BƠM LỊCH SỬ NHẬP HÀNG (Cấu trúc Master-Detail)
-- Tạo 1 Phiếu nhập tổng (Gồm Coca, Pepsi, Tiger)
INSERT INTO "ImportLogs" ("Id", "CreatedAt", "TotalAmount", "Status", "IsAutoSaved") VALUES 
('bbbb0001-0000-0000-0000-000000000000', NOW(), 4800000, 1, false);

-- Bơm 3 chi tiết nhập hàng vào Phiếu nhập trên
INSERT INTO "ImportLogDetails" ("Id", "ImportLogId", "ProductId", "QuantityAdded", "ActualImportPrice") VALUES 
(gen_random_uuid(), 'bbbb0001-0000-0000-0000-000000000000', 'aaaa0001-0000-0000-0000-000000000000', 100, 8000),
(gen_random_uuid(), 'bbbb0001-0000-0000-0000-000000000000', 'aaaa0002-0000-0000-0000-000000000000', 100, 8000),
(gen_random_uuid(), 'bbbb0001-0000-0000-0000-000000000000', 'aaaa0006-0000-0000-0000-000000000000', 200, 16000);

-- 5. BƠM ĐƠN HÀNG & CHI TIẾT ĐƠN HÀNG 
INSERT INTO "Orders" ("Id", "Status", "TotalAmount", "TotalProfit", "IsDraft", "OrderDate", "UpdatedAt") VALUES 
('ffff0001-0000-0000-0000-000000000000', 'Completed', 19000, 3500, false, NOW(), NOW()),
('ffff0002-0000-0000-0000-000000000000', 'Completed', 180000, 20000, false, NOW(), NOW());

INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Quantity", "UnitSalePrice", "UnitImportPrice", "TotalPrice") VALUES 
(gen_random_uuid(), 'ffff0001-0000-0000-0000-000000000000', 'aaaa0001-0000-0000-0000-000000000000', 1, 10000, 8000, 10000),
(gen_random_uuid(), 'ffff0001-0000-0000-0000-000000000000', 'aaaa0016-0000-0000-0000-000000000000', 2, 4500, 3500, 9000),
(gen_random_uuid(), 'ffff0002-0000-0000-0000-000000000000', 'aaaa0006-0000-0000-0000-000000000000', 10, 18000, 16000, 180000);

-- 6. BƠM TÀI KHOẢN USER (Đã rút gọn các cột, update Hash Password mới)
INSERT INTO "Users" ("Id", "Username", "PasswordHash", "HasCompletedOnboarding") VALUES 
(gen_random_uuid(), 'admin', '$2a$11$hZ2XA8fwjUeq42eIvn.QEeOaV1VbPVENZz5/.Wn1RIgqYvbXurWLS', true);