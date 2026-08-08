using BLL;
using Entity;
using ENTITY;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PROYECTO_RIEGO_AUTOMATICO
{
    public partial class MenuCuidador : Form
    {
        ServiciosPlanta serviciosPlanta;
        ServicioHistorial servicioHistorial;
        ServiciosUsuario serviciosUsuario;
        ServiciosAlertas serviciosAlertas;
        ServicioClima servicioClima;
        ServiciosHumedad serviciosHumedad;
        private ServicioPuerto _servicioPuerto;
        private float humedad_actual, temperatura_actual, viento_actual, humedad_suelo, humedad_real;
        private bool puedeRegar = true, Expandir = false, bombaAnteriorEncendida = false;
        private int IdDelUsuario, contadorIntentos=3;
        private Size originalFormSize;

        public MenuCuidador()
        {
            serviciosPlanta = new ServiciosPlanta();
            servicioHistorial = new ServicioHistorial();
            serviciosUsuario = new ServiciosUsuario();
            serviciosAlertas = new ServiciosAlertas();
            servicioClima = new ServicioClima();
            serviciosHumedad = new ServiciosHumedad();
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            originalFormSize = this.Size;
            cargarPlantas();
            _servicioPuerto = new ServicioPuerto("COM3", 9600);
            _servicioPuerto.DatosRecibidos += MostrarDatos;
            _ = ObtenerDatosClimaAsync();

        }
        private async void MostrarDatos(DatosArduinoModel datos)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            if (datos == null)
                return;

            float humedad = datos.Humedad;
            bool bombaEncendida = datos.BombaEncendida;
            humedad_real = humedad;

                    // Mostrar en interfaz
                    Invoke(new Action(() =>
                    {
                        lblHumedad.Text = humedad.ToString("F2");
                        lbEstadodeBomba.Text = bombaEncendida ? "ENCENDIDA" : "APAGADA";
                        lbEstadodeBomba.BackColor = bombaEncendida ? ColorTranslator.FromHtml("#21864B") : ColorTranslator.FromHtml("#8B0000");

                        // ✅ **CAMBIO CRÍTICO**: Registrar cada vez que la bomba se ENCIENDA
                        // Sin importar si ya estaba encendida antes
                        if (bombaEncendida && !bombaAnteriorEncendida)
                        {
                            // ✅ Nueva activación de riego
                            Historial_Riego historial = new Historial_Riego
                            {
                                Temperatura = temperatura_actual,
                                Humedad = humedad,
                                Fecha = DateTime.Now
                            };

                            try
                            {
                                servicioHistorial.Guardar(historial);
                                lbUltimoRegado.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                                Console.WriteLine($"✅ [HISTORIAL] Riego automático registrado - H:{humedad}%, T:{temperatura_actual}°C");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ [HISTORIAL] Error al guardar: {ex.Message}");
                            }
                        }
                        else if (!bombaEncendida && bombaAnteriorEncendida)
                        {
                            Console.WriteLine($"🛑 [HISTORIAL] Riego automático finalizado");
                        }

                        // Actualizar estado anterior
                        bombaAnteriorEncendida = bombaEncendida;
                    }));

                    // Guardar humedad en base de datos
                    var hum = new humedad
                    {
                        ValorHumedad = humedad,
                        FechaRegistro = DateTime.Now
                    };
                    serviciosHumedad.insertar(hum);
        }
        private void EnviarComandoSeguro(string comando)
        {
            if (_servicioPuerto != null && _servicioPuerto.PuertoAbierto)
                _servicioPuerto.EnviarComando(comando);
            else
                MessageBox.Show("El puerto serial no está disponible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private int NuevoId()
        {
            var todas = serviciosAlertas.MostrarTodos();

            int nuevoId = 1;
            if (todas.Lista != null && todas.Lista.Count > 0)
                nuevoId = todas.Lista.Max(a => a.IdAlerta) + 1;
            return nuevoId;
        }
        public async Task ObtenerDatosClimaAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            string apiKey = "91c59362a4519b067f3be52b6fe361f3";
            string ciudad = "Valledupar";
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={ciudad}&appid={apiKey}&units=metric&lang=es";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var weatherInfo = JsonSerializer.Deserialize<ENTITY.WeatherInfo>(responseBody);

                    // Mostrar en labels
                    lbTemperatura.Text = $"{weatherInfo.main.temp}°C";
                    lbHum.Text = $"{weatherInfo.main.humidity} %";
                    lbVie.Text = $"{weatherInfo.wind.speed} M/S";
                    lbDes.Text = $"{weatherInfo.weather[0].description.ToUpper()}";

                    // Variables
                    temperatura_actual = (float)weatherInfo.main.temp;
                    humedad_actual = (float)weatherInfo.main.humidity;
                    viento_actual = (float)weatherInfo.wind.speed;
                    humedad_suelo = float.Parse(lblHumedad.Text);


                    ActualizarGraficoClima(temperatura_actual, humedad_actual, viento_actual);

                    // 🔹 Verificar si ya existe un registro reciente similar (últimos 5 min)
                    var registrosExistentes = servicioClima.MostrarTodos().Lista;
                    var haceCincoMin = DateTime.Now.AddMinutes(-5);

                    // 🔹 Filtrar registros recientes
                    var registrosRecientes = registrosExistentes
                        .Where(r => r.Fecha >= haceCincoMin)
                        .OrderByDescending(r => r.Fecha)
                        .ToList();

                    // 🔹 Redondear valores actuales
                    float tempActual = (float)Math.Round(temperatura_actual, 2);
                    float humSueloActual = (float)Math.Round(humedad_suelo, 2);
                    float humAmbienteActual = (float)Math.Round(humedad_actual, 2);
                    float vientoActual = (float)Math.Round(viento_actual, 2);

                    // 🔍 Buscar si ya existe un registro igual
                    bool existeIgual = registrosRecientes.Any(r =>
                        Math.Round(r.Temperatura_Ambiente, 2) == tempActual &&
                        Math.Round(r.Humedad_Suelo, 2) == humSueloActual &&
                        Math.Round(r.Humedad_Ambiente, 2) == humAmbienteActual &&
                        Math.Round(r.Viento, 2) == vientoActual
                    );

                    if (!existeIgual)
                    {
                        RegistroClimatico clima = new RegistroClimatico
                        {
                            Humedad_Ambiente = humAmbienteActual,
                            Humedad_Suelo = humSueloActual,
                            Temperatura_Ambiente = tempActual,
                            Viento = vientoActual,
                            Fecha = DateTime.Now
                        };
                        servicioClima.Guardar(clima);
                    }

                    // 🔹 Obtener todas las alertas actuales
                    var alertasExistentes = serviciosAlertas.MostrarTodos().Lista;

                    // === VERIFICAR Y CREAR ALERTAS ===
                    void CrearAlertaSiNoExiste(string tipo, string descripcion, string nivel)
                    {
                        bool existeActiva = alertasExistentes.Any(a =>
                            a.TipoAlerta.Equals(tipo, StringComparison.OrdinalIgnoreCase) &&
                            a.Estado == false); // solo si hay una alerta igual activa

                        if (!existeActiva)
                        {
                            Alertas alerta = new Alertas
                            {
                                IdAlerta = NuevoId(),
                                FechaHora = DateTime.Now,
                                TipoAlerta = tipo,
                                Descripcion = descripcion,
                                NivelCritico = nivel,
                                Estado = false
                            };
                            serviciosAlertas.Agregar(alerta);
                            MessageBox.Show($"Alerta: {tipo}\nDescripción: {descripcion}\nNivel Crítico: {nivel}",
                                            "Alerta Climática", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    var cultivos = serviciosPlanta.ObtenerTodos();
                    foreach (var c in cultivos.Lista)
                    {
                        if (c.nivel_optimo_temperatura < weatherInfo.main.temp)
                        {
                            CrearAlertaSiNoExiste("Temperatura no Optima",
                            $"La temperatura actual es de {weatherInfo.main.temp}°C, y esta sobrepasa lo optimo para la planta {c.NombrePlanta} con id {c.IdPlanta}.",
                            "Medio");
                        }
                        if (c.nivel_optimo_humedad < humedad_real)
                        {
                            CrearAlertaSiNoExiste("Humedad no Optima",
                            $"La humedad actual es de {humedad_real}°C, y esta sobrepasa lo optimo para la planta {c.NombrePlanta} con id {c.IdPlanta}.",
                            "Medio");
                        }
                    }
                    if (weatherInfo.main.temp > 35)
                        CrearAlertaSiNoExiste("Alta Temperatura",
                            $"La temperatura actual es de {weatherInfo.main.temp}°C, lo cual supera el umbral seguro.",
                            "Alto");

                    if (weatherInfo.main.humidity < 20)
                        CrearAlertaSiNoExiste("Baja Humedad",
                            $"La humedad actual es de {weatherInfo.main.humidity}%, lo cual está por debajo del umbral seguro.",
                            "Medio");

                    if (weatherInfo.wind.speed > 10)
                        CrearAlertaSiNoExiste("Viento Fuerte",
                            $"La velocidad del viento es de {weatherInfo.wind.speed} m/s, lo cual supera el umbral seguro.",
                            "Bajo");

                    if (weatherInfo.weather[0].description.Contains("lluvia"))
                        CrearAlertaSiNoExiste("Lluvia",
                            "Se ha detectado lluvia en la zona, lo cual puede afectar el riego automático.",
                            "Bajo");

                    if (weatherInfo.weather[0].description.Contains("tormenta"))
                        CrearAlertaSiNoExiste("Tormenta",
                            "Se ha detectado una tormenta en la zona, lo cual puede afectar el riego automático.",
                            "Alto");
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error al obtener datos del clima: {e.Message}");
                    Alertas alerta = new Alertas
                    {
                        IdAlerta = NuevoId(),
                        FechaHora = DateTime.Now,
                        TipoAlerta = "Error Clima",
                        Descripcion = $"No se pudieron obtener los datos climáticos: {e.Message}",
                        NivelCritico = "Medio",
                        Estado = false
                    };
                    serviciosAlertas.Agregar(alerta);
                }
            }
        }


        private void ActualizarGraficoClima(float temp, float hum, float vie)
        {
            try
            {

                chartClima.Series.Clear();
                chartClima.Titles.Clear();

                Series serie = new Series("DatosClimáticos");
                serie.ChartType = SeriesChartType.Doughnut;
                serie.IsValueShownAsLabel = true;

                serie.Points.AddXY("Humedad (%)", Math.Round(hum, 0));
                serie.Points.AddXY("Viento (m/s)", Math.Round(vie, 2));
                serie.Points.AddXY("Temperatura (°C)", Math.Round(temp, 0));

                chartClima.Series.Add(serie);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar gráfico: {ex.Message}");
            }
        }
        private void ActualizarGraficoCultivo(float nivelHumedad, float nivelTemperatura, float nivelLuz)
        {
            try
            {

                chartCultivo.Series.Clear();
                chartCultivo.Titles.Clear();

                Series serie = new Series("NivelesÓptimos");
                serie.ChartType = SeriesChartType.Doughnut;
                serie.IsValueShownAsLabel = true;

                serie.Points.AddXY("Humedad óptima (%)", Math.Round(nivelHumedad, 0));
                serie.Points.AddXY("Temperatura óptima (°C)", Math.Round(nivelTemperatura, 0));
                serie.Points.AddXY("Luz óptima (%)", Math.Round(nivelLuz, 0));

                serie.Points[0].Color = Color.DeepSkyBlue;
                serie.Points[1].Color = Color.OrangeRed;
                serie.Points[2].Color = Color.Gold;

                chartCultivo.Titles.Add("Niveles Óptimos del Cultivo");

                chartCultivo.Series.Add(serie);

                chartCultivo.BackColor = Color.Transparent;
                chartCultivo.ChartAreas[0].BackColor = Color.Transparent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar gráfico de cultivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void cargarPlantas()
        {
            var lista = serviciosPlanta.ObtenerTodos();

            cbPlantas.DataSource = null;
            cbPlantas.Items.Clear();

            cbPlantas.DataSource = lista.Lista;
            cbPlantas.DisplayMember = "NombrePlanta"; // Lo que se muestra en el ComboBox
            cbPlantas.ValueMember = "IdPlanta";       // Valor interno (opcional)
        }
        private void BuscarPlanta()
        {
            if (cbPlantas.SelectedItem is Cultivo cultivo)
            {
                MId.Text = cultivo.IdPlanta.ToString();
                Mnombre.Text = cultivo.NombrePlanta;
                MDescripcion.Text = cultivo.Descripcion;
                Mhumedad.Text = cultivo.nivel_optimo_humedad.ToString("0.00") + "%";
                Mtemperatura.Text = cultivo.nivel_optimo_temperatura.ToString("0.00") + "C°";
                Mluz.Text = cultivo.nivel_optimo_luz.ToString("0.00") + "%";
                ActualizarGraficoCultivo(cultivo.nivel_optimo_humedad, cultivo.nivel_optimo_temperatura, cultivo.nivel_optimo_luz);
                if (!string.IsNullOrEmpty(cultivo.RutaImagen) && File.Exists(cultivo.RutaImagen))
                {
                    pbPlanta.Image = Image.FromFile(cultivo.RutaImagen);
                }
                else
                {
                    pbPlanta.Image = null;
                }
            }
            else
            {
                MessageBox.Show("Seleccione una planta primero.");
            }
        }
        private void ultimoRegado()
        {
            var lista = servicioHistorial.MostrarTodos();
            Historial_Riego ultimo = null;

            foreach (var item in lista)
            {
                ultimo = item;
            }

            if (ultimo != null)
            {
                lbUltimoRegado.Text = ultimo.Fecha.ToString("dd/MM/yyyy HH:mm:ss");
            }
            else
            {
                lbUltimoRegado.Text = "Sin registros";

            }

        }
        private void HacerCambios()
        {
            btnGuardarCambios.Enabled = true;
            btnEliminarUsuario.Enabled = true;
            btnSubirFoto.Enabled = true;
            btnSubirFoto.Enabled = true;
            txtNombreUsuario.Enabled = true;
            txtNombreUsuariodelUsuario.Enabled = true;
            txtEmailUsu.Enabled = true;
            cbRol.Enabled = true;

        }

        private void GraficarHumedad()
        {
            var listaHumedad = serviciosHumedad.MostrarTodos().Lista;
            if (listaHumedad == null || listaHumedad.Count == 0)
                return;

            var datosOrdenados = listaHumedad.OrderBy(d => d.FechaRegistro).ToList();
            var ultimosDatos = datosOrdenados.Skip(Math.Max(0, datosOrdenados.Count - 10)).ToList();

            var serieHum = chartRiego.Series["HUMEDAD DEL SUELO"];
            serieHum.Points.Clear();
            serieHum.ChartType = SeriesChartType.Spline; // 🔄 Línea suave
            serieHum.BorderWidth = 2;
            serieHum.Color = Color.MediumBlue;

            foreach (var item in ultimosDatos)
            {
                double xValue = item.FechaRegistro.ToOADate();
                int yValue = (int)item.ValorHumedad;

                int pointIndex = serieHum.Points.AddXY(xValue, yValue);

                // 🔹 Colorear según el valor
                if (yValue <= 30)
                    serieHum.Points[pointIndex].Color = Color.Red;
                else if (yValue <= 60)
                    serieHum.Points[pointIndex].Color = Color.Orange;
                else
                    serieHum.Points[pointIndex].Color = Color.Green;
            }

            var areaHum = chartRiego.ChartAreas[0];
            areaHum.AxisX.LabelStyle.Format = "HH:mm"; // 🕒 Mostrar hora real
            areaHum.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            areaHum.AxisX.Interval = 2; // Más juntos
            areaHum.AxisX.Title = "Hora";
            areaHum.AxisY.Title = "Humedad del Suelo (%)";
            areaHum.AxisX.LabelStyle.Angle = -45;
            areaHum.AxisX.LabelStyle.Font = new Font("Segoe UI", 9);
            areaHum.AxisX.MajorGrid.LineColor = Color.LightGray;
            areaHum.AxisY.MajorGrid.LineColor = Color.LightGray;
            areaHum.BackColor = Color.White;
            chartRiego.BackColor = Color.WhiteSmoke;

            // 🔹 Línea de referencia para humedad crítica
            areaHum.AxisY.StripLines.Clear(); // Limpiar anteriores
            var lineaCritica = new StripLine
            {
                IntervalOffset = 30,
                BorderColor = Color.Red,
                BorderWidth = 2,
                BorderDashStyle = ChartDashStyle.Dash,
                Text = "Umbral Crítico",
                TextAlignment = StringAlignment.Far,
                TextLineAlignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Red
            };
            areaHum.AxisY.StripLines.Add(lineaCritica);

            // 🔹 Ajuste de rango en eje X
            areaHum.AxisX.Minimum = ultimosDatos.First().FechaRegistro.ToOADate();
            areaHum.AxisX.Maximum = ultimosDatos.Last().FechaRegistro.ToOADate();

            chartRiego.Update();
        }
        private void GuardarCambios()
        {
            DialogResult resultado = MessageBox.Show($"¿Seguro que quiere hacer estos cambios?",
            "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                var usu = serviciosUsuario.BuscarPorId(IdDelUsuario);
                if (serviciosUsuario.BuscarPorId(int.Parse(txtIdUsuario.Text)) == null && serviciosUsuario.ValidarNombreUsuario(txtIdUsuario.Text) == null)
                {
                    MessageBox.Show("El nombre de usuario ya está en uso. Cámbialo por favor.");
                    return;
                }
                if (cbRol.SelectedIndex == -1)
                {
                    MessageBox.Show("Por favor, seleccione un rol.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    if (cbRol.SelectedItem.ToString() == "Dueño")
                    {
                        string men = Interaction.InputBox($"Ingrese la contraseña para ser Dueño:", "Registro obligatorio", "");
                        string contra = "2025";
                        if (men != contra)
                        {
                            MessageBox.Show($"Contraseña incorrecta para el rol de Dueño. tienes {contadorIntentos} intentos..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            contadorIntentos--;
                            if (contadorIntentos == 0)
                            {
                                MessageBox.Show("Ha excedido el número máximo de intentos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                cbRol.Enabled = false;
                            }
                            cbRol.SelectedIndex = 1;
                            return;
                        }

                    }
                }
                if (!txtEmailUsu.Text.Contains("@"))
                {
                    MessageBox.Show("El correo debe contener '@'.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmailUsu.Focus();
                    return;
                }
                if (!ValidarEmail(txtEmailUsu.Text))
                {
                    errorProvider1.SetError(txtEmailUsu, "El correo no tiene un formato válido (ejemplo: usuario@dominio.com)");
                    MessageBox.Show("⚠️ El correo no tiene un formato válido.", "Error de validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!txtNombreUsuario.Text.All(c => char.IsLetter(c) || c == ' '))
                {
                    MessageBox.Show("El nombre solo puede contener letras y espacios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                usu.Entidad.NombreUsuario = txtNombreUsuariodelUsuario.Text;
                usu.Entidad.Nombre = txtNombreUsuario.Text;
                usu.Entidad.Email = txtEmailUsu.Text;
                usu.Entidad.Rol = cbRol.GetItemText(cbRol.SelectedItem);

                var mensaje = serviciosUsuario.Actualizar(usu.Entidad);
                if (mensaje.Entidad != null)
                {
                    MessageBox.Show($"El/La Usuari@ {usu.Entidad.NombreUsuario} fue actualizad@ correctamente");
                    if(usu.Entidad.Rol == "Dueño")
                    {
                        MessageBox.Show("Cambiando a plando de DUEÑO.", "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        MENUPRINCIPAL form1 = new MENUPRINCIPAL();
                        form1.Show();
                        this.Hide();
                    }
                        
                }
                else
                {
                    MessageBox.Show("Hubo un error al guardar los cambios: ");
                }
                CargarUsuario();
            }
            return;
        }
        private bool ValidarEmail(string email)
        {
            string patron = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

            return Regex.IsMatch(email, patron);
        }
        private void CargarUsuario()
        {
            var list = serviciosUsuario.ObtenerTodos();
            foreach (var item in list.Lista)
            {
                if (item.Accedio == true)
                {
                    IdDelUsuario = item.IdUsuario;
                    break;
                }
            }

            var usuario = serviciosUsuario.BuscarPorId(IdDelUsuario);
            txtIdUsuario.Text = usuario.Entidad.IdUsuario.ToString();
            txtNombreUsuario.Text = usuario.Entidad.Nombre;
            txtNombreUsuariodelUsuario.Text = usuario.Entidad.NombreUsuario;
            cbRol.Text = usuario.Entidad.Rol;
            txtEmailUsu.Text = usuario.Entidad.Email;
            if (!string.IsNullOrEmpty(usuario.Entidad.RutaImagen) && File.Exists(usuario.Entidad.RutaImagen))
            {
                pbImagenUsuario.Image = Image.FromFile(usuario.Entidad.RutaImagen);
            }

        }

        private void ActualizarEstadoConexion()
        {
            if (Application.OpenForms["MENUPRINCIPAL"] != null)
            {
                lbConec.Text = "ACTIVO";
                lbConec.BackColor = ColorTranslator.FromHtml("#21864B"); // Verde
            }
            else
            {
                lbConec.Text = "DESCONECTADO";
                lbConec.BackColor = ColorTranslator.FromHtml("#8B0000"); // Rojo oscuro
            }
        }
        private async Task CicloRiegoAsync()
        {
            try
            {
                DialogResult resultado = MessageBox.Show($"¿Seguro que quiere iniciar el riego?",
                    "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    if (puedeRegar)
                    {
                        puedeRegar = false;
                        btnRiegoAuto.Enabled = false;

                        EnviarComandoSeguro("BOMBA_ON");
                        // Ejecuta el riego
                        lbEstadodeBomba.Text = "ACTIVO";
                        lbEstadodeBomba.BackColor = ColorTranslator.FromHtml("#21864B");

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        EnviarComandoSeguro("BOMBA_OFF");
                        EnviarComandoSeguro("AUTO");
                        lbEstadodeBomba.Text = "DESCONECTADO";
                        lbEstadodeBomba.BackColor = ColorTranslator.FromHtml("#8B0000");
                        await Task.Delay(TimeSpan.FromMinutes(5));

                        puedeRegar = true;
                        btnRiegoAuto.Enabled = true;
                        MessageBox.Show("Ya puedes volver a regar.");
                    }
                    else
                    {
                        MessageBox.Show("Debes esperar antes de volver a regar.");
                    }
                }
                else
                {
                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrio un error..." + ex);
            }

        }
        private void AplicarBordeSuave(Panel panel, int radio)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle rect = panel.ClientRectangle;
            int diametro = radio * 2;

            path.AddArc(rect.X, rect.Y, diametro, diametro, 180, 90);
            path.AddArc(rect.Right - diametro, rect.Y, diametro, diametro, 270, 90);
            path.AddArc(rect.Right - diametro, rect.Bottom - diametro, diametro, diametro, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diametro, diametro, diametro, 90, 90);
            path.CloseFigure();

            panel.Region = new Region(path);
        }

        private void panel8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }
        private void tabPage3_Click(object sender, EventArgs e)
        {

        }
        private void btnHam_Click(object sender, EventArgs e)
        {
            MenuTrancision.Start();
        }
        private void MenuTrancision_Tick_1(object sender, EventArgs e)
        {
            if (Expandir)
            {
                flowLayoutPanel1.Width -= 10;
                if (flowLayoutPanel1.Width <= 70)
                {
                    Expandir = false;
                    MenuTrancision.Stop();
                }

            }
            else
            {
                flowLayoutPanel1.Width += 10;
                if (flowLayoutPanel1.Width >= 216)
                {
                    Expandir = true;
                    MenuTrancision.Stop();
                }
            }
        }
        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void button5_Click_1(object sender, EventArgs e)
        {
            tabControl.SelectedIndex = 0;
        }
        private void tabPage3_Click_1(object sender, EventArgs e)
        {

        }
        private void lbUltimoRegado_Click(object sender, EventArgs e)
        {

        }
        private void panel2_Paint_1(object sender, PaintEventArgs e)
        {

        }
        private void tabPage1_Click_1(object sender, EventArgs e)
        {

        }
        private void button7_Click_1(object sender, EventArgs e)
        {
            tabControl.SelectedIndex = 1;

        }
        private void button6_Click_1(object sender, EventArgs e)
        {
            tabControl.SelectedIndex = 2;
        }
        private void label8_Click(object sender, EventArgs e)
        {

        }
        private void btnRiegoAuto_Click(object sender, EventArgs e)
        {
            _ = CicloRiegoAsync();
            ultimoRegado();
        }

        private void grilla_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            servicioHistorial.MostrarTodos();

        }
        private void label14_Click_1(object sender, EventArgs e)
        {

        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            lbFechaInicio.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        }

        private void MENUPRINCIPAL_Load_1(object sender, EventArgs e)
        {
            ActualizarEstadoConexion();
            ultimoRegado();
            timerTiempo.Start();
            timerClima.Start();
            CargarUsuario();
            timerHumedad.Start();
            AplicarBordesSuavesEnFormulario(this);



        }
        private void AplicarBordesSuavesEnFormulario(Control control)
        {
            foreach (Control c in control.Controls)
            {
                if (c is Panel panel)
                {
                    // Evita aplicar a paneles con diseño especial si lo deseas
                    AplicarBordeSuave(panel, 15);
                }

                // Si el control tiene hijos, recursivamente aplica también
                if (c.HasChildren)
                {
                    AplicarBordesSuavesEnFormulario(c);
                }
            }
        }


        private void btnHacerCambios_Click(object sender, EventArgs e)
        {
            HacerCambios();
        }

        private void button10_Click_1(object sender, EventArgs e)
        {

        }
        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void lbTemperatura_Click(object sender, EventArgs e)
        {

        }

        private void MENUPRINCIPAL_Resize(object sender, EventArgs e)
        {
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            _ = ObtenerDatosClimaAsync();
        }

        private void chartRiego_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void Mtemperatura_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            GuardarCambios();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialogo = new OpenFileDialog();
            var usu = serviciosUsuario.BuscarPorId(IdDelUsuario);
            dialogo.Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.bmp";

            if (dialogo.ShowDialog() == DialogResult.OK)
            {
                usu.Entidad.RutaImagen = dialogo.FileName;
                serviciosUsuario.Actualizar(usu.Entidad);
                pbImagenUsuario.Image = Image.FromFile(usu.Entidad.RutaImagen);
            }
            else
            {
                MessageBox.Show("No se seleccionó ninguna imagen.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            INICIAR form = new INICIAR();
            form.Show();
            this.Hide();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            DialogResult resultado = MessageBox.Show($"¿Seguro que quieres eliminar este Usuario?", "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                string contraseña = Interaction.InputBox($"Ingrese su la contraseña del Usuario {txtNombreUsuariodelUsuario.Text}:", "Registro obligatorio", "");
                var usu = serviciosUsuario.BuscarPorId(IdDelUsuario);
                if (contraseña == usu.Entidad.Password)
                {
                    var mensaje = serviciosUsuario.Eliminar(usu.Entidad.IdUsuario);
                    if (mensaje.Estado)
                    {
                        MessageBox.Show("Usuario eliminado correctamente. Volviendo a la pantalla de inicio de sesión.");
                        INICIAR form = new INICIAR();
                        form.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Hubo un error al eliminar el usuario.");
                    }
                }
            }
        }

        private void PanelPrincipal_Click(object sender, EventArgs e)
        {

        }
        private void MId_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            BuscarPlanta();
        }

        private void lblHumedad_Click(object sender, EventArgs e)
        {

        }
        private void MENUPRINCIPAL_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_servicioPuerto != null && _servicioPuerto.PuertoAbierto)
                {
                    _servicioPuerto.CerrarPuerto();
                    Console.WriteLine("Puerto cerrado correctamente al salir.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cerrar el puerto: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void cbRol_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listHum_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lbEstadodeBomba_Click(object sender, EventArgs e)
        {

        }
        private void timerHumedad_Tick(object sender, EventArgs e)
        {
            GraficarHumedad();
        }
        private void chart2_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            tabControl.SelectedIndex = 3;

        }
    }
}
