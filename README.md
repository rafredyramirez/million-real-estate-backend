RealEstate — Backend (.NET 8 + MongoDB + Docker)

API para consultar propiedades almacenadas en MongoDB con filtros por nombre, dirección y rango de precios, paginación, ordenamiento y detalle.

📦 Tecnologías
- Backend: ASP.NET Core 8 (C#)
- DB: MongoDB 7
- UI DB: mongo-express (admin web)
- Tests: XUnit 
- Contenedores: Docker / Docker Compose
- IDE: Visual Studio 2022

🧱Arquitectura & Patrones

Clean Architecture / Ports & Adapters

- RealEstate.Api                   → Presentación (Controllers, Swagger, CORS, Health)
- RealEstate.Application   → Casos de uso / Servicios / Interfaces (puertos)
- RealEstate.Contracts             → DTOs / Requests / Responses (contratos)
- RealEstate.Domain                → Entidades de Dominio 
- RealEstate.Infraestructure.Mongo → Adaptador MongoDB (Contexto, Repositorio, Índices)

Patrones y prácticas:

- Repository Pattern: IPropertyRepository + PropertyRepository
- Application Service: IPropertyService + PropertyService
- DTOs en Contracts 
- Options Pattern para MongoOptions
- Consultas eficientes (Aggregation): $match, $lookup (imagen principal), $project con "$toString" para ObjectId
- CORS configurable por entorno
- Health/Readiness: /healthz (liveness) y /readyz (ping a Mongo)

🗃️ Modelo de Datos (Mongo)

Colecciones y validadores ($jsonSchema) definidos en docker/mongo/init/00-init-realestate.js:

- Owners { _id, Name, Address?, Photo?, Birthday? }
- Properties { _id, IdOwner, Name, Address, Price, CodeInternal, Year, CreatedAt, UpdatedAt }
- PropertyImages { _id, IdProperty, File, Enabled }
- PropertyTraces { _id, IdProperty, DateSale, Name, Value, Tax }

Índices clave:
- Properties.tx_name_address: text en Name + Address
- Properties.uk_codeinternal: único en CodeInternal
- Properties.ix_price, ix_owner, ix_createdAt_desc
- PropertyImages.ix_property_enabled: (IdProperty, Enabled)
- PropertyTraces.ix_property_datesale_desc: (IdProperty, DateSale desc)

🚀 Ejecución con Docker 
- docker compose up -d --build
- Mongo-Express: http://127.0.0.1:8085 (admin/admin)
- API: http://localhost:5153
- Swagger: http://localhost:5153/swagger