using ENTITY;
using System;
using System.Collections.Generic;

namespace DAL
{
    public class HumedadRepository : BaseRepository
    {
        private const string TABLA = "humedad";

        public Response<humedad> Insertar(humedad registro)
        {
            try
            {
                if (registro.IdHumedad <= 0)
                    registro.IdHumedad = ObtenerSiguienteId(TABLA);

                Guardar(TABLA, registro.IdHumedad, registro);
                return new Response<humedad>(true, "Registro de humedad guardado correctamente", registro, null);
            }
            catch (Exception ex)
            {
                return new Response<humedad>(false, $"Error general: {ex.Message}", null, null);
            }
        }

        public Response<humedad> MostrarTodo()
        {
            try
            {
                var lista = ObtenerTodos<humedad>(TABLA);
                return new Response<humedad>(true, "Registros obtenidos correctamente.", null, lista);
            }
            catch (Exception ex)
            {
                return new Response<humedad>(false, $"Error general: {ex.Message}", null, null);
            }
        }
    }
}