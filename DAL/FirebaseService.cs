using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;

namespace DAL
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;

        public FirebaseService()
        {
            // Reemplaza con la URL exacta de tu Realtime Database
            var url = FirebaseSettings.DatabaseUrl;
            var token = FirebaseSettings.AuthToken;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _client = new FirebaseClient(url, new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => System.Threading.Tasks.Task.FromResult(token)
                });
            }
            else
            {
                _client = new FirebaseClient(url);
            }
        }

        public async Task ActualizarSensores(double humedad, double temperatura)
        {
            try
            {
                await _client
                  .Child("sistema_riego")
                  .Child("monitoreo")
                  .PutAsync(new
                  {
                      humedad = humedad,
                      temperatura = temperatura,
                      ultima_actualizacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                  });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}