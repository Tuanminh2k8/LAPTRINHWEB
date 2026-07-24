CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(250) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Combos] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Combos] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [Address] nvarchar(200) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [GoogleId] nvarchar(max) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FastFoods] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [CategoryId] int NOT NULL,
    [Theme] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_FastFoods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FastFoods_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ReceiverName] nvarchar(100) NOT NULL,
    [ReceiverPhone] nvarchar(max) NOT NULL,
    [ReceiverAddress] nvarchar(200) NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [ShippingFee] decimal(18,2) NOT NULL,
    [Discount] decimal(18,2) NOT NULL,
    [Note] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ComboDetails] (
    [ComboId] int NOT NULL,
    [FastFoodId] int NOT NULL,
    [Quantity] int NOT NULL,
    CONSTRAINT [PK_ComboDetails] PRIMARY KEY ([ComboId], [FastFoodId]),
    CONSTRAINT [FK_ComboDetails_Combos_ComboId] FOREIGN KEY ([ComboId]) REFERENCES [Combos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ComboDetails_FastFoods_FastFoodId] FOREIGN KEY ([FastFoodId]) REFERENCES [FastFoods] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [OrderDetails] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [FastFoodId] int NULL,
    [ComboId] int NULL,
    [Quantity] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderDetails_Combos_ComboId] FOREIGN KEY ([ComboId]) REFERENCES [Combos] ([Id]),
    CONSTRAINT [FK_OrderDetails_FastFoods_FastFoodId] FOREIGN KEY ([FastFoodId]) REFERENCES [FastFoods] ([Id]),
    CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [Description], [Name])
VALUES (1, N'Các loại bánh burger thơm ngon', N'Burgers'),
(2, N'Pizza nóng hổi, nhiều phô mai', N'Pizzas'),
(3, N'Gà chiên giòn rụm', N'Gà Rán'),
(4, N'Nước ngọt, khoai tây chiên, kem', N'Thức uống & Tráng miệng');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'ImageUrl', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[Combos]'))
    SET IDENTITY_INSERT [Combos] ON;
INSERT INTO [Combos] ([Id], [Description], [ImageUrl], [Name], [Price])
VALUES (1, N'2 Burger Bò Phô Mai + 1 Khoai Tây Chiên + 2 Coca Cola. Tiết kiệm hơn!', N'/images/combo_family.svg', N'Combo Gia Đình', 150000.0),
(2, N'1 Pizza Hải Sản + 1 Gà Rán Giòn Cay + 1 Khoai Tây Chiên + 2 Coca Cola. Cực vui cực đã!', N'/images/combo_party.svg', N'Combo Tiệc Tùng', 200000.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'ImageUrl', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[Combos]'))
    SET IDENTITY_INSERT [Combos] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Email', N'FullName', N'GoogleId', N'PasswordHash', N'PhoneNumber', N'Role', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [Address], [Email], [FullName], [GoogleId], [PasswordHash], [PhoneNumber], [Role], [Username])
VALUES (1, N'123 Đường Tô Ký, Quận 12, TP.HCM', N'admin@fastfood.com', N'Quản Trị Viên', NULL, N'$2a$11$ezY8eus712l.J/TErYvnveHybjXijpr.j7gucKR7G0q3xlgK6WCc6', N'0987654321', N'Admin', N'admin'),
(2, N'456 Đường Quang Trung, Gò Vấp, TP.HCM', N'customer@fastfood.com', N'Nguyễn Văn Khách', NULL, N'$2a$11$YIt.Q8rHNv0BKrlePDKezedHKn7OjqQYdbTAS7EramaJSAVPn.R/6', N'0912345678', N'Customer', N'customer');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Email', N'FullName', N'GoogleId', N'PasswordHash', N'PhoneNumber', N'Role', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'Description', N'ImageUrl', N'Name', N'Price', N'Theme') AND [object_id] = OBJECT_ID(N'[FastFoods]'))
    SET IDENTITY_INSERT [FastFoods] ON;
INSERT INTO [FastFoods] ([Id], [CategoryId], [Description], [ImageUrl], [Name], [Price], [Theme])
VALUES (1, 1, N'Bánh burger kẹp thịt bò nướng thơm ngon cùng lớp phô mai béo ngậy và rau tươi.', N'/images/burger_cheese.svg', N'Burger Bò Phô Mai', 55000.0, N'Gia đình'),
(2, 1, N'Bánh burger kẹp thịt gà chiên giòn tan, sốt mayonnaise và xà lách ngon tuyệt.', N'/images/burger_chicken.svg', N'Burger Gà Giòn', 50000.0, N'Trẻ em'),
(3, 2, N'Pizza với mực, tôm, thanh cua tươi ngon cùng phô mai Mozzarella thượng hạng.', N'/images/pizza_seafood.svg', N'Pizza Hải Sản', 120000.0, N'Tiệc tùng'),
(4, 2, N'Pizza đầy ắp thịt nguội, xúc xích pepperoni, ớt chuông, nấm và phô mai.', N'/images/pizza_mixed.svg', N'Pizza Thập Cẩm', 110000.0, N'Gia đình'),
(5, 3, N'Một miếng gà rán giòn rụm, tẩm ướp gia vị cay nồng đậm đà.', N'/images/chicken_spicy.svg', N'Gà Rán Giòn Cay', 35000.0, N'Ăn vặt'),
(6, 4, N'Khoai tây chiên vàng giòn, rắc chút muối thơm ngon.', N'/images/fries.svg', N'Khoai Tây Chiên', 25000.0, N'Ăn vặt'),
(7, 4, N'Nước ngọt có ga Coca Cola mát lạnh.', N'/images/coca.svg', N'Coca Cola', 15000.0, N'Ăn uống');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'Description', N'ImageUrl', N'Name', N'Price', N'Theme') AND [object_id] = OBJECT_ID(N'[FastFoods]'))
    SET IDENTITY_INSERT [FastFoods] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ComboId', N'FastFoodId', N'Quantity') AND [object_id] = OBJECT_ID(N'[ComboDetails]'))
    SET IDENTITY_INSERT [ComboDetails] ON;
