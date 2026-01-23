# Create a test environment for Guacamole Server with docker-compose

## Preconditions

Make sure you have Docker and Docker Compose installed on your system.
You also need a Traefik instance with a network named `traefik-test` for reverse proxying. Adjust the Traefik labels in the `docker-compose.yml` file if your setup differs.

## Access Guacamole
After setup of Guacamole and Traefik, you can access the Guacamole web interface by navigating to: `https://my.docker.host/guacamole/`

## Please note
This setup is intended for testing and demonstration purposes only.
It is not recommended for production use due to security and performance considerations.
Sandbox SSH servers are configured with weak credentials (`demo`/`guest`) and should not be exposed to untrusted networks.
Guacamole users `guacadmin/guacadmin`, `admin/admin` and `demo/demo` are created with fixed password hashes for easy access.

The PostgreSQL database is ephemeral and will reset on every start, losing all data. This is intentional for testing purposes. This means that every time you start the environment, the database will be re-initialized with the predefined schema and seed data. The SSH sandbox containers are set up to allow no real user interaction for security reasons.
    
## Create a docker-compose.yml file
Create a file named `docker-compose.yml` with the following content:
```yaml
version: "3.8"

services:

  # --- Generate DB schema + seed into a shared volume ---
  dbfiles:
    image: guacamole/guacamole:latest
    user: "0:0"
    networks:
      - guacdemo-internal
    volumes:
      - guacdemo_dbinit:/dbinit:rw
    restart: "no"
    command:
      - /bin/sh
      - -lc
      - |
          set -eux
          mkdir -p /dbinit
          chmod 0777 /dbinit

          /opt/guacamole/bin/initdb.sh --postgresql > /dbinit/001-schema.sql

          cat > /dbinit/999-seed.sql <<'SQL'
            -- 999-seed.sql
            -- Creates users 'admin' and 'demo' with fixed password hash/salt
            -- and grants 'admin' the same system permissions as 'guacadmin'.

            -- Ensure entities exist
            INSERT INTO guacamole_entity (name, type)
            SELECT 'admin', 'USER'
            WHERE NOT EXISTS (SELECT 1 FROM guacamole_entity WHERE name='admin' AND type='USER');

            INSERT INTO guacamole_entity (name, type)
            SELECT 'demo', 'USER'
            WHERE NOT EXISTS (SELECT 1 FROM guacamole_entity WHERE name='demo' AND type='USER');

            -- Create admin user (if missing) with fixed hash+salt
            INSERT INTO guacamole_user (
              entity_id, password_hash, password_salt, password_date, disabled, expired
            )
            SELECT
              e.entity_id,
              decode('24c08f6c702f8f9f2202fdef0aa8169c8e2631f008a5831ae0f3fe1c9da97498','hex'),
              decode('7ffadaa0434c77c677368ae1cae909b2a1f94bc4036e091cc69224cbffec624a','hex'),
              CURRENT_TIMESTAMP,
              FALSE,
              FALSE
            FROM guacamole_entity e
            WHERE e.name='admin' AND e.type='USER'
              AND NOT EXISTS (
                SELECT 1 FROM guacamole_user u WHERE u.entity_id = e.entity_id
              );

            -- Create demo user (if missing) with fixed hash+salt
            INSERT INTO guacamole_user (
              entity_id, password_hash, password_salt, password_date, disabled, expired
            )
            SELECT
              e.entity_id,
              decode('46db505c68291525451ade3edf2a3ad18ea980cad288bc757a62254ec8867ba9','hex'),
              decode('0c0b42f73b0f9b09e6892e5659b7d398bdd651e789441c74007f5c03c9261338','hex'),
              CURRENT_TIMESTAMP,
              FALSE,
              FALSE
            FROM guacamole_entity e
            WHERE e.name='demo' AND e.type='USER'
              AND NOT EXISTS (
                SELECT 1 FROM guacamole_user u WHERE u.entity_id = e.entity_id
              );

            -- Grant admin the same system permissions as guacadmin (idempotent)
            INSERT INTO guacamole_system_permission (entity_id, permission)
            SELECT
              admin_e.entity_id,
              p.permission
            FROM guacamole_system_permission p
            JOIN guacamole_entity guacadmin_e
              ON guacadmin_e.entity_id = p.entity_id
             AND guacadmin_e.name = 'guacadmin'
             AND guacadmin_e.type = 'USER'
            JOIN guacamole_entity admin_e
              ON admin_e.name = 'admin'
             AND admin_e.type = 'USER'
            WHERE NOT EXISTS (
              SELECT 1
              FROM guacamole_system_permission x
              WHERE x.entity_id = admin_e.entity_id
                AND x.permission = p.permission
            );

            -- -------------------------------------------------------------------
            -- SSH connections: sandbox1 + sandbox2
            -- admin + guacadmin: ADMINISTER (inkl. bearbeiten)
            -- demo: READ (sehen/benutzen/connecten)
            -- -------------------------------------------------------------------

            -- 1) Connections anlegen (falls nicht vorhanden)
            INSERT INTO guacamole_connection (connection_name, protocol, max_connections, max_connections_per_user)
            SELECT 'SSH - sandbox1', 'ssh', NULL, NULL
            WHERE NOT EXISTS (SELECT 1 FROM guacamole_connection WHERE connection_name='SSH - sandbox1');

            INSERT INTO guacamole_connection (connection_name, protocol, max_connections, max_connections_per_user)
            SELECT 'SSH - sandbox2', 'ssh', NULL, NULL
            WHERE NOT EXISTS (SELECT 1 FROM guacamole_connection WHERE connection_name='SSH - sandbox2');

            -- 2) Parameter setzen (idempotent)

            -- sandbox1 params
            INSERT INTO guacamole_connection_parameter (connection_id, parameter_name, parameter_value)
            SELECT c.connection_id, p.parameter_name, p.parameter_value
            FROM guacamole_connection c
            JOIN (
              VALUES
                ('hostname', 'sshbox1'),
                ('port',     '2222'),
                ('username', 'sandbox1'),
                ('password', 'sandbox1')
            ) AS p(parameter_name, parameter_value) ON TRUE
            WHERE c.connection_name='SSH - sandbox1'
            AND NOT EXISTS (
              SELECT 1 FROM guacamole_connection_parameter x
              WHERE x.connection_id=c.connection_id AND x.parameter_name=p.parameter_name
            );

            -- sandbox2 params
            INSERT INTO guacamole_connection_parameter (connection_id, parameter_name, parameter_value)
            SELECT c.connection_id, p.parameter_name, p.parameter_value
            FROM guacamole_connection c
            JOIN (
              VALUES
                ('hostname', 'sshbox2'),
                ('port',     '2222'),
                ('username', 'sandbox2'),
                ('password', 'sandbox2')
            ) AS p(parameter_name, parameter_value) ON TRUE
            WHERE c.connection_name='SSH - sandbox2'
            AND NOT EXISTS (
              SELECT 1 FROM guacamole_connection_parameter x
              WHERE x.connection_id=c.connection_id AND x.parameter_name=p.parameter_name
            );

            -- 3) Rechte vergeben
            -- demo: READ
            INSERT INTO guacamole_connection_permission (entity_id, connection_id, permission)
            SELECT e.entity_id, c.connection_id, 'READ'
            FROM guacamole_entity e
            JOIN guacamole_connection c ON c.connection_name IN ('SSH - sandbox1','SSH - sandbox2')
            WHERE e.type='USER' AND e.name='demo'
            AND NOT EXISTS (
              SELECT 1 FROM guacamole_connection_permission p
              WHERE p.entity_id=e.entity_id AND p.connection_id=c.connection_id AND p.permission='READ'
            );

            -- admin + guacadmin: ADMINISTER
            INSERT INTO guacamole_connection_permission (entity_id, connection_id, permission)
            SELECT e.entity_id, c.connection_id, 'ADMINISTER'
            FROM guacamole_entity e
            JOIN guacamole_connection c ON c.connection_name IN ('SSH - sandbox1','SSH - sandbox2')
            WHERE e.type='USER' AND e.name IN ('admin','guacadmin')
            AND NOT EXISTS (
              SELECT 1 FROM guacamole_connection_permission p
              WHERE p.entity_id=e.entity_id AND p.connection_id=c.connection_id AND p.permission='ADMINISTER'
            );

          SQL

          echo "dbfiles ready"
          ls -l /dbinit

  # --- PostgreSQL (ephemeral, auto-reset on every start) ---
  postgres:
    image: postgres:16-alpine
    depends_on:
      - dbfiles
    environment:
      POSTGRES_DB: guacamole_db
      POSTGRES_USER: guacamole_user
      POSTGRES_PASSWORD: guacamole_pass
    tmpfs:
      - /var/lib/postgresql/data
    volumes:
      - guacdemo_dbinit:/docker-entrypoint-initdb.d:ro
    networks:
      - guacdemo-internal
    restart: "no"
    labels:
      - "com.centurylinklabs.watchtower.enable=false"

  # --- guacd ---
  guacd:
    image: guacamole/guacd:latest
    networks:
      - guacdemo-internal
    restart: "no"
    labels:
      - "com.centurylinklabs.watchtower.enable=false"

  # --- Guacamole Webapp ---
  guacamole:
    image: guacamole/guacamole:latest
    depends_on:
      - postgres
      - guacd
    environment:
      GUACD_HOSTNAME: guacd
      GUACD_PORT: "4822"

      POSTGRESQL_HOSTNAME: postgres
      POSTGRESQL_PORT: "5432"
      POSTGRESQL_DATABASE: guacamole_db
      POSTGRESQL_USERNAME: guacamole_user
      POSTGRESQL_PASSWORD: guacamole_pass

    networks:
      - guacdemo-internal
      - traefik-test
    restart: "no"
    command:
      - /bin/bash
      - -lc
      - >
          set -eux;

          echo "Waiting for postgres TCP...";
          for i in $(seq 1 60); do
            (echo > /dev/tcp/postgres/5432) >/dev/null 2>&1 && break;
            sleep 1;
          done;

          echo "Waiting for schema (guacamole_entity)...";
          for i in $(seq 1 60); do
            PGPASSWORD="guacamole_pass" \
            psql -h postgres -U guacamole_user -d guacamole_db -c "SELECT 1 FROM guacamole_entity LIMIT 1;" >/dev/null 2>&1 && break;
            sleep 1;
          done;

          echo "Starting Guacamole…";
          exec /opt/guacamole/bin/entrypoint.sh
    labels:
      - "com.centurylinklabs.watchtower.enable=false"
      - "traefik.enable=true"
      - "traefik.http.routers.guacdemo.rule=Host(`services10.mgmt.compumaster.de`) && PathPrefix(`/guacamole`)"
      - "traefik.http.routers.guacdemo.entrypoints=websecure"
      - "traefik.http.routers.guacdemo.tls=true"
      - "traefik.http.services.guacdemo.loadbalancer.server.port=8080"
      - "traefik.docker.network=traefik-test"

  # --- SSH Sandbox 1 ---
  sshbox1:
    image: lscr.io/linuxserver/openssh-server:latest
    environment:
      PUID: 1000
      PGID: 1000
      TZ: Europe/Berlin
      PASSWORD_ACCESS: "true"
      USER_NAME: sandbox1
      USER_PASSWORD: sandbox1
      SUDO_ACCESS: "false"
    hostname: sandbox1
    networks:
      - guacdemo-internal
    restart: "no"
    labels:
      - "com.centurylinklabs.watchtower.enable=false"

  # --- SSH Sandbox 2 ---
  sshbox2:
    image: lscr.io/linuxserver/openssh-server:latest
    environment:
      PUID: 1000
      PGID: 1000
      TZ: Europe/Berlin
      PASSWORD_ACCESS: "true"
      USER_NAME: sandbox2
      USER_PASSWORD: sandbox2
      SUDO_ACCESS: "false"
    hostname: sandbox2
    networks:
      - guacdemo-internal
    restart: "no"
    labels:
      - "com.centurylinklabs.watchtower.enable=false"

networks:
  guacdemo-internal:
    driver: bridge
    internal: true
  traefik-test:
    external: true

volumes:
  guacdemo_dbinit:
```

## Compose up
Run the following command to start the Guacamole test environment:
```bash
docker-compose up -d
```
This will start the PostgreSQL database, guacd, Guacamole web application, and two SSH sandbox servers.
