using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistence.Migrations
{
    /// <summary>
    ///     Funciones PL/pgSQL del cargue masivo de trayectos (E4). La tabla de aterrizaje
    ///     `trayectos_tmp` se creó en la migración anterior; acá se agrega el trabajo masivo que
    ///     exige el enunciado: cargar, validar y procesar en una sola transacción.
    /// </summary>
    public partial class AddCargueTrayectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION sp_trayecto_cargue(p_id_cargue bigint, p_filas jsonb)
                RETURNS integer
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    v_total integer;
                BEGIN
                    INSERT INTO trayectos_tmp (
                        id,
                        id_cargue,
                        codigo_ruta,
                        identificacion_conductor,
                        placa_vehiculo,
                        ubicacion,
                        orden_parada,
                        latitud,
                        longitud,
                        login_usuario,
                        estado,
                        created_at_utc)
                    SELECT
                        gen_random_uuid(),
                        p_id_cargue,
                        f.codigo_ruta,
                        f.identificacion_conductor,
                        f.placa_vehiculo,
                        f.ubicacion,
                        f.orden_parada,
                        f.latitud,
                        f.longitud,
                        f.login_usuario,
                        'CARGADO',
                        now()
                    FROM jsonb_to_recordset(p_filas) AS f(
                        codigo_ruta varchar(32),
                        identificacion_conductor varchar(20),
                        placa_vehiculo varchar(10),
                        ubicacion varchar(200),
                        orden_parada varchar(10),
                        latitud varchar(30),
                        longitud varchar(30),
                        login_usuario varchar(64));

                    GET DIAGNOSTICS v_total = ROW_COUNT;
                    RETURN v_total;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION sp_trayecto_validar(p_id_cargue bigint)
                RETURNS TABLE (validados integer, errores integer)
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    v_validados integer;
                    v_errores integer;
                BEGIN
                    WITH base AS (
                        SELECT
                            t.*,
                            upper(btrim(coalesce(t.codigo_ruta, ''))) AS codigo_norm,
                            btrim(coalesce(t.identificacion_conductor, '')) AS identificacion_norm,
                            upper(btrim(coalesce(t.placa_vehiculo, ''))) AS placa_norm,
                            btrim(coalesce(t.login_usuario, '')) AS login_norm,
                            CASE WHEN t.orden_parada ~ '^[0-9]+$'
                                 THEN t.orden_parada::integer
                                 ELSE NULL END AS orden_num,
                            CASE WHEN t.latitud ~ '^[+-]?[0-9]+([.][0-9]+)?$'
                                 THEN t.latitud::double precision
                                 ELSE NULL END AS lat_num,
                            CASE WHEN t.longitud ~ '^[+-]?[0-9]+([.][0-9]+)?$'
                                 THEN t.longitud::double precision
                                 ELSE NULL END AS lon_num
                        FROM trayectos_tmp t
                        WHERE t.id_cargue = p_id_cargue
                          AND t.estado = 'CARGADO'
                    ),
                    con_persona AS (
                        SELECT
                            b.*,
                            p.id AS persona_id,
                            p.tipo_persona,
                            p.licencia_conduccion,
                            p.fecha_vigencia_licencia,
                            v.id AS vehiculo_id
                        FROM base b
                        LEFT JOIN personas p ON p.numero_identificacion = b.identificacion_norm
                        LEFT JOIN vehiculos v ON v.placa = b.placa_norm
                    ),
                    con_asociacion AS (
                        SELECT
                            cp.*,
                            cv.id AS asociacion_id,
                            cv.estado AS asociacion_estado
                        FROM con_persona cp
                        LEFT JOIN conductores_vehiculos cv
                            ON cv.id_persona = cp.persona_id
                           AND cv.id_vehiculo = cp.vehiculo_id
                    ),
                    con_documentos AS (
                        SELECT
                            ca.*,
                            (SELECT count(*)
                               FROM documentos_vehiculo d
                              WHERE d.id_vehiculo = ca.vehiculo_id) AS total_documentos,
                            (SELECT count(*)
                               FROM documentos_vehiculo d
                              WHERE d.id_vehiculo = ca.vehiculo_id
                                AND (d.estado <> 'HABILITADO' OR d.fecha_vencimiento <= now())) AS documentos_invalidos
                        FROM con_asociacion ca
                    ),
                    con_login AS (
                        SELECT
                            cd.*,
                            (SELECT count(*)
                               FROM usuarios u
                              WHERE u.login = cd.login_norm) AS logins
                        FROM con_documentos cd
                    ),
                    agrupadas AS (
                        -- PostgreSQL no soporta count(DISTINCT ...) OVER (...), así que la
                        -- coherencia por ruta se agrega primero y luego se une a cada fila.
                        SELECT
                            codigo_norm,
                            count(*) AS total_ruta,
                            count(DISTINCT identificacion_norm) AS conductores_ruta,
                            count(DISTINCT placa_norm) AS vehiculos_ruta,
                            count(DISTINCT login_norm) AS logins_ruta,
                            count(*) FILTER (WHERE orden_num = 0) AS iniciales_ruta
                        FROM con_login
                        GROUP BY codigo_norm
                    ),
                    evaluadas AS (
                        SELECT
                            cl.*,
                            g.total_ruta,
                            g.conductores_ruta,
                            g.vehiculos_ruta,
                            g.logins_ruta,
                            g.iniciales_ruta,
                            count(*) OVER (PARTITION BY cl.codigo_norm, cl.orden_parada) AS orden_repetido
                        FROM con_login cl
                        JOIN agrupadas g ON g.codigo_norm = cl.codigo_norm
                    )
                    UPDATE trayectos_tmp t
                    SET id_persona = e.persona_id,
                        id_vehiculo = e.vehiculo_id,
                        estado = CASE WHEN e.motivo IS NULL THEN 'VALIDADO' ELSE 'ERROR' END,
                        observacion = e.motivo,
                        updated_at_utc = now()
                    FROM (
                        SELECT
                            e.id,
                            e.persona_id,
                            e.vehiculo_id,
                            NULLIF(concat_ws(' ',
                                CASE WHEN e.codigo_norm = ''
                                     THEN 'El código de ruta es obligatorio.' END,
                                CASE WHEN e.identificacion_norm = ''
                                     THEN 'La identificación del conductor es obligatoria.' END,
                                CASE WHEN e.placa_norm = ''
                                     THEN 'La placa del vehículo es obligatoria.' END,
                                CASE WHEN btrim(coalesce(e.ubicacion, '')) = ''
                                     THEN 'La ubicación es obligatoria.' END,
                                CASE WHEN e.login_norm = ''
                                     THEN 'El login del usuario es obligatorio.' END,
                                CASE WHEN e.orden_num IS NULL OR e.orden_num < 0 OR e.orden_num > 6
                                     THEN 'El orden de parada debe estar entre 0 y 6.' END,
                                CASE WHEN e.latitud IS NOT NULL
                                          AND btrim(e.latitud) <> ''
                                          AND e.lat_num IS NULL
                                     THEN 'La latitud no es un número válido.' END,
                                CASE WHEN e.longitud IS NOT NULL
                                          AND btrim(e.longitud) <> ''
                                          AND e.lon_num IS NULL
                                     THEN 'La longitud no es un número válido.' END,
                                CASE WHEN e.lat_num IS NOT NULL
                                          AND (e.lat_num < -90 OR e.lat_num > 90)
                                     THEN 'La latitud está fuera de rango.' END,
                                CASE WHEN e.lon_num IS NOT NULL
                                          AND (e.lon_num < -180 OR e.lon_num > 180)
                                     THEN 'La longitud está fuera de rango.' END,
                                CASE WHEN (e.latitud IS NULL OR btrim(e.latitud) = '')
                                          <> (e.longitud IS NULL OR btrim(e.longitud) = '')
                                     THEN 'La latitud y la longitud deben venir juntas.' END,
                                CASE WHEN e.persona_id IS NULL
                                     THEN 'El conductor no existe.' END,
                                CASE WHEN e.persona_id IS NOT NULL
                                          AND e.tipo_persona <> 'CONDUCTOR'
                                     THEN 'La persona no es CONDUCTOR.' END,
                                CASE WHEN e.persona_id IS NOT NULL
                                          AND (e.licencia_conduccion IS NULL
                                               OR e.fecha_vigencia_licencia IS NULL
                                               OR e.fecha_vigencia_licencia <= now())
                                     THEN 'El conductor no tiene licencia vigente.' END,
                                CASE WHEN e.vehiculo_id IS NULL
                                     THEN 'El vehículo no existe.' END,
                                CASE WHEN e.vehiculo_id IS NOT NULL
                                          AND (e.asociacion_id IS NULL OR e.asociacion_estado <> 'PO')
                                     THEN 'El conductor no está habilitado para el vehículo.' END,
                                CASE WHEN e.vehiculo_id IS NOT NULL
                                          AND (e.total_documentos = 0 OR e.documentos_invalidos > 0)
                                     THEN 'El vehículo no tiene todos sus documentos habilitados y vigentes.' END,
                                CASE WHEN e.logins = 0
                                     THEN 'El login del usuario no existe.' END,
                                CASE WHEN e.total_ruta > 7
                                     THEN 'La ruta supera el máximo de 7 paradas.' END,
                                CASE WHEN e.conductores_ruta > 1
                                     THEN 'La ruta tiene conductores distintos.' END,
                                CASE WHEN e.vehiculos_ruta > 1
                                     THEN 'La ruta tiene vehículos distintos.' END,
                                CASE WHEN e.logins_ruta > 1
                                     THEN 'La ruta tiene logins distintos.' END,
                                CASE WHEN e.orden_repetido > 1
                                     THEN 'La ruta repite el orden de una parada.' END,
                                CASE WHEN e.iniciales_ruta = 0
                                     THEN 'La ruta no incluye la parada inicial (orden 0).' END,
                                CASE WHEN EXISTS (
                                        SELECT 1
                                          FROM rutas r
                                         WHERE r.codigo_ruta = e.codigo_norm
                                           AND (r.id_persona <> e.persona_id
                                                OR r.id_vehiculo <> e.vehiculo_id
                                                OR r.login_usuario <> e.login_norm))
                                     THEN 'La ruta ya existe con un conductor o vehículo distinto.' END
                            ), '') AS motivo
                        FROM evaluadas e
                    ) e
                    WHERE t.id = e.id;

                    SELECT
                        count(*) FILTER (WHERE estado = 'VALIDADO')::integer,
                        count(*) FILTER (WHERE estado = 'ERROR')::integer
                    INTO v_validados, v_errores
                    FROM trayectos_tmp
                    WHERE id_cargue = p_id_cargue;

                    RETURN QUERY SELECT v_validados, v_errores;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION sp_trayecto_procesar(p_id_cargue bigint)
                RETURNS TABLE (procesados integer, rechazados integer)
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    v_procesados integer;
                    v_rechazados integer := 0;
                BEGIN
                    INSERT INTO rutas (
                        id,
                        codigo_ruta,
                        id_persona,
                        id_vehiculo,
                        login_usuario,
                        created_at_utc)
                    SELECT
                        gen_random_uuid(),
                        upper(btrim(t.codigo_ruta)),
                        t.id_persona,
                        t.id_vehiculo,
                        btrim(t.login_usuario),
                        now()
                    FROM trayectos_tmp t
                    WHERE t.id_cargue = p_id_cargue
                      AND t.estado = 'VALIDADO'
                    GROUP BY
                        upper(btrim(t.codigo_ruta)),
                        t.id_persona,
                        t.id_vehiculo,
                        btrim(t.login_usuario)
                    ON CONFLICT (codigo_ruta) DO NOTHING;

                    INSERT INTO paradas (
                        id,
                        id_ruta,
                        ubicacion,
                        orden_parada,
                        latitud,
                        longitud,
                        created_at_utc)
                    SELECT
                        gen_random_uuid(),
                        r.id,
                        btrim(t.ubicacion),
                        t.orden_parada::integer,
                        CASE WHEN t.latitud IS NULL OR btrim(t.latitud) = ''
                             THEN NULL
                             ELSE t.latitud::double precision END,
                        CASE WHEN t.longitud IS NULL OR btrim(t.longitud) = ''
                             THEN NULL
                             ELSE t.longitud::double precision END,
                        now()
                    FROM trayectos_tmp t
                    JOIN rutas r ON r.codigo_ruta = upper(btrim(t.codigo_ruta))
                    WHERE t.id_cargue = p_id_cargue
                      AND t.estado = 'VALIDADO'
                    ON CONFLICT (id_ruta, orden_parada) DO NOTHING;

                    UPDATE trayectos_tmp
                    SET estado = 'PROCESADO',
                        updated_at_utc = now()
                    WHERE id_cargue = p_id_cargue
                      AND estado = 'VALIDADO';

                    GET DIAGNOSTICS v_procesados = ROW_COUNT;

                    RETURN QUERY SELECT v_procesados, v_rechazados;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sp_trayecto_procesar(bigint);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sp_trayecto_validar(bigint);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sp_trayecto_cargue(bigint, jsonb);");
        }
    }
}
