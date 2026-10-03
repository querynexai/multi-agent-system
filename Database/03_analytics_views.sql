-- ============================================
-- Analytics Views for Dashboard
-- Run after 01_schema.sql and 02_sample_data.sql
-- ============================================

-- Monthly revenue summary
CREATE MATERIALIZED VIEW mv_monthly_revenue AS
SELECT
    DATE_TRUNC('month', o.order_date) AS month,
    COUNT(DISTINCT o.order_id) AS order_count,
    COUNT(DISTINCT o.customer_id) AS unique_customers,
    SUM(o.total_amount) AS revenue,
    AVG(o.total_amount) AS avg_order_value
FROM orders o
WHERE o.status = 'delivered'
GROUP BY DATE_TRUNC('month', o.order_date)
ORDER BY month;

CREATE UNIQUE INDEX idx_mv_monthly_revenue ON mv_monthly_revenue (month);

-- Top products by revenue
CREATE MATERIALIZED VIEW mv_top_products AS
SELECT
    p.product_id,
    p.name,
    c.name AS category,
    SUM(oi.quantity) AS units_sold,
    SUM(oi.quantity * oi.unit_price) AS revenue
FROM order_items oi
JOIN products p ON p.product_id = oi.product_id
JOIN categories c ON c.category_id = p.category_id
JOIN orders o ON o.order_id = oi.order_id
WHERE o.status = 'delivered'
GROUP BY p.product_id, p.name, c.name
ORDER BY revenue DESC;

CREATE UNIQUE INDEX idx_mv_top_products ON mv_top_products (product_id);

-- Customer growth by month
CREATE MATERIALIZED VIEW mv_customer_growth AS
SELECT
    DATE_TRUNC('month', created_at) AS month,
    COUNT(*) AS new_customers,
    SUM(COUNT(*)) OVER (ORDER BY DATE_TRUNC('month', created_at)) AS cumulative_customers
FROM customers
GROUP BY DATE_TRUNC('month', created_at)
ORDER BY month;

CREATE UNIQUE INDEX idx_mv_customer_growth ON mv_customer_growth (month);

-- Revenue by category
CREATE MATERIALIZED VIEW mv_revenue_by_category AS
SELECT
    c.name AS category,
    SUM(oi.quantity * oi.unit_price) AS revenue,
    SUM(oi.quantity) AS units_sold
FROM order_items oi
JOIN products p ON p.product_id = oi.product_id
JOIN categories c ON c.category_id = p.category_id
JOIN orders o ON o.order_id = oi.order_id
WHERE o.status = 'delivered'
GROUP BY c.name
ORDER BY revenue DESC;

CREATE UNIQUE INDEX idx_mv_revenue_by_category ON mv_revenue_by_category (category);

-- Order status breakdown
CREATE MATERIALIZED VIEW mv_order_status AS
SELECT
    status,
    COUNT(*) AS order_count,
    SUM(total_amount) AS total_value
FROM orders
GROUP BY status;

CREATE UNIQUE INDEX idx_mv_order_status ON mv_order_status (status);

-- Refresh function (call after data changes)
CREATE OR REPLACE FUNCTION refresh_all_analytics_views()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW mv_monthly_revenue;
    REFRESH MATERIALIZED VIEW mv_top_products;
    REFRESH MATERIALIZED VIEW mv_customer_growth;
    REFRESH MATERIALIZED VIEW mv_revenue_by_category;
    REFRESH MATERIALIZED VIEW mv_order_status;
END;
$$ LANGUAGE plpgsql;