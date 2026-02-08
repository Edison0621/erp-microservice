-- TimescaleDB Initialization Script
-- This script creates hypertables for time-series analytics

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Create analytics database if not exists
CREATE DATABASE IF NOT EXISTS analyticsdb;

\c analyticsdb;

CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Create hypertable for inventory transactions (time-series)
CREATE TABLE IF NOT EXISTS inventory_transactions_ts (
    time TIMESTAMPTZ NOT NULL,
    transaction_id VARCHAR(50) NOT NULL,
    tenant_id VARCHAR(50),
    warehouse_id VARCHAR(50),
    material_id VARCHAR(50),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity_change DECIMAL(18, 4),
    source_type VARCHAR(50),
    source_id VARCHAR(50),
    performed_by VARCHAR(50),
    PRIMARY KEY (time, transaction_id)
);

-- Convert to hypertable (partitioned by time)
SELECT create_hypertable('inventory_transactions_ts', 'time', if_not_exists => TRUE);

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS idx_inv_ts_material ON inventory_transactions_ts (material_id, time DESC);
CREATE INDEX IF NOT EXISTS idx_inv_ts_warehouse ON inventory_transactions_ts (warehouse_id, time DESC);
CREATE INDEX IF NOT EXISTS idx_inv_ts_source ON inventory_transactions_ts (source_type, source_id, time DESC);

-- Create hypertable for financial cost movements
CREATE TABLE IF NOT EXISTS cost_movements_ts (
    time TIMESTAMPTZ NOT NULL,
    movement_id VARCHAR(50) NOT NULL,
    tenant_id VARCHAR(50),
    material_id VARCHAR(50),
    warehouse_id VARCHAR(50),
    quantity DECIMAL(18, 4),
    unit_cost DECIMAL(18, 4),
    total_value DECIMAL(18, 4),
    movement_type VARCHAR(50), -- RECEIPT, ISSUE, ADJUSTMENT
    source_type VARCHAR(50),
    source_id VARCHAR(50),
    PRIMARY KEY (time, movement_id)
);

SELECT create_hypertable('cost_movements_ts', 'time', if_not_exists => TRUE);

CREATE INDEX IF NOT EXISTS idx_cost_ts_material ON cost_movements_ts (material_id, time DESC);

-- Create continuous aggregate for daily inventory summary
CREATE MATERIALIZED VIEW IF NOT EXISTS daily_inventory_summary
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 day', time) AS day,
    material_id,
    warehouse_id,
    SUM(quantity_change) AS net_change,
    COUNT(*) AS transaction_count
FROM inventory_transactions_ts
GROUP BY day, material_id, warehouse_id
WITH NO DATA;

-- Refresh policy for continuous aggregate (refresh last 7 days every hour)
SELECT add_continuous_aggregate_policy('daily_inventory_summary',
    start_offset => INTERVAL '7 days',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour',
    if_not_exists => TRUE);

-- Create retention policy (keep detailed data for 2 years)
SELECT add_retention_policy('inventory_transactions_ts', INTERVAL '2 years', if_not_exists => TRUE);
SELECT add_retention_policy('cost_movements_ts', INTERVAL '2 years', if_not_exists => TRUE);

COMMENT ON TABLE inventory_transactions_ts IS 'Time-series optimized table for inventory transaction analytics';
COMMENT ON TABLE cost_movements_ts IS 'Time-series optimized table for financial cost movement tracking';
COMMENT ON MATERIALIZED VIEW daily_inventory_summary IS 'Continuous aggregate providing daily inventory movement summaries';
