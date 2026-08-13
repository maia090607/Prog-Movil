using Firebase.Database;
using Microsoft.AspNetCore.Mvc;

namespace RiegoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConnDbController : ControllerBase
    {
        private const string RAIZ = "sistema_riego";

        [HttpGet("conn")]
        public IActionResult ProbarConexion([FromServices] IConfiguration config)
        {
            string url = config["Firebase:DatabaseUrl"] ?? "https://smartdrop-60e34-default-rtdb.firebaseio.com/";

            try
            {
                using var client = new FirebaseClient(url);
                // Lectura mínima para validar conectividad con la Realtime Database
                var res = client.Child(RAIZ).OnceAsync<object>().GetAwaiter().GetResult();
                return Ok("conexion exitosa a la base de datos firebase");
            }
            catch (Exception ex)
            {
                return BadRequest("error en la conexion: " + ex.Message);
            }
        }
    }
}