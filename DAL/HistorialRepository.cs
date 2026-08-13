using ENTITY;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL
{
    public class HistorialRepository : BaseRepository
    {
        private const string TABLA = "historial_riego";

        public Response<Historial_Riego> Insertar(Historial_Riego historial)
        {
            try
            {
                if (historial.Id <= 0)
                    historial.Id = ObtenerSiguienteId(TABLA);

                if (string.IsNullOrEmpty(historial.TipoRiego))
                    historial.TipoRiego = "Manual";

                EnriquecerNombres(historial);

                Guardar(TABLA, historial.Id, historial);
                return new Response<Historial_Riego>(true, "Historial registrado correctamente", historial, null);
            }
            catch (Exception ex)
            {
                return new Response<Historial_Riego>(false, "Error DAL: " + ex.Message, null, null);
            }
        }

        public Response<List<Historial_Riego>> MostrarTodos()
        {
            try
            {
                var lista = ObtenerTodos<Historial_Riego>(TABLA)
                    .OrderByDescending(h => h.Fecha)
                    .ToList();

                foreach (var h in lista)
                {
                    if (string.IsNullOrEmpty(h.NombrePlanta) ||
                        string.IsNullOrEmpty(h.NombrePropietario))
                    {
                        EnriquecerNombres(h);
                    }
                }

                return new Response<List<Historial_Riego>>(true, "Ok", lista, null);
            }
            catch (Exception ex)
            {
                return new Response<List<Historial_Riego>>(false, ex.Message, null, null);
            }
        }

        public Response<Historial_Riego> BuscarPorId(int id)
        {
            try
            {
                var historial = ObtenerPorId<Historial_Riego>(TABLA, id);
                if (historial == null)
                    return new Response<Historial_Riego>(false, "No implementado", null, null);

                EnriquecerNombres(historial);
                return new Response<Historial_Riego>(true, "Historial encontrado", historial, null);
            }
            catch (Exception ex)
            {
                return new Response<Historial_Riego>(false, ex.Message, null, null);
            }
        }

        private void EnriquecerNombres(Historial_Riego historial)
        {
            var planta = historial.IdPlanta > 0
                ? ObtenerPorId<Cultivo>("plantas", historial.IdPlanta)
                : null;

            if (planta != null)
            {
                historial.NombrePlanta = planta.NombrePlanta ?? "Desconocida";

                var usuario = planta.IdUsuario > 0
                    ? ObtenerPorId<Usuario>("usuarios", planta.IdUsuario)
                    : null;

                historial.NombrePropietario = usuario != null && !string.IsNullOrEmpty(usuario.NombreUsuario)
                    ? usuario.NombreUsuario
                    : "Sin Asignar";
            }
            else
            {
                historial.NombrePlanta = historial.NombrePlanta ?? "Desconocida";
                historial.NombrePropietario = historial.NombrePropietario ?? "Sin Asignar";
            }
        }
    }
}