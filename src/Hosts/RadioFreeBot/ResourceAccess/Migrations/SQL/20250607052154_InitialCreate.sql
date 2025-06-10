CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Playlists" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Playlists" PRIMARY KEY AUTOINCREMENT,
    "external_id" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "conversation_id" INTEGER NOT NULL,
    "created_on" TEXT NOT NULL
);

CREATE TABLE "Songs" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Songs" PRIMARY KEY AUTOINCREMENT,
    "external_id" TEXT NULL,
    "name" TEXT NOT NULL,
    "original_url" TEXT NOT NULL,
    "created_on" TEXT NOT NULL
);

CREATE TABLE "Users" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "TelegramId" INTEGER NULL,
    "created_on" TEXT NOT NULL
);

CREATE TABLE "PlaylistSongs" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_PlaylistSongs" PRIMARY KEY AUTOINCREMENT,
    "playlist_id" INTEGER NOT NULL,
    "song_id" INTEGER NOT NULL,
    "added_by_user_id" INTEGER NULL,
    "added_at" TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "created_on" TEXT NOT NULL,
    CONSTRAINT "AK_PlaylistSongs_playlist_id_song_id" UNIQUE ("playlist_id", "song_id"),
    CONSTRAINT "FK_PlaylistSongs_Playlists_playlist_id" FOREIGN KEY ("playlist_id") REFERENCES "Playlists" ("id") ON DELETE CASCADE,
    CONSTRAINT "FK_PlaylistSongs_Songs_song_id" FOREIGN KEY ("song_id") REFERENCES "Songs" ("id") ON DELETE CASCADE,
    CONSTRAINT "FK_PlaylistSongs_Users_added_by_user_id" FOREIGN KEY ("added_by_user_id") REFERENCES "Users" ("id") ON DELETE CASCADE
);

CREATE INDEX "IX_PlaylistSongs_added_by_user_id" ON "PlaylistSongs" ("added_by_user_id");

CREATE INDEX "IX_PlaylistSongs_song_id" ON "PlaylistSongs" ("song_id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250607052154_InitialCreate', '9.0.5');

COMMIT;

