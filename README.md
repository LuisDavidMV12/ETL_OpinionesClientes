Documentación del Proyecto ETL – Sistema de Análisis de Opiniones de Clientes
1. Descripción general

El proyecto consiste en un proceso ETL (Extracción, Transformación y Carga) que permite procesar archivos CSV con información de clientes, productos, encuestas, comentarios sociales, reseñas web y fuentes de datos, para almacenarlos en una base de datos relacional y facilitar su análisis.

2. Flujo de trabajo del pipeline ETL
Fase 1: Extracción

Se leen los archivos CSV correspondientes a:

clientes.csv → información de clientes (IdCliente, Nombre, Apellido, Email, etc.)

productos.csv → información de productos (IdProducto, Nombre, Categoría, Precio, etc.)

surveys_part1.csv → encuestas realizadas por los clientes

social_comments.csv → comentarios sociales de usuarios

web_reviews.csv → reseñas de productos en la web

sources.csv → fuentes de datos

Fase 2: Transformación

Se realiza limpieza y normalización de los datos:

Eliminación de registros duplicados o con campos nulos.

Normalización de formatos de fecha, texto y números.

Validación de integridad referencial (ej. cada encuesta debe corresponder a un cliente existente).

Fase 3: Carga

Se insertan los datos transformados en las tablas de la base de datos siguiendo este orden:

Clientes

Productos

Fuentes

Comentarios sociales

Encuestas

Reseñas web

Se respetan las claves primarias y foráneas para mantener la integridad de los datos.

3. Modelo de datos

Tablas principales y relaciones:

Tabla	PK	FK	Descripción
Clientes	IdCliente	—	Información de los clientes
Productos	IdProducto	—	Información de productos
Fuentes	IdFuente	—	Fuentes de datos
ComentariosSociales	IdComentario	IdCliente, IdFuente	Comentarios sociales de clientes
Encuestas	IdEncuesta	IdCliente, IdProducto	Encuestas realizadas por clientes
WebReviews	IdReseña	IdProducto, IdFuente	Reseñas web de productos
