using ENTITY;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL
{
    public class AlertasRepository : BaseRepository
    {
        private const string TABLA = "alertas";

        public Response<Alertas> Agregar(Alertas entidad)
        {
            try
            {
                if (entidad.IdAlerta <= 0)
                    entidad.IdAlerta = ObtenerSiguienteId(TABLA);

                Guardar(TABLA, entidad.IdAlerta, entidad);
                return new Response<Alertas>(true, "Alerta registrada correctamente", entidad, null);
            }
            catch (Exception ex)
            {
                return new Response<Alertas>(false, "Error DAL: " + ex.Message, null, null);
            }
        }

        public Response<Alertas> MostrarTodos()
        {
            try
            {
                var lista = ObtenerTodos<Alertas>(TABLA)
                    .OrderByDescending(a => a.FechaHora)
                    .ToList();

                return new Response<Alertas>(true, $"Se encontraron {lista.Count} alertas.", null, lista);
            }
            catch (Exception ex)
            {
                return new Response<Alertas>(false, "Error al listar: " + ex.Message, null, null);
            }
        }

        public Response<Alertas> Actualizar(Alertas entidad)
        {
            try
            {
                if (entidad.IdAlerta <= 0)
                    return new Response<Alertas>(false, "El ID de alerta no es válido", null, null);

                var existente = ObtenerPorId<Alertas>(TABLA, entidad.IdAlerta);
                if (existente == null)
                    return new Response<Alertas>(false, "Alerta no encontrada", null, null);

                Guardar(TABLA, entidad.IdAlerta, entidad);
                return new Response<Alertas>(true, "Alerta actualizada correctamente", entidad, null);
            }
            catch (Exception ex)
            {
                return new Response<Alertas>(false, "Error al actualizar: " + ex.Message, null, null);
            }
        }

        public Response<Alertas> Eliminar(int id)
        {
            try
            {
                bool exito = Eliminar(TABLA, id);
                return exito
                    ? new Response<Alertas>(true, "Alerta eliminada correctamente", null, null)
                    : new Response<Alertas>(false, "Alerta no encontrada", null, null);
            }
            catch (Exception ex)
            {
                return new Response<Alertas>(false, "Error al eliminar: " + ex.Message, null, null);
            }
        }

        public Response<Alertas> ObtenerPorId(int id)
        {
            try
            {
                var alerta = ObtenerPorId<Alertas>(TABLA, id);
                if (alerta == null)
                    return new Response<Alertas>(false, "Alerta no encontrada", null, null);

                return new Response<Alertas>(true, "Alerta encontrada", alerta, null);
            }
            catch (Exception ex)
            {
                return new Response<Alertas>(false, "Error al buscar: " + ex.Message, null, null);
            }
        }
    }
}