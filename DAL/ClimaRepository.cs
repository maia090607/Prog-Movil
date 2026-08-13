using Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL
{
    public class RepositorioClima : BaseRepository
    {
        private const string TABLA = "registro_climatico";

        public Response<RegistroClimatico> Insertar(RegistroClimatico registro)
        {
            try
            {
                if (registro.IdRegistro <= 0)
                    registro.IdRegistro = ObtenerSiguienteId(TABLA);

                Guardar(TABLA, registro.IdRegistro, registro);
                return new Response<RegistroClimatico>(true, "Registro climático guardado correctamente", registro, null);
            }
            catch (Exception ex)
            {
                return new Response<RegistroClimatico>(false, $"Error: {ex.Message}", null, null);
            }
        }

        public Response<RegistroClimatico> MostrarTodos()
        {
            try
            {
                var lista = ObtenerTodos<RegistroClimatico>(TABLA)
                    .OrderByDescending(r => r.Fecha)
                    .ToList();

                return new Response<RegistroClimatico>(true, $"Se encontraron {lista.Count} registros.", null, lista);
            }
            catch (Exception ex)
            {
                return new Response<RegistroClimatico>(false, $"Error general: {ex.Message}", null, null);
            }
        }
    }
}