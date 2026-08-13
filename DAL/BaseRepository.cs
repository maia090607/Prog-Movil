using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public abstract class BaseRepository : IDisposable
    {
        protected const string RAIZ = "sistema_riego";
        protected const string SECUENCIAS = "secuencias";
        protected readonly FirebaseClient _client;

        public BaseRepository()
        {
            var url = FirebaseSettings.DatabaseUrl;
            var token = FirebaseSettings.AuthToken;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _client = new FirebaseClient(url, new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(token)
                });
            }
            else
            {
                _client = new FirebaseClient(url);
            }
        }

        // ================================================================
        // HELPERS DE SINCRONIZACIÓN (mantienen firmas síncronas del DAL)
        // ================================================================

        protected TResult Ejecutar<TResult>(Func<Task<TResult>> accion)
        {
            return accion().GetAwaiter().GetResult();
        }

        protected void Ejecutar(Func<Task> accion)
        {
            accion().GetAwaiter().GetResult();
        }

        // ================================================================
        // ACCESO A NODOS
        // ================================================================

        protected FirebaseQuery Nodo(string tabla)
        {
            return _client.Child(RAIZ).Child(tabla);
        }

        protected FirebaseQuery Nodo(string tabla, object id)
        {
            return _client.Child(RAIZ).Child(tabla).Child(id.ToString());
        }

        // ================================================================
        // SECUENCIAS NUMÉRICAS (IDs compatibles con el modelo actual)
        // ================================================================

        protected int ObtenerSiguienteId(string tabla)
        {
            return Ejecutar(() => ObtenerSiguienteIdAsync(tabla));
        }

        private async Task<int> ObtenerSiguienteIdAsync(string tabla)
        {
            var refSecuencia = _client.Child(RAIZ).Child(SECUENCIAS).Child(tabla);
            int actual = 0;

            try
            {
                var snapshot = await refSecuencia.OnceSingleAsync<int>();
                actual = snapshot;
            }
            catch { actual = 0; }

            int siguiente = actual + 1;
            await refSecuencia.PutAsync(siguiente);
            return siguiente;
        }

        // ================================================================
        // OPERACIONES GENÉRICAS
        // ================================================================

        protected void Guardar(string tabla, object id, object entidad)
        {
            Ejecutar(() => Nodo(tabla, id).PutAsync(entidad));
        }

        protected T ObtenerPorId<T>(string tabla, object id) where T : class, new()
        {
            try
            {
                return Ejecutar(() => Nodo(tabla, id).OnceSingleAsync<T>());
            }
            catch { return null; }
        }

        protected List<T> ObtenerTodos<T>(string tabla) where T : class, new()
        {
            try
            {
                var resultado = Ejecutar(() => Nodo(tabla).OnceAsync<T>());
                return resultado.Select(x => x.Object).ToList();
            }
            catch { return new List<T>(); }
        }

        protected bool Eliminar(string tabla, object id)
        {
            try
            {
                Ejecutar(() => Nodo(tabla, id).DeleteAsync());
                return true;
            }
            catch { return false; }
        }

        public void Dispose() { }
    }
}