INSERT INTO [ComboDetails] ([ComboId], [FastFoodId], [Quantity])
VALUES (1, 1, 2),
(1, 6, 1),
(1, 7, 2),
(2, 3, 1),
(2, 5, 1),
(2, 6, 1),
(2, 7, 2);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ComboId', N'FastFoodId', N'Quantity') AND [object_id] = OBJECT_ID(N'[ComboDetails]'))
    SET IDENTITY_INSERT [ComboDetails] OFF;
GO


CREATE INDEX [IX_ComboDetails_FastFoodId] ON [ComboDetails] ([FastFoodId]);
GO


CREATE INDEX [IX_FastFoods_CategoryId] ON [FastFoods] ([CategoryId]);
GO


CREATE INDEX [IX_OrderDetails_ComboId] ON [OrderDetails] ([ComboId]);
GO


CREATE INDEX [IX_OrderDetails_FastFoodId] ON [OrderDetails] ([FastFoodId]);
GO


CREATE INDEX [IX_OrderDetails_OrderId] ON [OrderDetails] ([OrderId]);
GO


CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
GO


CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
GO


CREATE INDEX [IX_Orders_IsDeleted] ON [Orders] ([IsDeleted]);
GO


CREATE INDEX [IX_Orders_OrderDate] ON [Orders] ([OrderDate]);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Discount', N'IsDeleted', N'Note', N'OrderDate', N'PaymentMethod', N'ReceiverAddress', N'ReceiverName', N'ReceiverPhone', N'ShippingFee', N'Status', N'TotalAmount', N'UpdatedAt', N'UserId') AND [object_id] = OBJECT_ID(N'[Orders]'))
    SET IDENTITY_INSERT [Orders] ON;
INSERT INTO [Orders] ([Id], [UserId], [OrderDate], [TotalAmount], [Status], [ReceiverName], [ReceiverPhone], [ReceiverAddress], [PaymentMethod], [ShippingFee], [Discount], [Note], [IsDeleted], [UpdatedAt])
VALUES (1, 2, N'2026-07-19 00:00:00', 205000.0, N'Delivered', N'Nguyễn Văn Khách', N'0912345678', N'456 Đường Quang Trung, Gò Vấp, TP.HCM', N'COD', 0.0, 0.0, N'Giao hàng giờ hành chính', 0, N'2026-07-21 00:00:00'),
(2, 2, N'2026-07-22 00:00:00', 150000.0, N'Shipping', N'Nguyễn Văn Khách', N'0912345678', N'456 Đường Quang Trung, Gò Vấp, TP.HCM', N'COD', 15000.0, 10000.0, N'Giao nhanh nếu có thể', 0, N'2026-07-23 00:00:00'),
(3, 1, N'2026-07-23 00:00:00', 235000.0, N'Preparing', N'Quản Trị Viên', N'0987654321', N'123 Đường Tô Ký, Quận 12, TP.HCM', N'COD', 0.0, 0.0, N'Không hàng sốt', 0, N'2026-07-23 12:00:00'),
(4, 2, N'2026-07-24 10:00:00', 110000.0, N'Pending', N'Nguyễn Văn Khách', N'0912345678', N'456 Đường Quang Trung, Gò Vấp, TP.HCM', N'COD', 0.0, 5000.0, N'', 0, N'2026-07-24 11:00:00'),
(5, 2, N'2026-07-14 00:00:00', 35000.0, N'Cancelled', N'Nguyễn Văn Khách', N'0912345678', N'456 Đường Quang Trung, Gò Vấp, TP.HCM', N'COD', 0.0, 0.0, N'Khách hủy do thay đổi ý định', 0, N'2026-07-15 00:00:00');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Discount', N'IsDeleted', N'Note', N'OrderDate', N'PaymentMethod', N'ReceiverAddress', N'ReceiverName', N'ReceiverPhone', N'ShippingFee', N'Status', N'TotalAmount', N'UpdatedAt', N'UserId') AND [object_id] = OBJECT_ID(N'[Orders]'))
    SET IDENTITY_INSERT [Orders] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ComboId', N'FastFoodId', N'OrderId', N'Price', N'Quantity') AND [object_id] = OBJECT_ID(N'[OrderDetails]'))
    SET IDENTITY_INSERT [OrderDetails] ON;
INSERT INTO [OrderDetails] ([Id], [OrderId], [FastFoodId], [ComboId], [Quantity], [Price])
VALUES (1, 1, 1, NULL, 2, 55000.0),
(2, 1, 6, NULL, 1, 25000.0),
(3, 1, 7, NULL, 2, 15000.0),
(4, 2, 3, NULL, 1, 120000.0),
(5, 2, 5, NULL, 2, 35000.0),
(6, 2, 6, NULL, 1, 25000.0),
(7, 3, 2, NULL, 1, 50000.0),
(8, 3, 7, NULL, 2, 15000.0),
(9, 3, 4, NULL, 1, 110000.0),
(10, 4, 1, NULL, 1, 55000.0),
(11, 4, 6, NULL, 1, 25000.0),
(12, 5, 5, NULL, 1, 35000.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ComboId', N'FastFoodId', N'OrderId', N'Price', N'Quantity') AND [object_id] = OBJECT_ID(N'[OrderDetails]'))
    SET IDENTITY_INSERT [OrderDetails] OFF;
GO
