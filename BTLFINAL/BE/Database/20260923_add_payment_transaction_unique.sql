-- Chạy 1 lần trên PostgreSQL hiện tại trước khi dùng bản code mới.
-- Kiểm tra trùng trước khi tạo unique index.
DO $$
BEGIN
    IF EXISTS (
        SELECT ma_giao_dich
        FROM thanh_toan
        WHERE ma_giao_dich IS NOT NULL
        GROUP BY ma_giao_dich
        HAVING COUNT(*) > 1
    ) THEN
        RAISE EXCEPTION 'Không thể tạo unique index: thanh_toan.ma_giao_dich đang có dữ liệu trùng.';
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_thanh_toan_ma_giao_dich
ON thanh_toan (ma_giao_dich)
WHERE ma_giao_dich IS NOT NULL;
