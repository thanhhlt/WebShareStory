DBCC CHECKIDENT ('Categories', RESEED, 0);
INSERT INTO Categories (Name, Description, Slug, ParentCateId)
VALUES
(N'Vấn đề chung của trang web', N'Những vấn đề chung của trang web, như thông báo, góp ý của thành viên', N'van-de-chung-cua-trang-web', NULL),
(N'Cuộc sống', N'Những câu chuyện trong cộc sống', N'cuoc-song', NULL),
(N'Tình yêu, Gia đình và Mối quan hệ', N'Chia sẻ về tình yêu, gia đình và các mối quan hệ trong cuộc sống', N'tinh-yeu-gia-dinh-va-moi-quan-he', NULL),
(N'Công việc', N'Câu chuyện về sự nghiệp và công việc', N'cong-viec', NULL),
(N'Du lịch và Khám phá', N'Chia sẻ về các chuyến đi và khám phá thế giới', N'du-lich-va-kham-pha', NULL),
(N'Sự kỳ bí và Siêu nhiên', N'Những câu chuyện kỳ lạ và siêu nhiên', N'su-ky-bi-va-sieu-nhien', NULL),
(N'Kỷ niệm', N'Chia sẻ về những kỷ niệm đáng nhớ trong cuộc sống', N'ky-niem', NULL),
(N'Truyện sáng tác', N'Các câu chuyện sáng tác của người dùng', N'truyen-sang-tac', NULL);

INSERT INTO Categories (Name, Description, Slug, ParentCateId)
VALUES
-- Chủ đề con cho "Vấn đề chung của trang web"
(N'Thông báo chung', N'Nơi đăng các thông báo', N'thong-bao-chung', 1),
(N'Hướng dẫn và Trợ giúp', N'Nơi thành viên đăng các bài viết yêu cầu hướng dẫn và trợ giúp khi sử dụng web', N'huong-dan-va-tro-giup', 1),
(N'Xử lý vi phạm - Khiếu nại', N'Nơi các thành viên khiếu nại về các tranh chấp và các lệnh ban, các vấn đề trên web', N'xu-ly-vi-pham-khieu-nai', 1),
(N'Góp ý', N'Khu vực đề thành viên đăng các góp ý cho trang web', N'gop-y', 1),

-- Chủ đề con cho "Cuộc sống"
(N'Sức khỏe và phong cách sống', N'Chia sẻ về cách giữ sức khỏe, lối sống lành mạnh', N'suc-khoe-va-phong-cach-song', 2),
(N'Những câu chuyện đời thường', N'Chia sẻ những câu chuyện nhỏ trong cuộc sống hàng ngày', N'nhung-cau-chuyen-doi-thuong', 2),
(N'Bài học cuộc sống', N'Những bài học quý giá từ trải nghiệm cuộc sống', N'bai-hoc-cuoc-song', 2),
(N'Khó khăn trong cuộc sống', N'Chia sẻ về những thử thách và cách vượt qua', N'kho-khan-va-cach-vuot-qua', 2),

-- Chủ đề con cho "Tình yêu, Gia đình và Mối quan hệ"
(N'Câu chuyện tình yêu', N'Những câu chuyện về tình yêu và hẹn hò', N'cau-chuyen-tinh-yeu', 3),
(N'Gia đình', N'Chia sẻ về mối quan hệ trong gia đình', N'gia-dinh', 3),
(N'Bạn bè và các mối quan hệ khác', N'Câu chuyện về bạn bè và các mối quan hệ xã hội', N'ban-be-va-cac-moi-quan-he-khac', 3),

-- Chủ đề con cho "Công việc"
(N'Thành công trong sự nghiệp', N'Những câu chuyện về thành công trong công việc', N'thanh-cong-trong-su-nghiep', 4),
(N'Khởi nghiệp và ý tưởng', N'Chia sẻ về hành trình khởi nghiệp và các ý tưởng', N'khoi-nghiep-va-y-tuong', 4),
(N'Công việc và áp lực', N'Những chia sẻ về áp lực và cách cân bằng trong công việc', N'cong-viec-va-ap-luc', 4),

-- Chủ đề con cho "Du lịch và Khám phá"
(N'Những chuyến đi đáng nhớ', N'Những câu chuyện và kỷ niệm từ các chuyến đi', N'nhung-chuyen-di-dang-nho', 5),
(N'Khám phá văn hóa', N'Chia sẻ về những nét văn hóa đặc sắc', N'kham-pha-van-hoa', 5),
(N'Hướng dẫn du lịch', N'Các mẹo và kinh nghiệm du lịch', N'huong-dan-du-lich', 5),

-- Chủ đề con cho "Sự kỳ bí và Siêu nhiên"
(N'Hiện tượng kỳ bí', N'Chia sẻ về những hiện tượng khó giải thích', N'hien-tuong-ky-bi', 6),
(N'Câu chuyện tâm linh', N'Những câu chuyện liên quan đến tâm linh', N'cau-chuyen-tam-linh', 6),

-- Chủ đề con cho "Kỷ niệm"
(N'Tuổi thơ', N'Chia sẻ về những kỷ niệm đáng nhớ thời thơ ấu', N'tuoi-tho', 7),
(N'Thời thanh xuân', N'Câu chuyện về những năm tháng thanh xuân', N'thoi-thanh-xuan', 7),
(N'Kỷ niệm đặc biệt', N'Những kỷ niệm sâu sắc và đáng nhớ nhất', N'ky-niem-dac-biet', 7),

-- Chủ đề con cho "Truyện sáng tác"
(N'Truyện ngắn', N'Những truyện ngắn sáng tác bởi người dùng', N'truyen-ngan', 8),
(N'Truyện dài', N'Những tác phẩm truyện dài sáng tác', N'truyen-dai', 8),
(N'Thơ ca', N'Những bài thơ sáng tác', N'tho-ca', 8);