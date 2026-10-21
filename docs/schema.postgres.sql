CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE universities (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        short_name character varying(50) NOT NULL,
        latitude double precision NOT NULL,
        longitude double precision NOT NULL,
        address character varying(300) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_universities" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(255) NOT NULL,
        password character varying(255) NOT NULL,
        full_name character varying(150) NOT NULL,
        phone_number character varying(20),
        avatar text,
        role text NOT NULL,
        is_edu_verified boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE rooms (
        id uuid NOT NULL,
        landlord_id uuid NOT NULL,
        university_id uuid,
        title character varying(255) NOT NULL,
        description text NOT NULL,
        address character varying(300) NOT NULL,
        latitude double precision NOT NULL,
        longitude double precision NOT NULL,
        base_price numeric(12,2) NOT NULL,
        electric_price numeric(10,2) NOT NULL,
        water_price numeric(10,2) NOT NULL,
        internet_price numeric(10,2) NOT NULL,
        other_fee numeric(10,2) NOT NULL,
        area double precision NOT NULL,
        max_people integer NOT NULL,
        amenities text NOT NULL,
        status text NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_rooms" PRIMARY KEY (id),
        CONSTRAINT "FK_rooms_universities_university_id" FOREIGN KEY (university_id) REFERENCES universities (id) ON DELETE SET NULL,
        CONSTRAINT "FK_rooms_users_landlord_id" FOREIGN KEY (landlord_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE "Bookings" (
        "Id" uuid NOT NULL,
        "RoomId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "VisitAt" timestamp with time zone NOT NULL,
        "Note" character varying(1000) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Bookings_rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES rooms (id) ON DELETE CASCADE,
        CONSTRAINT "FK_Bookings_users_StudentId" FOREIGN KEY ("StudentId") REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE "Favorites" (
        "UserId" uuid NOT NULL,
        "RoomId" uuid NOT NULL,
        CONSTRAINT "PK_Favorites" PRIMARY KEY ("UserId", "RoomId"),
        CONSTRAINT "FK_Favorites_rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES rooms (id) ON DELETE CASCADE,
        CONSTRAINT "FK_Favorites_users_UserId" FOREIGN KEY ("UserId") REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE reviews (
        id uuid NOT NULL,
        room_id uuid NOT NULL,
        user_id uuid NOT NULL,
        rating integer NOT NULL,
        comment text NOT NULL,
        is_flagged boolean NOT NULL,
        flag_reason text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reviews" PRIMARY KEY (id),
        CONSTRAINT "FK_reviews_rooms_room_id" FOREIGN KEY (room_id) REFERENCES rooms (id) ON DELETE CASCADE,
        CONSTRAINT "FK_reviews_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE room_images (
        id uuid NOT NULL,
        room_id uuid NOT NULL,
        url text NOT NULL,
        public_id text,
        display_order integer NOT NULL,
        is_cover boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_room_images" PRIMARY KEY (id),
        CONSTRAINT "FK_room_images_rooms_room_id" FOREIGN KEY (room_id) REFERENCES rooms (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE TABLE reports (
        id uuid NOT NULL,
        reporter_id uuid NOT NULL,
        room_id uuid,
        review_id uuid,
        reason character varying(255) NOT NULL,
        description text,
        status text NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reports" PRIMARY KEY (id),
        CONSTRAINT "FK_reports_reviews_review_id" FOREIGN KEY (review_id) REFERENCES reviews (id) ON DELETE CASCADE,
        CONSTRAINT "FK_reports_rooms_room_id" FOREIGN KEY (room_id) REFERENCES rooms (id) ON DELETE CASCADE,
        CONSTRAINT "FK_reports_users_reporter_id" FOREIGN KEY (reporter_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_Bookings_RoomId" ON "Bookings" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE UNIQUE INDEX "IX_Bookings_StudentId_RoomId" ON "Bookings" ("StudentId", "RoomId") WHERE "Status" IN ('PENDING', 'CONFIRMED');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_Favorites_RoomId" ON "Favorites" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_reports_reporter_id" ON reports (reporter_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_reports_review_id" ON reports (review_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_reports_room_id" ON reports (room_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_reviews_room_id" ON reviews (room_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE UNIQUE INDEX "IX_reviews_user_id_room_id" ON reviews (user_id, room_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_room_images_room_id" ON room_images (room_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_rooms_landlord_id" ON rooms (landlord_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_rooms_latitude_longitude" ON rooms (latitude, longitude);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_rooms_status" ON rooms (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE INDEX "IX_rooms_university_id" ON rooms (university_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE UNIQUE INDEX "IX_universities_short_name" ON universities (short_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    CREATE UNIQUE INDEX "IX_users_email" ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100657_InitialProduction') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261021100657_InitialProduction', '8.0.32');
    END IF;
END $EF$;
COMMIT;

