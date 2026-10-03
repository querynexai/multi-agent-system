-- ============================================
-- Intelligent Document Processing Schema
-- Reuses e-commerce tables (customers, orders, products)
-- ============================================

-- Uploaded documents
CREATE TABLE IF NOT EXISTS invoices (
    invoice_id SERIAL PRIMARY KEY,
    filename VARCHAR(255) NOT NULL,
    file_size INT,
    mime_type VARCHAR(100),
    document_type VARCHAR(50),           -- invoice, purchase_order, receipt, contract
    raw_text TEXT,                        -- OCR output
    vendor_name VARCHAR(255),
    invoice_number VARCHAR(100),
    invoice_date DATE,
    due_date DATE,
    subtotal DECIMAL(12,2),
    tax_amount DECIMAL(12,2),
    total_amount DECIMAL(12,2),
    currency VARCHAR(10) DEFAULT 'INR',
    status VARCHAR(50) DEFAULT 'pending', -- pending, processed, needs_review, approved, rejected
    confidence_score DECIMAL(5,4),        -- 0.0 to 1.0 overall
    ai_model VARCHAR(100),
    processing_time_ms INT,
    related_order_id INT REFERENCES orders(order_id),
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP,
    reviewed_by VARCHAR(100),
    reviewed_at TIMESTAMP,
    notes TEXT
);

CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices(status);
CREATE INDEX IF NOT EXISTS idx_invoices_uploaded ON invoices(uploaded_at DESC);
CREATE INDEX IF NOT EXISTS idx_invoices_vendor ON invoices(vendor_name);

-- Line items extracted from each invoice
CREATE TABLE IF NOT EXISTS invoice_line_items (
    line_item_id SERIAL PRIMARY KEY,
    invoice_id INT REFERENCES invoices(invoice_id) ON DELETE CASCADE,
    line_number INT,
    description TEXT,
    quantity DECIMAL(10,3),
    unit_price DECIMAL(12,2),
    total_price DECIMAL(12,2),
    confidence_score DECIMAL(5,4),
    product_id INT REFERENCES products(product_id)  -- matches to catalog if possible
);

CREATE INDEX IF NOT EXISTS idx_line_items_invoice ON invoice_line_items(invoice_id);

-- Field-level extraction audit (for reviewing edits)
CREATE TABLE IF NOT EXISTS extraction_fields (
    field_id SERIAL PRIMARY KEY,
    invoice_id INT REFERENCES invoices(invoice_id) ON DELETE CASCADE,
    field_name VARCHAR(100),
    field_value TEXT,
    confidence_score DECIMAL(5,4),
    was_edited BOOLEAN DEFAULT FALSE,
    original_ai_value TEXT,
    edited_by VARCHAR(100),
    edited_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_extraction_invoice ON extraction_fields(invoice_id);