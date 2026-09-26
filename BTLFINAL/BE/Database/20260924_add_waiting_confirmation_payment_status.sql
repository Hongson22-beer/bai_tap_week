-- ============================================================
-- PAYMENT STATUS
-- Bổ sung WAITING_CONFIRMATION cho quy trình chuyển khoản
--
-- Flow:
-- PENDING
-- -> WAITING_CONFIRMATION
-- -> PAID / FAILED
-- ============================================================

ALTER TABLE thanh_toan
DROP CONSTRAINT IF EXISTS ck_tt_trang_thai;

ALTER TABLE thanh_toan
ADD CONSTRAINT ck_tt_trang_thai
CHECK (
    trang_thai IN (
        'PENDING',
        'WAITING_CONFIRMATION',
        'PAID',
        'FAILED',
        'REFUND_PENDING',
        'REFUNDED'
    )
);