using ENTITY;
using System;
using System.Collections.Generic;

namespace DAL
{
    public class TemperaturaRepository : BaseRepository
    {
        private const string TABLA = "temperatura";

        public Response<Temperatura> Insertar(Temperatura entidad)
        {
            try
            {
                if (entidad.IdTemperatura <= 0)
                    entidad.IdTemperatura = ObtenerSiguienteId(TABLA);

                Guardar(TABLA, entidad.IdTemperatura, entidad);
                return new Response<Temperatura>(true, "Temperatura registrada correctamente", entidad, null);
            }
            catch (Exception ex)
            {
                return new Response<Temperatura>(false, "Error DAL: " + ex.Message, null, null);
            }
        }
    }
}