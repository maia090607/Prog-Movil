using ENTITY;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL
{
    public class PlantaRepository : BaseRepository, IRepository<Cultivo>
    {
        private const string TABLA = "plantas";

        public Response<Cultivo> Insertar(Cultivo planta)
        {
            try
            {
                if (planta.IdPlanta <= 0)
                    planta.IdPlanta = ObtenerSiguienteId(TABLA);

                planta.NombrePropietario = ObtenerNombrePropietario(planta.IdUsuario);

                Guardar(TABLA, planta.IdPlanta, planta);
                return new Response<Cultivo>(true, "Planta registrada correctamente", planta, null);
            }
            catch (Exception ex)
            {
                return new Response<Cultivo>(false, $"Error: {ex.Message}", null, null);
            }
        }

        public Response<Cultivo> Actualizar(Cultivo planta)
        {
            try
            {
                if (planta.IdPlanta <= 0)
                    return new Response<Cultivo>(false, "El ID de planta no es válido", null, null);

                var existente = ObtenerPorId<Cultivo>(TABLA, planta.IdPlanta);
                if (existente == null)
                    return new Response<Cultivo>(false, "Planta no encontrada", null, null);

                planta.NombrePropietario = ObtenerNombrePropietario(planta.IdUsuario);

                Guardar(TABLA, planta.IdPlanta, planta);
                return new Response<Cultivo>(true, "Planta actualizada correctamente", planta, null);
            }
            catch (Exception ex)
            {
                return new Response<Cultivo>(false, $"Error: {ex.Message}", null, null);
            }
        }

        public Response<Cultivo> Eliminar(int id)
        {
            try
            {
                bool exito = Eliminar(TABLA, id);
                return exito
                    ? new Response<Cultivo>(true, "Planta eliminada correctamente", null, null)
                    : new Response<Cultivo>(false, "Planta no encontrada", null, null);
            }
            catch (Exception ex)
            {
                return new Response<Cultivo>(false, $"Error: {ex.Message}", null, null);
            }
        }

        public Response<Cultivo> ObtenerPorId(int id)
        {
            try
            {
                var planta = ObtenerPorId<Cultivo>(TABLA, id);
                if (planta == null)
                    return new Response<Cultivo>(false, "Planta no encontrada", null, null);

                planta.NombrePropietario = ObtenerNombrePropietario(planta.IdUsuario);
                return new Response<Cultivo>(true, "Planta encontrada", planta, null);
            }
            catch (Exception ex)
            {
                return new Response<Cultivo>(false, $"Error: {ex.Message}", null, null);
            }
        }

        public Response<Cultivo> ObtenerTodos()
        {
            try
            {
                var lista = ObtenerTodos<Cultivo>(TABLA);
                foreach (var planta in lista)
                {
                    if (string.IsNullOrEmpty(planta.NombrePropietario))
                        planta.NombrePropietario = ObtenerNombrePropietario(planta.IdUsuario);
                }

                return new Response<Cultivo>(true, $"Encontradas {lista.Count}", null, lista);
            }
            catch (Exception ex)
            {
                return new Response<Cultivo>(false, $"Error: {ex.Message}", null, null);
            }
        }

        public Response<Cultivo> ObtenerPorUsuario(int idUsuario)
        {
            try
            {
                var lista = ObtenerTodos<Cultivo>(TABLA)
                    .Where(p => p.IdUsuario == idUsuario)
                    .ToList();

                return new Response<Cultivo>(true, $"Encontradas {lista.Count}", null, lista);
            }
            catch (Exception ex)
            {
                return new Response<Cultivo>(false, ex.Message, null, null);
            }
        }

        private string ObtenerNombrePropietario(int idUsuario)
        {
            if (idUsuario <= 0) return "Sin Asignar";

            var usuario = ObtenerPorId<Usuario>("usuarios", idUsuario);
            return usuario != null && !string.IsNullOrEmpty(usuario.NombreUsuario)
                ? usuario.NombreUsuario
                : "Sin Asignar";
        }
    }
}