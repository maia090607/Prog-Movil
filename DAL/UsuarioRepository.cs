using ENTITY;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL
{
    public class UsuarioRepository : BaseRepository
    {
        private const string TABLA = "usuarios";

        public Response<Usuario> Insertar(Usuario usuario)
        {
            try
            {
                if (usuario.IdUsuario <= 0)
                    usuario.IdUsuario = ObtenerSiguienteId(TABLA);

                Guardar(TABLA, usuario.IdUsuario, usuario);
                return new Response<Usuario>(true, "Usuario registrado correctamente", usuario, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> Actualizar(Usuario usuario)
        {
            try
            {
                if (usuario.IdUsuario <= 0)
                    return new Response<Usuario>(false, "El ID de usuario no es válido", null, null);

                var existente = ObtenerPorId<Usuario>(TABLA, usuario.IdUsuario);
                if (existente == null)
                    return new Response<Usuario>(false, "Usuario no encontrado", null, null);

                Guardar(TABLA, usuario.IdUsuario, usuario);
                return new Response<Usuario>(true, "Usuario actualizado correctamente", usuario, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> BuscarPorId(int id)
        {
            try
            {
                var usuario = ObtenerPorId<Usuario>(TABLA, id);
                if (usuario == null)
                    return new Response<Usuario>(false, "Usuario no encontrado", null, null);

                return new Response<Usuario>(true, "Usuario encontrado", usuario, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> ObtenerTodos()
        {
            try
            {
                var lista = ObtenerTodos<Usuario>(TABLA);
                return new Response<Usuario>(true, $"Usuarios listados ({lista.Count})", null, lista);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> Eliminar(int id)
        {
            try
            {
                bool exito = Eliminar(TABLA, id);
                return exito
                    ? new Response<Usuario>(true, "Usuario eliminado correctamente", null, null)
                    : new Response<Usuario>(false, "Usuario no encontrado", null, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> BuscarPorUsuario(string nombreUsuario)
        {
            try
            {
                var lista = ObtenerTodos<Usuario>(TABLA);
                var usuario = lista.FirstOrDefault(u =>
                    string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.Ordinal));

                if (usuario == null)
                    return new Response<Usuario>(false, "No encontrado", null, null);

                return new Response<Usuario>(true, "Encontrado", usuario, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> BuscarPorCredenciales(string usuarioNombre, string pass)
        {
            try
            {
                var lista = ObtenerTodos<Usuario>(TABLA);
                var usuario = lista.FirstOrDefault(u =>
                    string.Equals(u.NombreUsuario, usuarioNombre, StringComparison.Ordinal) &&
                    string.Equals(u.Password, pass, StringComparison.Ordinal));

                if (usuario == null)
                    return new Response<Usuario>(false, "Credenciales incorrectas", null, null);

                return new Response<Usuario>(true, "Login correcto", usuario, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }

        public Response<Usuario> BuscarPorTelefono(string telefono)
        {
            try
            {
                var lista = ObtenerTodos<Usuario>(TABLA);
                var usuario = lista.FirstOrDefault(u =>
                    string.Equals(u.Telefono, telefono, StringComparison.Ordinal));

                if (usuario == null)
                    return new Response<Usuario>(false, "Usuario no encontrado por teléfono", null, null);

                return new Response<Usuario>(true, "Usuario encontrado por teléfono", usuario, null);
            }
            catch (Exception ex)
            {
                return new Response<Usuario>(false, ex.Message, null, null);
            }
        }
    }
}