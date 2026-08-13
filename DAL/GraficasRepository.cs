using ENTITY;
using System;
using System.Collections.Generic;

namespace DAL
{
    public class GraficasRepository : BaseRepository
    {
        private const string TABLA = "graficas";

        public Response<List<Main>> MostrarTodos()
        {
            try
            {
                var lista = ObtenerTodos<Main>(TABLA);
                return new Response<List<Main>>(true, "Datos de gráficas obtenidos correctamente", lista, null);
            }
            catch (Exception ex)
            {
                return new Response<List<Main>>(false, $"Error general: {ex.Message}", null, null);
            }
        }

        public Response<Main> ObtenerPorId(int id)
        {
            try
            {
                var grafica = ObtenerPorId<Main>(TABLA, id);
                if (grafica == null)
                    return new Response<Main>(false, "No se encontró la gráfica con el ID especificado", null, null);

                return new Response<Main>(true, "Gráfica encontrada correctamente", grafica, null);
            }
            catch (Exception ex)
            {
                return new Response<Main>(false, $"Error general: {ex.Message}", null, null);
            }
        }

        public Response<Main> Guardar(Main entidad)
        {
            try
            {
                int id = ObtenerSiguienteId(TABLA);
                Guardar(TABLA, id, entidad);
                return new Response<Main>(true, "Datos de gráfica guardados correctamente", entidad, null);
            }
            catch (Exception ex)
            {
                return new Response<Main>(false, $"Error general: {ex.Message}", null, null);
            }
        }
    }
}