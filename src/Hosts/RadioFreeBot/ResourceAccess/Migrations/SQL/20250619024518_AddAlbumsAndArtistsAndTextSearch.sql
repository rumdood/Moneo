BEGIN TRANSACTION;
CREATE TABLE "Artists" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Artists" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "created_on" TEXT NOT NULL
);

CREATE TABLE "Albums" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Albums" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ArtistId" INTEGER NULL,
    "SongId" INTEGER NULL,
    "created_on" TEXT NOT NULL,
    CONSTRAINT "FK_Albums_Artists_ArtistId" FOREIGN KEY ("ArtistId") REFERENCES "Artists" ("id"),
    CONSTRAINT "FK_Albums_Songs_SongId" FOREIGN KEY ("SongId") REFERENCES "Songs" ("id")
);

CREATE TABLE "ArtistSong" (
    "ArtistsId" INTEGER NOT NULL,
    "SongsId" INTEGER NOT NULL,
    CONSTRAINT "PK_ArtistSong" PRIMARY KEY ("ArtistsId", "SongsId"),
    CONSTRAINT "FK_ArtistSong_Artists_ArtistsId" FOREIGN KEY ("ArtistsId") REFERENCES "Artists" ("id") ON DELETE CASCADE,
    CONSTRAINT "FK_ArtistSong_Songs_SongsId" FOREIGN KEY ("SongsId") REFERENCES "Songs" ("id") ON DELETE CASCADE
);

CREATE INDEX "IX_Albums_ArtistId" ON "Albums" ("ArtistId");

CREATE INDEX "IX_Albums_SongId" ON "Albums" ("SongId");

CREATE INDEX "IX_ArtistSong_SongsId" ON "ArtistSong" ("SongsId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250619024518_AddArtistsAndAlbumsAndTextSearch', '9.0.5');

COMMIT;

