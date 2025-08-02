BEGIN TRANSACTION;
INSERT INTO Playlists (
    external_id,
    name,
    conversation_id,
    created_on
)
VALUES (
           'PLxWwmAsHCiEhbgBnAloj7zdO8wDkdVe8P',
           'Radio Free Telegram',
           -4862426510,
           '2025-05-29 02:41:53'
       );

INSERT INTO Playlists (
    external_id,
    name,
    conversation_id,
    created_on
)
VALUES (
           'PLxWwmAsHCiEhbgBnAloj7zdO8wDkdVe8P',
           'RFT',
           122243374,
           '2025-05-29 02:41:53'
       );

COMMIT;