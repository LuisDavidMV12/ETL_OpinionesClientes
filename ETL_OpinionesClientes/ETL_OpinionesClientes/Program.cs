using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ETL_OpinionesClientes
{
    class Program
    {
        // Configuración de rutas y conexión
        private const string ConnectionString = "Server=localhost;Database=AnalisisOpinionesClientes;Integrated Security=true;";
        private static readonly string DataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== INICIANDO PIPELINE ETL - ANÁLISIS DE OPINIONES DE CLIENTES ===\n");

                // Verificar que existe la carpeta Data
                if (!Directory.Exists(DataPath))
                {
                    Directory.CreateDirectory(DataPath);
                    Console.WriteLine($"📁 Se creó la carpeta: {DataPath}");
                    Console.WriteLine("⚠️  Por favor, coloca los archivos CSV en esta carpeta y vuelve a ejecutar.");
                    return;
                }

                Console.WriteLine($"📂 Carpeta de datos: {DataPath}\n");

                // Fase 1: Extracción
                Console.WriteLine("1. EXTRACCIÓN DE DATOS...");
                var clientes = ExtraerClientes("clients.csv");
                var productos = ExtraerProductos("products.csv");
                var fuentes = ExtraerFuentes("fuente_datos.csv");
                var comentariosSociales = ExtraerComentariosSociales("social_comments.csv");
                var encuestas = ExtraerEncuestas("surveys_part1.csv");
                var reseñasWeb = ExtraerResenasWeb("web_reviews.csv");

                Console.WriteLine($"✓ Clientes extraídos: {clientes.Count}");
                Console.WriteLine($"✓ Productos extraídos: {productos.Count}");
                Console.WriteLine($"✓ Fuentes extraídas: {fuentes.Count}");
                Console.WriteLine($"✓ Comentarios sociales extraídos: {comentariosSociales.Count}");
                Console.WriteLine($"✓ Encuestas extraídas: {encuestas.Count}");
                Console.WriteLine($"✓ Reseñas web extraídas: {reseñasWeb.Count}");

                // Verificar que hay datos
                if (clientes.Count == 0 || productos.Count == 0)
                {
                    Console.WriteLine("\n❌ ERROR: No se pudieron extraer datos. Verifica que los archivos CSV estén en la carpeta Data.");
                    Console.WriteLine($"📁 Los archivos deben estar en: {DataPath}");
                    MostrarArchivosEnCarpeta();
                    return;
                }

                // Fase 2: Transformación
                Console.WriteLine("\n2. TRANSFORMACIÓN DE DATOS...");
                var clientesTransformados = TransformarClientes(clientes);
                var productosTransformados = TransformarProductos(productos);
                var fuentesTransformadas = TransformarFuentes(fuentes);
                var comentariosTransformados = TransformarComentariosSociales(comentariosSociales);
                var encuestasTransformadas = TransformarEncuestas(encuestas, clientesTransformados);
                var reseñasTransformadas = TransformarResenasWeb(reseñasWeb);

                Console.WriteLine("✓ Datos transformados y validados");

                // Fase 3: Carga
                Console.WriteLine("\n3. CARGA DE DATOS A LA BASE DE DATOS...");
                CargarDatos(clientesTransformados, productosTransformados, fuentesTransformadas,
                           comentariosTransformados, encuestasTransformadas, reseñasTransformadas);

                Console.WriteLine("\n=== PIPELINE ETL COMPLETADO EXITOSAMENTE ===");

                // Mostrar estadísticas
                MostrarEstadisticas();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en el pipeline ETL: {ex.Message}");
                Console.WriteLine($"🔍 Detalles: {ex.StackTrace}");
            }

            Console.WriteLine("\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }

        static void MostrarArchivosEnCarpeta()
        {
            Console.WriteLine("\n📋 Archivos encontrados en la carpeta Data:");
            try
            {
                var archivos = Directory.GetFiles(DataPath);
                foreach (var archivo in archivos)
                {
                    Console.WriteLine($"   - {Path.GetFileName(archivo)}");
                }

                if (archivos.Length == 0)
                {
                    Console.WriteLine("   (No se encontraron archivos)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error al leer carpeta: {ex.Message}");
            }
        }

        // Métodos de Extracción (modificados para manejar errores)
        static List<Cliente> ExtraerClientes(string fileName)
        {
            var clientes = new List<Cliente>();
            string filePath = Path.Combine(DataPath, fileName);

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Archivo no encontrado: {filePath}");
                    return clientes;
                }

                var lineas = File.ReadAllLines(filePath);
                if (lineas.Length <= 1)
                {
                    Console.WriteLine($"⚠️  Archivo vacío: {fileName}");
                    return clientes;
                }

                for (int i = 1; i < lineas.Length; i++) // Saltar encabezado
                {
                    var campos = lineas[i].Split(',');
                    if (campos.Length >= 3 && int.TryParse(campos[0], out int id))
                    {
                        clientes.Add(new Cliente
                        {
                            IdCliente = id,
                            Nombre = campos[1],
                            Email = campos[2]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo {fileName}: {ex.Message}");
            }

            return clientes;
        }

        // ... (Similar para los otros métodos de extracción)

        static List<Producto> ExtraerProductos(string fileName)
        {
            var productos = new List<Producto>();
            string filePath = Path.Combine(DataPath, fileName);

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Archivo no encontrado: {filePath}");
                    return productos;
                }

                var lineas = File.ReadAllLines(filePath);
                if (lineas.Length <= 1) return productos;

                for (int i = 1; i < lineas.Length; i++)
                {
                    var campos = lineas[i].Split(',');
                    if (campos.Length >= 3 && int.TryParse(campos[0], out int id))
                    {
                        productos.Add(new Producto
                        {
                            IdProducto = id,
                            Nombre = campos[1],
                            Categoria = campos[2]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo {fileName}: {ex.Message}");
            }

            return productos;
        }

        static List<Fuente> ExtraerFuentes(string filePath)
        {
            var fuentes = new List<Fuente>();
            var lineas = File.ReadAllLines(filePath).Skip(1);

            foreach (var linea in lineas)
            {
                var campos = linea.Split(',');
                if (campos.Length >= 3)
                {
                    fuentes.Add(new Fuente
                    {
                        IdFuente = campos[0],
                        TipoFuente = campos[1],
                        FechaCarga = DateTime.ParseExact(campos[2], "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    });
                }
            }
            return fuentes;
        }

        static List<ComentarioSocial> ExtraerComentariosSociales(string fileName)
        {
            var comentarios = new List<ComentarioSocial>();
            string filePath = Path.Combine(DataPath, fileName);

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Archivo no encontrado: {filePath}");
                    return comentarios;
                }

                var lineas = File.ReadAllLines(filePath);
                if (lineas.Length <= 1) return comentarios;

                Console.WriteLine($"📖 Leyendo comentarios sociales de: {fileName}");

                for (int i = 1; i < lineas.Length; i++)
                {
                    try
                    {
                        var campos = ParseCsvLine(lineas[i]);

                        if (campos.Length >= 6)
                        {
                            // IdCliente puede estar vacío
                            string idCliente = campos[1];
                            if (!string.IsNullOrEmpty(idCliente) && idCliente.StartsWith("C"))
                            {
                                idCliente = idCliente.Substring(1);
                            }

                            // IdProducto
                            string idProducto = campos[2];
                            if (idProducto.StartsWith("P"))
                            {
                                idProducto = idProducto.Substring(1);
                            }

                            // Validar fecha
                            if (!DateTime.TryParseExact(campos[4], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                            {
                                Console.WriteLine($"⚠️ Fecha inválida en línea {i}: {campos[4]}");
                                continue;
                            }

                            comentarios.Add(new ComentarioSocial
                            {
                                IdComment = campos[0],
                                IdCliente = string.IsNullOrEmpty(idCliente) ? null : idCliente,
                                IdProducto = idProducto,
                                Fuente = campos[3],
                                Fecha = fecha,
                                Comentario = campos[5].Trim('"')
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error procesando línea {i} de comentarios sociales: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo {fileName}: {ex.Message}");
            }

            Console.WriteLine($"✓ Comentarios sociales procesados: {comentarios.Count}");
            return comentarios;
        }

        static List<Encuesta> ExtraerEncuestas(string fileName)
        {
            var encuestas = new List<Encuesta>();
            string filePath = Path.Combine(DataPath, fileName);

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Archivo no encontrado: {filePath}");
                    return encuestas;
                }

                var lineas = File.ReadAllLines(filePath);
                if (lineas.Length <= 1)
                {
                    Console.WriteLine($"⚠️ Archivo vacío: {fileName}");
                    return encuestas;
                }

                Console.WriteLine($"📖 Leyendo encuestas de: {fileName}");

                for (int i = 1; i < lineas.Length; i++) // Saltar encabezado
                {
                    try
                    {
                        var linea = lineas[i];

                        // Manejar comas dentro de campos entre comillas
                        var campos = ParseCsvLine(linea);

                        if (campos.Length >= 8)
                        {
                            // Debug: mostrar los primeros campos para verificar
                            if (i <= 3) // Mostrar solo las primeras 3 líneas para debug
                            {
                                Console.WriteLine($"   Línea {i}: {string.Join(" | ", campos.Take(5))}...");
                            }

                            // Validar y convertir IdOpinion
                            if (!int.TryParse(campos[0], out int idOpinion))
                            {
                                Console.WriteLine($"⚠️ IdOpinion inválido en línea {i}: {campos[0]}");
                                continue;
                            }

                            // Validar y convertir IdCliente
                            if (!int.TryParse(campos[1], out int idCliente))
                            {
                                Console.WriteLine($"⚠️ IdCliente inválido en línea {i}: {campos[1]}");
                                continue;
                            }

                            // Validar y convertir IdProducto
                            if (!int.TryParse(campos[2], out int idProducto))
                            {
                                Console.WriteLine($"⚠️ IdProducto inválido en línea {i}: {campos[2]}");
                                continue;
                            }

                            // Validar y convertir Fecha
                            if (!DateTime.TryParseExact(campos[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                            {
                                Console.WriteLine($"⚠️ Fecha inválida en línea {i}: {campos[3]}");
                                continue;
                            }

                            // El comentario está en campos[4] (puede contener comas)
                            string comentario = campos[4].Trim('"');

                            // Clasificación está en campos[5]
                            string clasificacion = campos[5];

                            // Validar y convertir PuntajeSatisfaccion (campo 6)
                            if (!int.TryParse(campos[6], out int puntaje) || puntaje < 1 || puntaje > 5)
                            {
                                Console.WriteLine($"⚠️ Puntaje inválido en línea {i}: {campos[6]}");
                                continue;
                            }

                            // Fuente está en campos[7]
                            string fuente = campos[7];

                            encuestas.Add(new Encuesta
                            {
                                IdOpinion = idOpinion,
                                IdCliente = idCliente,
                                IdProducto = idProducto,
                                Fecha = fecha,
                                Comentario = comentario,
                                Clasificacion = clasificacion,
                                PuntajeSatisfaccion = puntaje,
                                Fuente = fuente
                            });
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Línea {i} tiene {campos.Length} campos (se esperaban 8)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error procesando línea {i} de encuestas: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo {fileName}: {ex.Message}");
            }

            Console.WriteLine($"✓ Encuestas procesadas: {encuestas.Count}");
            return encuestas;
        }

        // Método auxiliar para parsear líneas CSV con comas dentro de campos entre comillas
        static string[] ParseCsvLine(string linea)
        {
            var campos = new List<string>();
            var campoActual = new StringBuilder();
            bool entreComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];

                if (c == '"')
                {
                    entreComillas = !entreComillas;
                }
                else if (c == ',' && !entreComillas)
                {
                    campos.Add(campoActual.ToString());
                    campoActual.Clear();
                }
                else
                {
                    campoActual.Append(c);
                }
            }

            // Añadir el último campo
            campos.Add(campoActual.ToString());

            return campos.ToArray();
        }

        static List<ResenaWeb> ExtraerResenasWeb(string fileName)
        {
            var reseñas = new List<ResenaWeb>();
            string filePath = Path.Combine(DataPath, fileName);

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Archivo no encontrado: {filePath}");
                    return reseñas;
                }

                var lineas = File.ReadAllLines(filePath);
                if (lineas.Length <= 1) return reseñas;

                Console.WriteLine($"📖 Leyendo reseñas web de: {fileName}");

                for (int i = 1; i < lineas.Length; i++)
                {
                    try
                    {
                        var campos = ParseCsvLine(lineas[i]);

                        if (campos.Length >= 6)
                        {
                            // IdCliente
                            string idCliente = campos[1];
                            if (idCliente.StartsWith("C"))
                            {
                                idCliente = idCliente.Substring(1);
                            }

                            // IdProducto
                            string idProducto = campos[2];
                            if (idProducto.StartsWith("P"))
                            {
                                idProducto = idProducto.Substring(1);
                            }

                            // Validar fecha
                            if (!DateTime.TryParseExact(campos[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                            {
                                Console.WriteLine($"⚠️ Fecha inválida en línea {i}: {campos[3]}");
                                continue;
                            }

                            // Validar rating
                            if (!int.TryParse(campos[5], out int rating) || rating < 1 || rating > 5)
                            {
                                Console.WriteLine($"⚠️ Rating inválido en línea {i}: {campos[5]}");
                                continue;
                            }

                            reseñas.Add(new ResenaWeb
                            {
                                IdReview = campos[0],
                                IdCliente = idCliente,
                                IdProducto = idProducto,
                                Fecha = fecha,
                                Comentario = campos[4],
                                Rating = rating
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error procesando línea {i} de reseñas web: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo {fileName}: {ex.Message}");
            }

            Console.WriteLine($"✓ Reseñas web procesadas: {reseñas.Count}");
            return reseñas;
        }

        // Métodos de Transformación
        static List<Cliente> TransformarClientes(List<Cliente> clientes)
        {
            // Eliminar duplicados
            return clientes.GroupBy(c => c.IdCliente)
                          .Select(g => g.First())
                          .ToList();
        }

        static List<Producto> TransformarProductos(List<Producto> productos)
        {
            // Validar y limpiar categorías
            var categoriasValidas = new HashSet<string> { "Juguetes", "Electrónica", "Ropa", "Hogar", "Deportes" };

            return productos.Where(p => categoriasValidas.Contains(p.Categoria))
                           .GroupBy(p => p.IdProducto)
                           .Select(g => g.First())
                           .ToList();
        }

        static List<ComentarioSocial> TransformarComentariosSociales(List<ComentarioSocial> comentarios)
        {
            // Limpiar comentarios y validar fechas
            return comentarios.Where(c => c.Fecha.Year >= 2024)
                             .Where(c => !string.IsNullOrEmpty(c.Comentario))
                             .ToList();
        }

        static List<Encuesta> TransformarEncuestas(List<Encuesta> encuestas, List<Cliente> clientes)
        {
            // Crear conjunto de IDs de clientes existentes
            var idsClientesExistentes = new HashSet<int>(clientes.Select(c => c.IdCliente));

            Console.WriteLine($"🔍 Filtrando encuestas - Clientes existentes: {idsClientesExistentes.Count}");

            // Filtrar encuestas que tengan clientes existentes
            var encuestasFiltradas = encuestas.Where(e => idsClientesExistentes.Contains(e.IdCliente)).ToList();

            // También filtrar por productos existentes si es necesario
            // var idsProductosExistentes = new HashSet<int>(productos.Select(p => p.IdProducto));
            // encuestasFiltradas = encuestasFiltradas.Where(e => idsProductosExistentes.Contains(e.IdProducto)).ToList();

            Console.WriteLine($"✓ Encuestas después de filtrar: {encuestasFiltradas.Count} (de {encuestas.Count} originales)");

            // Validar puntajes (1-5) y fechas
            return encuestasFiltradas
                .Where(e => e.PuntajeSatisfaccion >= 1 && e.PuntajeSatisfaccion <= 5)
                .Where(e => e.Fecha.Year >= 2024)
                .ToList();
        }

        static List<ResenaWeb> TransformarResenasWeb(List<ResenaWeb> reseñas)
        {
            // Validar ratings (1-5) y limpiar datos
            return reseñas.Where(r => r.Rating >= 1 && r.Rating <= 5)
                         .Where(r => r.Fecha.Year >= 2024)
                         .ToList();
        }

        static List<Fuente> TransformarFuentes(List<Fuente> fuentes)
        {
            return fuentes.GroupBy(f => f.IdFuente)
                         .Select(g => g.First())
                         .ToList();
        }

        // Métodos de Carga
        static void CargarDatos(List<Cliente> clientes, List<Producto> productos, List<Fuente> fuentes,
                       List<ComentarioSocial> comentarios, List<Encuesta> encuestas, List<ResenaWeb> reseñas)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // Diccionarios para mapear IDs antiguos a nuevos IDs generados
                var clienteIdMap = new Dictionary<int, int>();
                var productoIdMap = new Dictionary<int, int>();

                // 1. Cargar Clientes y capturar los nuevos IDs
                Console.WriteLine("Cargando clientes...");
                foreach (var cliente in clientes)
                {
                    var command = new SqlCommand(
                        "INSERT INTO Clientes (Nombre, Email) OUTPUT INSERTED.IdCliente VALUES (@Nombre, @Email)",
                        connection);
                    command.Parameters.AddWithValue("@Nombre", cliente.Nombre);
                    command.Parameters.AddWithValue("@Email", cliente.Email);

                    int nuevoId = (int)command.ExecuteScalar();
                    clienteIdMap[cliente.IdCliente] = nuevoId; // Mapear ID antiguo → nuevo
                }

                // 2. Cargar Productos y capturar los nuevos IDs
                Console.WriteLine("Cargando productos...");
                foreach (var producto in productos)
                {
                    var command = new SqlCommand(
                        "INSERT INTO Productos (Nombre, Categoria) OUTPUT INSERTED.IdProducto VALUES (@Nombre, @Categoria)",
                        connection);
                    command.Parameters.AddWithValue("@Nombre", producto.Nombre);
                    command.Parameters.AddWithValue("@Categoria", producto.Categoria);

                    int nuevoId = (int)command.ExecuteScalar();
                    productoIdMap[producto.IdProducto] = nuevoId; // Mapear ID antiguo → nuevo
                }

                // 3. Cargar Fuentes (sin cambios)
                Console.WriteLine("Cargando fuentes...");
                foreach (var fuente in fuentes)
                {
                    var command = new SqlCommand(
                        "INSERT INTO Fuentes (IdFuente, TipoFuente, FechaCarga) VALUES (@Id, @Tipo, @Fecha)",
                        connection);
                    command.Parameters.AddWithValue("@Id", fuente.IdFuente);
                    command.Parameters.AddWithValue("@Tipo", fuente.TipoFuente);
                    command.Parameters.AddWithValue("@Fecha", fuente.FechaCarga);
                    command.ExecuteNonQuery();
                }

                // 4. Cargar Comentarios Sociales (usar mapeo de IDs)
                Console.WriteLine("Cargando comentarios sociales...");
                foreach (var comentario in comentarios)
                {
                    // Verificar si el IdCliente existe en el mapeo
                    int? nuevoIdCliente = null;
                    if (!string.IsNullOrEmpty(comentario.IdCliente) &&
                        int.TryParse(comentario.IdCliente, out int idClienteAntiguo) &&
                        clienteIdMap.ContainsKey(idClienteAntiguo))
                    {
                        nuevoIdCliente = clienteIdMap[idClienteAntiguo];
                    }

                    // Verificar IdProducto
                    if (!int.TryParse(comentario.IdProducto, out int idProductoAntiguo) ||
                        !productoIdMap.ContainsKey(idProductoAntiguo))
                    {
                        Console.WriteLine($"⚠️ Producto no encontrado para comentario {comentario.IdComment}");
                        continue;
                    }

                    var command = new SqlCommand(
                        "INSERT INTO ComentariosSociales (IdComment, IdCliente, IdProducto, Fuente, Fecha, Comentario) " +
                        "VALUES (@Id, @IdCliente, @IdProducto, @Fuente, @Fecha, @Comentario)",
                        connection);

                    command.Parameters.AddWithValue("@Id", comentario.IdComment);
                    command.Parameters.AddWithValue("@IdCliente", nuevoIdCliente ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IdProducto", productoIdMap[idProductoAntiguo]);
                    command.Parameters.AddWithValue("@Fuente", comentario.Fuente);
                    command.Parameters.AddWithValue("@Fecha", comentario.Fecha);
                    command.Parameters.AddWithValue("@Comentario", comentario.Comentario);
                    command.ExecuteNonQuery();
                }

                // 5. Cargar Encuestas (usar mapeo de IDs)
                Console.WriteLine("Cargando encuestas...");
                foreach (var encuesta in encuestas)
                {
                    // Verificar que existan los IDs mapeados
                    if (!clienteIdMap.ContainsKey(encuesta.IdCliente))
                    {
                        Console.WriteLine($"⚠️ Cliente no encontrado para encuesta {encuesta.IdOpinion}");
                        continue;
                    }
                    if (!productoIdMap.ContainsKey(encuesta.IdProducto))
                    {
                        Console.WriteLine($"⚠️ Producto no encontrado para encuesta {encuesta.IdOpinion}");
                        continue;
                    }

                    var command = new SqlCommand(
                        "INSERT INTO Encuestas (IdOpinion, IdCliente, IdProducto, Fecha, Comentario, Clasificacion, PuntajeSatisfaccion, Fuente) " +
                        "VALUES (@Id, @IdCliente, @IdProducto, @Fecha, @Comentario, @Clasificacion, @Puntaje, @Fuente)",
                        connection);

                    command.Parameters.AddWithValue("@Id", encuesta.IdOpinion);
                    command.Parameters.AddWithValue("@IdCliente", clienteIdMap[encuesta.IdCliente]);
                    command.Parameters.AddWithValue("@IdProducto", productoIdMap[encuesta.IdProducto]);
                    command.Parameters.AddWithValue("@Fecha", encuesta.Fecha);
                    command.Parameters.AddWithValue("@Comentario", encuesta.Comentario);
                    command.Parameters.AddWithValue("@Clasificacion", encuesta.Clasificacion);
                    command.Parameters.AddWithValue("@Puntaje", encuesta.PuntajeSatisfaccion);
                    command.Parameters.AddWithValue("@Fuente", encuesta.Fuente);
                    command.ExecuteNonQuery();
                }

                // 6. Cargar Reseñas Web (usar mapeo de IDs)
                Console.WriteLine("Cargando reseñas web...");
                foreach (var reseña in reseñas)
                {
                    // Verificar IDs
                    if (!int.TryParse(reseña.IdCliente, out int idClienteAntiguo) ||
                        !clienteIdMap.ContainsKey(idClienteAntiguo))
                    {
                        Console.WriteLine($"⚠️ Cliente no encontrado para reseña {reseña.IdReview}");
                        continue;
                    }
                    if (!int.TryParse(reseña.IdProducto, out int idProductoAntiguo) ||
                        !productoIdMap.ContainsKey(idProductoAntiguo))
                    {
                        Console.WriteLine($"⚠️ Producto no encontrado para reseña {reseña.IdReview}");
                        continue;
                    }

                    var command = new SqlCommand(
                        "INSERT INTO ResenasWeb (IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating) " +
                        "VALUES (@Id, @IdCliente, @IdProducto, @Fecha, @Comentario, @Rating)",
                        connection);

                    command.Parameters.AddWithValue("@Id", reseña.IdReview);
                    command.Parameters.AddWithValue("@IdCliente", clienteIdMap[idClienteAntiguo]);
                    command.Parameters.AddWithValue("@IdProducto", productoIdMap[idProductoAntiguo]);
                    command.Parameters.AddWithValue("@Fecha", reseña.Fecha);
                    command.Parameters.AddWithValue("@Comentario", reseña.Comentario);
                    command.Parameters.AddWithValue("@Rating", reseña.Rating);
                    command.ExecuteNonQuery();
                }
            }
        }

        static void MostrarEstadisticas()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var tablas = new[] { "Clientes", "Productos", "Fuentes", "ComentariosSociales", "Encuestas", "ResenasWeb" };

                Console.WriteLine("\n=== ESTADÍSTICAS DE CARGA ===");
                foreach (var tabla in tablas)
                {
                    var command = new SqlCommand($"SELECT COUNT(*) FROM {tabla}", connection);
                    var count = command.ExecuteScalar();
                    Console.WriteLine($"{tabla}: {count} registros");
                }

                Console.WriteLine("\n=== MUESTRA DE DATOS CARGADOS ===");
                foreach (var tabla in tablas)
                {
                    Console.WriteLine($"\n--- {tabla} (primeros 3 registros) ---");
                    var command = new SqlCommand($"SELECT TOP 3 * FROM {tabla}", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i)}\t");
                        }
                        Console.WriteLine();

                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.Write($"{reader[i]}\t");
                            }
                            Console.WriteLine();
                        }
                        reader.Close();
                    }
                }
            }
        }
    }

}
