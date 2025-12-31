-- Create the FlatListings table for RoomMitra
-- Run this SQL in your PostgreSQL database (testdb)

CREATE TABLE IF NOT EXISTS "FlatListings" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL DEFAULT '',
    "City" VARCHAR(100) NOT NULL DEFAULT 'Bengaluru',
    "Locality" VARCHAR(200) NOT NULL,
    "FlatType" INT NOT NULL,           -- 1=OneBhk, 2=TwoBhk, 3=ThreeBhk
    "RoomType" INT NOT NULL,           -- 1=PrivateRoom, 2=SharedRoom
    "Furnishing" INT NOT NULL,         -- 1=Unfurnished, 2=SemiFurnished, 3=FullyFurnished
    "Rent" DECIMAL(18,2) NOT NULL,
    "Deposit" DECIMAL(18,2) NOT NULL,
    "Amenities" JSONB NOT NULL DEFAULT '[]',
    "Preferences" JSONB NOT NULL DEFAULT '[]',
    "AvailableFrom" DATE,
    "Images" JSONB NOT NULL DEFAULT '[]',
    "PostedByUserId" UUID NOT NULL,
    "Status" INT NOT NULL DEFAULT 1,   -- 1=Active, 2=Inactive, 3=Deleted
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS "IX_FlatListings_City" ON "FlatListings" ("City");
CREATE INDEX IF NOT EXISTS "IX_FlatListings_Locality" ON "FlatListings" ("Locality");
CREATE INDEX IF NOT EXISTS "IX_FlatListings_Rent" ON "FlatListings" ("Rent");
CREATE INDEX IF NOT EXISTS "IX_FlatListings_Status" ON "FlatListings" ("Status");
CREATE INDEX IF NOT EXISTS "IX_FlatListings_PostedByUserId" ON "FlatListings" ("PostedByUserId");
CREATE INDEX IF NOT EXISTS "IX_FlatListings_CreatedAt" ON "FlatListings" ("CreatedAt" DESC);

-- Combined index for search queries
CREATE INDEX IF NOT EXISTS "IX_FlatListings_Search" ON "FlatListings" ("City", "Status", "Rent");
