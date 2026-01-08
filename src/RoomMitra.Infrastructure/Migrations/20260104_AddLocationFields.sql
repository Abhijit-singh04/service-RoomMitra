-- Migration: Add location fields to flat_listings and create nearby_essentials table
-- Run this migration against your PostgreSQL database

-- Add latitude/longitude columns to flat_listings
ALTER TABLE flat_listings 
ADD COLUMN IF NOT EXISTS latitude DOUBLE PRECISION NULL,
ADD COLUMN IF NOT EXISTS longitude DOUBLE PRECISION NULL;

-- Create index for geo queries (bounding box pre-filter)
CREATE INDEX IF NOT EXISTS ix_flat_listings_coordinates 
ON flat_listings (latitude, longitude) 
WHERE latitude IS NOT NULL AND longitude IS NOT NULL;

-- Create nearby_essentials table for cached POI data
CREATE TABLE IF NOT EXISTS nearby_essentials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    flat_listing_id UUID NOT NULL REFERENCES flat_listings(id) ON DELETE CASCADE,
    category VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    distance_meters INTEGER NOT NULL,
    fetched_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Index for efficient lookups by listing
CREATE INDEX IF NOT EXISTS ix_nearby_essentials_flat_listing_id 
ON nearby_essentials (flat_listing_id);

-- Comment for documentation
COMMENT ON TABLE nearby_essentials IS 'Cached POI data for listings. Categories: metro_station, grocery_or_supermarket, hospital';
COMMENT ON COLUMN flat_listings.latitude IS 'Latitude for geo-search. Only lat/lon stored, no full address.';
COMMENT ON COLUMN flat_listings.longitude IS 'Longitude for geo-search. Only lat/lon stored, no full address.';
