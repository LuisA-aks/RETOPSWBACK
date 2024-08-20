using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RETOPSW.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        /*private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };*/

        private readonly ILogger<WeatherForecastController> _logger;
        private IConfiguration _config;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
        }

        [HttpGet]
        [Route("DatosIniciales")]  ////Datos Iniciales de la tabla de tareas,procedimiento funciona también para filtrar
        public IActionResult ObtenerTareas(int? idTarea = null, int? idColaborador = null, int? idEstado = null, int? idPrioridad = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            string query = "TAREAS_LISTA";
            DataTable table = new DataTable();
            string sqlDataSource = _config.GetConnectionString("AppConn");
            SqlDataReader myreader;

            using (SqlConnection mycon = new SqlConnection(sqlDataSource))
            {
                mycon.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, mycon))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    // Añadir los parámetros al comando SQL
                    sqlCommand.Parameters.AddWithValue("@Id_Tarea", idTarea ?? (object)DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Id_Colaborador", idColaborador ?? (object)DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Id_Estado", idEstado ?? (object)DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Id_Prioridad", idPrioridad ?? (object)DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Fecha_Inicio", fechaInicio ?? (object)DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Fecha_Fin", fechaFin ?? (object)DBNull.Value);

                    myreader = sqlCommand.ExecuteReader();
                    table.Load(myreader);
                    myreader.Close();
                    mycon.Close();
                }
            }
            return new JsonResult(table);
        }

        [HttpGet]
        [Route("CargoCombox")] ///Carga inicial de combox para estados,colaboradores y prioridades, devuelvo objeto JSON 
        ///nodos Estados, Prioridades y Colaboradores
        public IActionResult GetTables()
        {
            var result = new
            {
                Estados = new List<object>(),
                Prioridades = new List<object>(),
                Colaboradores = new List<object>()
            };

            string sqlDataSource = _config.GetConnectionString("AppConn");

            using (SqlConnection mycon = new SqlConnection(sqlDataSource))
            {
                mycon.Open();

                // Cargar datos de la tabla ESTADOS
                string queryEstados = "SELECT * FROM ESTADOS";
                using (SqlCommand cmdEstados = new SqlCommand(queryEstados, mycon))
                {
                    using (SqlDataReader reader = cmdEstados.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Estados.Add(new
                            {
                                Id_Estado = reader["Id_Estado"],
                                Estado = reader["Estado"]
                            });
                        }
                    }
                }

                // Cargar datos de la tabla PRIORIDAD
                string queryPrioridad = "SELECT * FROM PRIORIDAD";
                using (SqlCommand cmdPrioridad = new SqlCommand(queryPrioridad, mycon))
                {
                    using (SqlDataReader reader = cmdPrioridad.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Prioridades.Add(new
                            {
                                Id_Prioridad = reader["Id_Prioridad"],
                                Prioridad = reader["Prioridad"]
                            });
                        }
                    }
                }

                // Cargar datos de la tabla COLABORADOR
                string queryColaborador = "SELECT * FROM COLABORADOR";
                using (SqlCommand cmdColaborador = new SqlCommand(queryColaborador, mycon))
                {
                    using (SqlDataReader reader = cmdColaborador.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Colaboradores.Add(new
                            {
                                Id_Colaborador = reader["Id_Colaborador"],
                                Nombre = reader["Nombre"]
                            });
                        }
                    }
                }
            }

            return new JsonResult(result);
        }


        [HttpPost]
        [Route("EliminarDato")] /// Se utiliza para eliminar un tarea con el procedimiento almacenado
        public JsonResult EliminarTarea([FromForm] string id)
        {
            string connectionString = _config.GetConnectionString("AppConn");
            string message;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("ELIMINAR_TAREA", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id_Tarea", int.Parse(id));

                    // Ejecutar el procedimiento almacenado y obtener el mensaje de resultado
                    var result = command.ExecuteScalar();
                    message = result != null ? result.ToString() : "Error desconocido";
                }
            }

            // Devolver el mensaje como una cadena de texto
            return new JsonResult(message);
        }


        [HttpPost]
        [Route("AgregarDato")]  /// agregamos tarea, campo colaborador opcional, para cumplir el requisito pedido
        public JsonResult AgregarDato(
        [FromForm] string descripcion,
        [FromForm] string notas,
        [FromForm] string prioridad,
        [FromForm] string? colaborador,
        [FromForm] string estado,
        [FromForm] DateTime FechaInicio,
        [FromForm] DateTime FechaFin)
        {
            string message = "Tarea agregada exitosamente";
            string sqlDataSource = _config.GetConnectionString("AppConn");

            using (SqlConnection mycon = new SqlConnection(sqlDataSource))
            {
                try
                {
                    mycon.Open();
                    using (SqlCommand sqlCommand = new SqlCommand("AGREGAR_TAREA", mycon))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;

                        sqlCommand.Parameters.AddWithValue("@Descripcion", descripcion);
                        sqlCommand.Parameters.AddWithValue("@Notas", notas);
                        sqlCommand.Parameters.AddWithValue("@Id_Prioridad", prioridad);
                        sqlCommand.Parameters.AddWithValue("@Id_Colaborador", colaborador);
                        sqlCommand.Parameters.AddWithValue("@Id_Estado", estado);
                        sqlCommand.Parameters.AddWithValue("@Fecha_Inicio", FechaInicio);
                        sqlCommand.Parameters.AddWithValue("@Fecha_Fin", FechaFin);

                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    message = $"Error al agregar la tarea: {ex.Message}";
                }
                finally
                {
                    mycon.Close();
                }
            }

            return new JsonResult(new { Message = message });
        }

        [HttpPost]
        [Route("EditarDato")] //// Sirve para la edición de una tarea
        public IActionResult EditarDato([FromForm] string idtarea, [FromForm] string descripcion,
            [FromForm] string prioridad, [FromForm] string colaborador, [FromForm] string estado,
            [FromForm] DateTime FechaInicio, [FromForm] DateTime FechaFin)
        {
            string sqlDataSource = _config.GetConnectionString("AppConn");
            string message = "Edición tarea exitosa";

            using (SqlConnection mycon = new SqlConnection(sqlDataSource))
            {
                try
                {
                    mycon.Open();
                    using (SqlCommand sqlCommand = new SqlCommand("EDITAR_TAREA", mycon))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;

                        sqlCommand.Parameters.AddWithValue("@Id_Tarea", idtarea);
                        sqlCommand.Parameters.AddWithValue("@Descripcion", descripcion);
                        sqlCommand.Parameters.AddWithValue("@Id_Prioridad", int.Parse(prioridad));
                        sqlCommand.Parameters.AddWithValue("@Id_Estado", int.Parse(estado));
                        //sqlCommand.Parameters.AddWithValue("@Id_Colaborador", int.Parse(colaborador));
                        int colaboradorId;
                        if (int.TryParse(colaborador, out colaboradorId))
                        {
                            sqlCommand.Parameters.AddWithValue("@Id_Colaborador", colaboradorId);
                        }
                        else
                        {
                            sqlCommand.Parameters.AddWithValue("@Id_Colaborador", DBNull.Value);
                        }


                        sqlCommand.Parameters.AddWithValue("@Fecha_Inicio", FechaInicio);
                        sqlCommand.Parameters.AddWithValue("@Fecha_Fin", FechaFin);

                        // Ejecutar el procedimiento almacenado y obtener el mensaje de resultado
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    message = $"Error al agregar la tarea: {ex.Message}";
                }
                finally
                {
                    mycon.Close();
                }
            }
            return Ok(new { success = true, message = "Dato editado correctamente." });
        }
        







    }
}
