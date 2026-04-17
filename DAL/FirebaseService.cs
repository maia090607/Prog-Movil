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
            _client = new FirebaseClient("https://smartdrop-db-27a5a-default-rtdb.firebaseio.com/");
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