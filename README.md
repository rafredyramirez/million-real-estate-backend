# RealEstate — Backend (.NET 8 + MongoDB + Docker)

API para consultar propiedades almacenadas en MongoDB con **filtros por nombre, dirección y rango de precios**, **paginación**, **ordenamiento** y **detalle**. Incluye **CORS configurable**, **health/readiness**, **logging estructurado (Serilog)** con **Correlation-ID**, y **tests** (NUnit + Moq).

---

## 📦 Tecnologías

- **Framework:** ASP.NET Core 8 (C#)
- **DB:** MongoDB 7
- **UI DB:** mongo-express (admin web)
- **Tests:** NUnit, Moq, FluentAssertions
- **Logging:** Serilog (JSON), Correlation-ID
- **Contenedores:** Docker / Docker Compose
- **IDE:** Visual Studio 2022 (o CLI)

---

## 🧱 Arquitectura & Patrones

**Clean Architecture / Ports & Adapters**

- `RealEstate.Api` → Presentación (Controllers, Swagger, CORS, Health/Ready, Middlewares)
- `RealEstate.Application` → Casos de uso / Servicios / Interfaces (puertos)
- `RealEstate.Contracts` → DTOs / Requests / Responses (contratos)
- `RealEstate.Domain` → Entidades de Dominio
- `RealEstate.Infraestructure.Mongo` → Adaptador MongoDB (Contexto, Repositorio, Índices)

**Patrones y prácticas**
- Repository Pattern: `IPropertyRepository` + `PropertyRepository`
- Application Service: `IPropertyService` + `PropertyService`
- DTOs en Contracts (separación clara dominio <-> transporte)
- Options Pattern para `MongoOptions`
- Consultas eficientes (Aggregation): `$match`, `$lookup` (imagen principal), `$project` con `"$toString"` para `ObjectId`
- CORS configurable por entorno (`Cors:AllowedOrigins`)
- Health/Readiness: `/healthz` (liveness) y `/readyz` (ping a Mongo con timeout controlado)
- Serilog + Correlation-ID: request logging, trazabilidad y diagnóstico en contenedores

---

## 🗃️ Modelo de Datos (Mongo)

Colecciones y **validadores ($jsonSchema)** definidos en `docker/mongo/init/00-init-realestate.js` (estructura) y `docker/mongo/init/01-seed-realestate.js` (datos demo):

- **Owners** `{ _id, Name, Address?, Photo?, Birthday? }`
- **Properties** `{ _id, IdOwner, Name, Address, Price, CodeInternal, Year, CreatedAt, UpdatedAt }`
- **PropertyImages** `{ _id, IdProperty, File, Enabled }`
- **PropertyTraces** `{ _id, IdProperty, DateSale, Name, Value, Tax }`

**Índices clave**
- `Properties.tx_name_address`: **text** en `Name` + `Address`
- `Properties.uk_codeinternal`: **único** en `CodeInternal`
- `Properties.ix_price`, `ix_owner`, `ix_createdAt_desc`
- `PropertyImages.ix_property_enabled`: `(IdProperty, Enabled)`
- `PropertyTraces.ix_property_datesale_desc`: `(IdProperty, DateSale desc)`

---

## 🔧 Configuración

### Variables de entorno (.env)

Para configurar variables locales sin comprometer credenciales, este repo incluye un archivo .env.example.
No edites ese archivo directamente: duplica el archivo, renómbralo a .env.

---

## ▶️ Ejecución con Docker

Desde la carpeta del backend:

```bash
docker compose up -d --build
```

Servicios típicos:
- **Mongo**: `localhost:27017` 
- **mongo-express**: `http://localhost:8081` (admin/admin)
- **API**: `http://localhost:5153`
  - Swagger: `http://localhost:5153/swagger`
  - Health: `http://localhost:5153/healthz`
  - Ready: `http://localhost:5153/readyz`

**Conexión API ↔ Mongo en Docker**
- Dentro de la red de Docker, el host de Mongo es **`mongo`**.  

---

## ▶️ Ejecución local (sin Docker)

### Restaurar & Ejecutar
```bash
dotnet restore
dotnet build
dotnet run --project ./RealEstate.Api/RealEstate.Api.csproj
# API en http://localhost:5153
```

### Conexión a Mongo local
Asegúrate de que el contenedor de Mongo exponga 27017:
```bash
docker ps  # ver 0.0.0.0:27017->27017/tcp
```
Prueba:
```bash
mongosh "mongodb://realestate_app:changeme@localhost:27017/realestate?authSource=realestate&authMechanism=SCRAM-SHA-256" --eval "db.runCommand({ ping: 1 })"
```

---

## 🔗 Endpoints principales

Prefijo: **`/api`**

- **GET** `/api/Properties` — filtros:
  - `name` (string), `address` (string)
  - `minPrice`, `maxPrice` (número)
  - `page`, `pageSize`
  - `sortBy` = `CreatedAt|Price|Name`, `sortDir` = `asc|desc`

  **Ejemplo:**
  ```text
  GET http://localhost:5153/api/Properties?page=1&pageSize=12&sortBy=CreatedAt&sortDir=desc&name=casa
  ```

- **GET** `/api/Properties/{id}` — detalle por Id

**Health**
- **GET** `/healthz` → `{ status: "ok" }`
- **GET** `/readyz` → `{ status: "ready" }` si Mongo responde a `ping`

---




## 🧪 Tests

Proyecto `RealEstate.UnitTests` (NUnit + Moq + FluentAssertions).

### Ejecutar
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura (global)
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
# abrir coverage/index.html
```

### Cobertura (VS Test)
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./TestResults/coverage/ /p:CoverletOutputFormat=cobertura
reportgenerator -reports:./**/TestResults/coverage/coverage.cobertura.xml -targetdir:coverage
```

**Pruebas incluidas**
- `PropertyService`:
  - Normaliza filtros nulos y delega a repositorio
  - Devuelve `PagedResult` correcto
- `PropertiesController`:
  - Valida `id` inválidos → 400
  - `id` no encontrado → 404
  - Lista con filtros vacíos → 200 + payload esperado

---

## 🔗 Uso conjunto con el Frontend

### 1) Backend
- Repositorio: **million-real-estate-backend**  
- Arranca con Docker:
   ```bash
   docker compose up -d --build
   # Swagger: http://localhost:5153/swagger
   ```
- Verifica:
  - Health: `http://localhost:5153/healthz` → `{ "status": "ok" }`
  - Ready: `http://localhost:5153/readyz` → `{ "status": "ready" }`
  - Swagger: `http://localhost:5153/swagger`

### 2) Frontend
- Repositorio: **million-real-estate-frontend**
- Dev local:
     ```bash
     # .env.development → VITE_API_URL=http://localhost:5153/api
     npm ci && npm run dev
     # http://localhost:5173
     ```
- Docker frontend:
  ```bash
  # .env (Compose) con VITE_API_URL=http://localhost:5153/api
  docker compose up -d --build
  # http://localhost:8080
  ```