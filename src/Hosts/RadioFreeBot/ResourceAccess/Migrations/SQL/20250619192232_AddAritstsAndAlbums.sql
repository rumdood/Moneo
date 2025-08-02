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

DROP INDEX "IX_Albums_ArtistId";

DROP INDEX "IX_Albums_SongId";

CREATE TABLE "AlbumArtist" (
    "AlbumsId" INTEGER NOT NULL,
    "ArtistsId" INTEGER NOT NULL,
    CONSTRAINT "PK_AlbumArtist" PRIMARY KEY ("AlbumsId", "ArtistsId"),
    CONSTRAINT "FK_AlbumArtist_Albums_AlbumsId" FOREIGN KEY ("AlbumsId") REFERENCES "Albums" ("id") ON DELETE CASCADE,
    CONSTRAINT "FK_AlbumArtist_Artists_ArtistsId" FOREIGN KEY ("ArtistsId") REFERENCES "Artists" ("id") ON DELETE CASCADE
);

CREATE TABLE "AlbumSong" (
    "AlbumsId" INTEGER NOT NULL,
    "SongsId" INTEGER NOT NULL,
    CONSTRAINT "PK_AlbumSong" PRIMARY KEY ("AlbumsId", "SongsId"),
    CONSTRAINT "FK_AlbumSong_Albums_AlbumsId" FOREIGN KEY ("AlbumsId") REFERENCES "Albums" ("id") ON DELETE CASCADE,
    CONSTRAINT "FK_AlbumSong_Songs_SongsId" FOREIGN KEY ("SongsId") REFERENCES "Songs" ("id") ON DELETE CASCADE
);

CREATE INDEX "IX_AlbumArtist_ArtistsId" ON "AlbumArtist" ("ArtistsId");

CREATE INDEX "IX_AlbumSong_SongsId" ON "AlbumSong" ("SongsId");

CREATE TABLE "ef_temp_Albums" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Albums" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "created_on" TEXT NOT NULL
);

INSERT INTO "ef_temp_Albums" ("id", "Name", "created_on")
SELECT "id", "Name", "created_on"
FROM "Albums";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "Albums";

ALTER TABLE "ef_temp_Albums" RENAME TO "Albums";

COMMIT;

PRAGMA foreign_keys = 1;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250619192232_AddAlbumRelationships', '9.0.5');

